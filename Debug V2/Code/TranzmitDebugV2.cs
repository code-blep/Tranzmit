using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using UnityEngine.UIElements;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.Serialization;
using Object = System.Object;

namespace Blep.Tranzmit
{
    public class TranzmitDebugV2 : SerializedMonoBehaviour
    {
        /// <summary>
        /// Used for testing and general debugging.
        /// This script is not designed for use when stress testing / checking for GC, and it is recommended to remove for correct results in Profiler.
        /// It is a verbose tool that may generate GC, although I have tried to make it as friendly to GC as I can in practical terms. 
        /// </summary>
        public static TranzmitDebugV2 Instance; 
        
        [BoxGroup("REFERENCES")]
        [Required] public Tranzmit Tranzmit;

        [BoxGroup("SEQUENTIAL LOG OPTIONS")]
        public bool SequentialLogEnabled = true;
        
        [BoxGroup("SEQUENTIAL LOG OPTIONS")]
        public bool SequentialLogErrorsOnly = false;
        
        [BoxGroup("SEQUENTIAL LOG OPTIONS")]
        public bool SequentialLogStorePayloads = true;
        
        [BoxGroup("SEQUENTIAL LOG OPTIONS")]
        public bool SequentialLogValuesOnlyPayloads = true;
        
        [BoxGroup("DEBUG LOG OPTIONS")]
        public bool DebugLogEnabled = true;
        
        [BoxGroup("DEBUG LOG OPTIONS")]
        public bool DebugLogStorePayloads = true;

        [BoxGroup("LOGS")][TableList]
        public List<SequentialLogData> SequentialLog = new List<SequentialLogData>();
        
        [BoxGroup("LOGS")]
        public Dictionary<Tranzmit.EventNames, LogData> DebugLog = new Dictionary<Tranzmit.EventNames, LogData>();

        [ShowOdinSerializedPropertiesInInspector][HideReferenceObjectPicker]
        public class SequentialLogData
        {
            [TableColumnWidth(300, Resizable = false)]
            public SequentialGeneralData General;

            [TableColumnWidth(300, Resizable = false)]
            [HideReferenceObjectPicker][HideDuplicateReferenceBox]
            public List<Object> Subscribers;
            
            [TextArea(9,30)] public string Payload;
        }
        
        [ShowOdinSerializedPropertiesInInspector][HideReferenceObjectPicker]
        public class SequentialGeneralData
        {
            [ProgressBar(0,0, ColorGetter = "StatusColor", DrawValueLabel = false)][HideLabel]
            public int Status;
            public int FrameNumber;
            public object Broadcaster;
            public Tranzmit.EventNames EventName;
            public Tranzmit.DeliveryStatuses DeliveryStatus;
            public GameObject UIElement;
            public List<Tranzmit.Errors> Errors = new List<Tranzmit.Errors>();

            public Color StatusColor()
            {
                if (DeliveryStatus == Tranzmit.DeliveryStatuses.Failed)
                {
                    return Color.red;
                }

                return new Color(0, .8f, 1f);
            }
        }
        
        [ShowOdinSerializedPropertiesInInspector][HideReferenceObjectPicker]
        public class LogData
        {
            [HideReferenceObjectPicker]
            public List<Delegate> Subscribers = new List<Delegate>();
            public LogStats Success = new LogStats();
            public LogStats MissingSource = new LogStats();
            public LogStats MissingPayload = new LogStats();
            public LogStats WrongDataType = new LogStats();
        }

        [HideReferenceObjectPicker]
        public class LogStats
        {
            [Tooltip("The number of time that this type of Event Status has occured.")]
            public int Count;
            
            [Tooltip("Certain malformed events will not have a broadcaster object. We count them here.")]
            public int NullSources;

            [HideReferenceObjectPicker]
            [Tooltip("Payload that was sent by the Event. Malformed Events might result in a Null entry.")]
            public List<object> Sources = new List<object>();
            
            [HideReferenceObjectPicker]
            [Tooltip("Payload that was sent by the Event. Malformed Events might result in a Null entry.")]
            [TextArea(5,100)]
            public List<string> Payloads = new List<string>();
        }
        
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Used instead of Start as using [ExecuteAlways]. Called after recompiles etc
        /// </summary>
        void OnEnable()
        {
            Instance = this;
            SubscribeToTranzmit();
            InitializeDebugLog();
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// This is called by Unity when an Object is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeFromTranzmit();
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// You might have noticed that I am using in-built Events to subscribe to Tranzmit, rather than using Tranzmit to handle them. There are multiple reasons including:
        /// - If Tranzmit breaks, the debugger has a chance of also failing. The number of events are minimal and easy to handle, so no biggy.
        /// - Reduces clutter in Tranzmit Events for the end user, so only their Events will be present.
        /// </summary>
        [ButtonGroup]
        void SubscribeToTranzmit()
        {
            UnsubscribeFromTranzmit();

            if (Tranzmit != null)
            {
                Tranzmit.EventAdded += EventAdded;
                Tranzmit.EventDeleted += EventDeleted;
                Tranzmit.EventSent += EventReceived;
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!\nTranzmit Graph will not be generated");
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Unsubscribes from Tranzmits in-built events that are aimed for use by the debug tool, or any other tool the user might create.
        /// See above for further details on why this approach has been taken.
        /// </summary>
        [ButtonGroup]
        void UnsubscribeFromTranzmit()
        {
            if (Tranzmit != null)
            {
                Tranzmit.EventAdded -= EventAdded;
                Tranzmit.EventDeleted -= EventDeleted;
                Tranzmit.EventSent -= EventReceived;
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!\nTranzmit Graph will not be generated");
            }
        }
        
         // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A built-in Tranzmit Event sent by Tranzmit when an Event has been added to the Tranzmit Events Dictionary.
        /// </summary>
        /// <param name="eventName">The name of the event that was added to Tranzmit.Events</param>
        public void EventAdded(Tranzmit.EventNames eventName)
        {
            // Reserved
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A built-in Tranzmit Event sent by Tranzmit when an Event has been removed from the Tranzmit Events Dictionary.
        /// When received, Tranzmit Debug will auto remove an debug data associated with the Event Name.
        /// </summary>
        /// <param name="eventName">The name of the event that was removed from Tranzmit.Events</param>
        public void EventDeleted(Tranzmit.EventNames eventName)
        {
            // Reserved
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The business end of Tranzmit Debug. When Tranzmit ATTEMPTS to send an event, it also broadcasts information about it. This is sent regardless of whether the requested Tranzmit Event was valid and sent. 
        /// It creates and saves the associated data. Notice that we store the button information in here. This allows for more efficient updating of the Graphview.
        /// </summary>
        /// <param name="source">The object that requested for a Tranzmit event to be sent. Can be Null</param>
        /// <param name="status">A basic 'Success' or 'Failed' output.</param>
        /// <param name="errors">A list of Enums that will show ALL issues (if any) with the Tranzmit Event Send request.</param>
        /// <param name="eventName">The name of the Tranzmit Event.</param>
        /// <param name="requiredDataType">The data type specified by the user when they configured the Tranzmit Event.</param>
        /// <param name="providedDataType">The type of data the user actually attached to the event. Can be Null.</param>
        /// <param name="tranzmitDelegate">The Event/Delegate of the Tranzmit Event.</param>
        public void EventReceived(object payload, object source, Tranzmit.DeliveryStatuses status, List<Tranzmit.Errors> errors, Tranzmit.EventNames eventName, Type requiredDataType, Type providedDataType, Tranzmit.EventData.TranzmitDelegate tranzmitDelegate)
        {
            var logData = DebugLog[eventName];

            if (SequentialLogEnabled)
            {
                GenerateNewSequentialLog(eventName, payload, source, status, errors);
            }

            if (SequentialLogEnabled)
            {
                GenerateDebugLog(logData, payload, source, status, errors, eventName);
            }
        }
        
        // -----------------------------------------------------------------------------------------

        [Button]
        public void InitializeDebugLog()
        {
            if (Tranzmit != null)
            {
                // SEQUENTIAL LOG
                if (SequentialLog == null)
                {
                    SequentialLog = new List<SequentialLogData>();
                }
                else
                {
                    SequentialLog.Clear();
                }
                
                // DEBUG LOG
                if (DebugLog == null)
                {
                    DebugLog = new Dictionary<Tranzmit.EventNames, LogData>();
                }
                else
                {
                    DebugLog.Clear(); 
                }

                foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
                {
                    DebugLog.Add(tranzmitEvent.Key, new LogData(){ Subscribers = tranzmitEvent.Value.GetSubscribers()});
                }
            }
        }
        
        // -----------------------------------------------------------------------------------------

        public void GenerateNewSequentialLog(Tranzmit.EventNames eventName, object payload, object source, Tranzmit.DeliveryStatuses status, List<Tranzmit.Errors> errors)
        {
            if (SequentialLogErrorsOnly == true && status == Tranzmit.DeliveryStatuses.Success)
            {
                return;
            }

            var subscribers = Tranzmit.Events[eventName].GetSubscribers().Select(x => x.Target).ToList();

            var payloadJSON = "";
            
            if (SequentialLogStorePayloads)
            {
                payloadJSON = System.Text.Encoding.Default.GetString(SerializationUtility.SerializeValue(payload, DataFormat.JSON));

                if (SequentialLogValuesOnlyPayloads)
                {
                    payloadJSON = payloadJSON.Substring(payloadJSON.IndexOf(",") + 1);
                    payloadJSON = payloadJSON.Substring(payloadJSON.IndexOf(",") + 1);
                    payloadJSON = payloadJSON.Substring(payloadJSON.IndexOf(",") + 1);
                    payloadJSON = payloadJSON.Replace("    ", "");
                    payloadJSON = payloadJSON.Replace("}", "");
                }
            }

            var logStats = new SequentialGeneralData();
            logStats.FrameNumber = Time.frameCount;
            logStats.Broadcaster = source;
            logStats.EventName = eventName;
            logStats.DeliveryStatus = status;
            logStats.Errors = errors;

            var logData = new SequentialLogData() {General = logStats, Subscribers = subscribers, Payload = payloadJSON};
            
            SequentialLog.Add(logData);
            TranzmitDebugV2UI.Instance.AddNewEvent(logData);
        }
        
        // -----------------------------------------------------------------------------------------

        public void GenerateDebugLog(LogData logData, object payload, object source, Tranzmit.DeliveryStatuses status, List<Tranzmit.Errors> errors, Tranzmit.EventNames eventName)
        {
            // Success
            if (status == Tranzmit.DeliveryStatuses.Success)
            {
                logData.Success.Count++;
                UpdateBroadcaster(source, logData.Success);
                
                if (DebugLogStorePayloads)
                {
                    var result = SerializationUtility.SerializeValue(payload, DataFormat.JSON);
                    logData.Success.Payloads.Add(System.Text.Encoding.Default.GetString(result));
                }
            }
            
            // Failed: NOTE...Error Type "NoSubscribers" will not trigger Debug.
            if (status == Tranzmit.DeliveryStatuses.Failed)
            {
                foreach (var error in errors)
                {
                    if (error == Tranzmit.Errors.MissingPayload)
                    {
                        logData.MissingPayload.Count++;
                        UpdateBroadcaster(source, logData.MissingPayload);

                        if (DebugLogStorePayloads)
                        {
                            var result = SerializationUtility.SerializeValue(payload, DataFormat.JSON);
                            logData.MissingPayload.Payloads.Add(System.Text.Encoding.Default.GetString(result));
                        }     
                    }
                    
                    if (error == Tranzmit.Errors.MissingSource)
                    {
                        logData.MissingSource.Count++;
                        UpdateBroadcaster(source, logData.MissingSource);

                        if (DebugLogStorePayloads)
                        {
                            var result = SerializationUtility.SerializeValue(payload, DataFormat.JSON);
                            logData.MissingSource.Payloads.Add(System.Text.Encoding.Default.GetString(result));
                        }        
                    }
                    
                    if (error == Tranzmit.Errors.WrongDataType)
                    {
                        logData.WrongDataType.Count++;
                        UpdateBroadcaster(source, logData.WrongDataType);

                        if (DebugLogStorePayloads)
                        {
                            var result = SerializationUtility.SerializeValue(payload, DataFormat.JSON);
                            logData.WrongDataType.Payloads.Add(System.Text.Encoding.Default.GetString(result));
                        }  
                    }
                }
            }
        }
        
        // -----------------------------------------------------------------------------------------

        public void UpdateBroadcaster(object source, LogStats logStats)
        {
            if (source != null)
            {
                var broadcaster = logStats.Sources.Find(x => x == source);

                if (broadcaster == null)
                {
                    logStats.Sources.Add(source);
                }
            }
            else
            {
                logStats.NullSources++;
            }
        }
        
        // -----------------------------------------------------------------------------------------

        public int GetErrorsCount()
        {
            int count = 0;
            
            foreach(KeyValuePair<Tranzmit.EventNames, LogData> entry in DebugLog)
            {
                count += entry.Value.MissingPayload.Count;
                count += entry.Value.MissingSource.Count;
                count += entry.Value.WrongDataType.Count;
            }

            return count;
        }
    }
}