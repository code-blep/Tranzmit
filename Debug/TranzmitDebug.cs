using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using UnityEngine.UIElements;
using System.Linq;

namespace Blep.Tranzmit
{
    /// <summary>
    /// Used for testing and general debugging.
    /// This script is not designed for use when stress testing / checking for GC, and it is recommened to remove for correct results in Profiler.
    /// It is a verbose tool that may generate GC, although I have tried to make it as friendly to GC as I can in practical terms. 
    /// 
    /// Event grouping order is: [SUCCSESS or FAILED][EVENT NAME][ERROR LIST][BROADCASTERS && SUBSCRIBERS]
    /// 
    /// </summary>

    [ExecuteAlways]
    public class TranzmitDebug : SerializedMonoBehaviour
    {
        [Required]
        public Tranzmit Tranzmit;

        [FoldoutGroup("Events")]
        [ReadOnly]
        public Dictionary<Tranzmit.EventNames, List<LogEntry>> Success = new Dictionary<Tranzmit.EventNames, List<LogEntry>>();

        [FoldoutGroup("Events")]
        [ReadOnly]
        public Dictionary<Tranzmit.EventNames, List<LogEntry>> Failed = new Dictionary<Tranzmit.EventNames, List<LogEntry>>();
        public enum DebugTypes { Info, Warning, Error }

        [ShowOdinSerializedPropertiesInInspector]
        public class LogEntry
        {    
            public Tranzmit.EventNames TranzmitEvent;
            public Tranzmit.DeliveryStatuses Status;

            // This errors list is key to organising and grouping stored events
            [ListDrawerSettings(Expanded = true)]
            public List<Tranzmit.Errors> Errors;

            public Type RequiredDataType;
            public List<Type> ProvidedDataTypes = new List<Type>();
            public Dictionary<object, ButtonData> Broadcasters = new Dictionary<object, ButtonData>();
            public Dictionary<object, ButtonData> Subscribers = new Dictionary<object, ButtonData>();
        }

        [Serializable]
        public class ButtonData
        {
            public int EventCount = 0;

            //[HideInInspector]
            public Button GraphButton;
        }

        // Used for graceful allocation in Failed Log. Otherwise a new button etc will be generated for each Event without a source. 
        // Pointless creating new ones as we don't know the source! ;)
        [HideInInspector]
        public object NullSourceObject = new object();

        // -----------------------------------------------------------------------------------------


        /// <summary>
        /// Used instead of Start as using [ExecuteAlways]. Called after recompiles etc
        /// </summary>
        void OnEnable()
        {
            SubscribeToTranzmit();
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
        /// You might have noticed that I am using in-built Events to subscribe to Tranzmit, rather than using Tranzmit to handle them. There are multiple reasons inclduding:
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
                Tranzmit.EventSent += EventSent;
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
        void UnsubscribeFromTranzmit()
        {
            if (Tranzmit != null)
            {
                Tranzmit.EventAdded -= EventAdded;
                Tranzmit.EventDeleted -= EventDeleted;
                Tranzmit.EventSent -= EventSent;
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!\nTranzmit Graph will not be generated");
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Clears all collected debug data
        /// </summary>
        [ButtonGroup]
        public void Reset()
        {
            Success = new Dictionary<Tranzmit.EventNames, List<LogEntry>> ();
            Failed = new Dictionary<Tranzmit.EventNames, List<LogEntry>> ();

            if (Tranzmit != null)
            {
                Tranzmit.Broadcast_Tranzmit_Debug_Reset();
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A built-in Tranzmit Event sent by Tranzmit when an Event has been added to the Tranzmit Events Dictionary.
        /// </summary>
        /// <param name="eventName">The name of the event that was added to Tranzmit.Events</param>
        public void EventAdded(Tranzmit.EventNames eventName)
        {
            // Currently Unused.
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A built-in Tranzmit Event sent by Tranzmit when an Event has been removed from the Tranzmit Events Dictionary.
        /// When recieved, Tranzmit Debug will auto remove an debug data associated with the Event Name.
        /// </summary>
        /// <param name="eventName">The name of the event that was removed from Tranzmit.Events</param>
        public void EventDeleted(Tranzmit.EventNames eventName)
        {
            Remove_Debug_Data_For_Deleted_Event_Type_ENUM();
            Remove_Debug_Data_For_Deleted_Event_Type_In_TRANZMIT();
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The business end of Tranzmit Debug. When Tranzmit ATTEMPTS to send an event, it also broadacasts information about it. This is sent regadless of whether the requested Tranzmit Event was valid and sent. 
        /// It creates and saves the associated data. Notice that we store the button information in here. This allows for more eficient updating of the Graphview.
        /// </summary>
        /// <param name="source">The object that requested for a Tranzmit event to be sent. Can be Null</param>
        /// <param name="status">A basic 'Success' or 'Failed' output.</param>
        /// <param name="errors">A list of Enums that will show ALL issues (if any) with the Tranzmit Event Send request.</param>
        /// <param name="eventName">The name of the Tranzmit Event.</param>
        /// <param name="requiredDataType">The data type specified by the user when they configured the Tranzmit Event.</param>
        /// <param name="providedDataType">The type of data the user actually attached to the event. Can be Null.</param>
        /// <param name="tranzmitDelegate">The Event/Delegate of the Tranzmit Event.</param>
        public void EventSent(object source, Tranzmit.DeliveryStatuses status, List<Tranzmit.Errors> errors, Tranzmit.EventNames eventName, Type requiredDataType, Type providedDataType, Tranzmit.EventData.TranzmitDelegate tranzmitDelegate)
        {
            // This will be assigned to later on, and allows for cleaner code and less if statements etc
            Dictionary<Tranzmit.EventNames, List<LogEntry>> saveLocation = null;

            // Allocate the Dicitonary to use
            if (status == Tranzmit.DeliveryStatuses.Success)
            {
                saveLocation = Success;
            }
            else
            {
                saveLocation = Failed;
            }

            // Deal with potential NULL source
            if (source == null)
            {
                // We have to give it something!
                source = NullSourceObject;
            }

            // CREATE NEW EVENT LOGS FIRST - WILL APPLY TOTAL TO SUBSCRIBERS AND BROADCASTERS AFTER (BELOW)
            // NEW ENTRY INTO LIST - AS EVENT NAME NOT FOUND
            if (!saveLocation.ContainsKey(eventName))
            {
                var newLogEntry = GenerateLogEntry(source, status, errors, eventName, requiredDataType, providedDataType, tranzmitDelegate);
                saveLocation.Add(eventName, new List<LogEntry>() { newLogEntry });

                Tranzmit.Broadcast_Tranzmit_Log_Update(newLogEntry);
            }
            else // EXISTING EVENT - We now Compare the ERRORS list - If no match found create a new entry based on ERRRORS
            {
                // Null if not match found
                var foundLog = ReturnFirstLogEntryWithMatchingErrorsList(saveLocation[eventName], errors);

                // NEW ERRORS LIST
                if (foundLog == null)
                {
                    var newLogEntry = GenerateLogEntry(source, status, errors, eventName, requiredDataType, providedDataType, tranzmitDelegate);
                    saveLocation[eventName].Add(newLogEntry);
                    Tranzmit.Broadcast_Tranzmit_Log_Update(newLogEntry);
                }
            }

            // SUBSCRIBERS - We update subscribers in case new ones have been added through code at some point.
            //...and also add to the count if they already exist
            var currentSubscribers = Tranzmit.GetSubscribersInSentEvent(tranzmitDelegate);

            // SUBSCRIBERS
            if (saveLocation.ContainsKey(eventName))
            {
                var foundLog = ReturnFirstLogEntryWithMatchingErrorsList(saveLocation[eventName], errors);

                if (foundLog != null)
                {
                    foreach (object o in currentSubscribers)
                    {
                        if (foundLog.Subscribers.ContainsKey(o))
                        {
                            foundLog.Subscribers[o].EventCount++;
                        }
                        else
                        {
                            foundLog.Subscribers.Add(o, new ButtonData() { EventCount = 0});
                        }
                    }

                    Tranzmit.Broadcast_Tranzmit_Log_Update(foundLog);
                }
            }

            // BROADCASTERS
            if (saveLocation.ContainsKey(eventName))
            {
                var foundLog = ReturnFirstLogEntryWithMatchingErrorsList(saveLocation[eventName], errors);

                if (foundLog != null)
                {
                    if (foundLog.Broadcasters.ContainsKey(source))
                    {
                        foundLog.Broadcasters[source].EventCount++;
                    }
                    else
                    {
                        foundLog.Broadcasters.Add(source, new ButtonData() { EventCount = 1});
                    }

                    Tranzmit.Broadcast_Tranzmit_Log_Update(foundLog);
                }
            }
        }


        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Seperate function to deal with the actual creation of the Debug Log entry.
        /// </summary>
        /// <returns>The new generated log entry.</returns>
        public LogEntry GenerateLogEntry(object source, Tranzmit.DeliveryStatuses status, List<Tranzmit.Errors> errors, Tranzmit.EventNames eventName, Type requiredDataType, Type providedDataType, Tranzmit.EventData.TranzmitDelegate tranzmitDelegate)
        {
            var entry = new LogEntry();
            entry.Status = status;
            entry.Errors = errors;
            entry.TranzmitEvent = eventName;
            entry.RequiredDataType = requiredDataType;
            entry.ProvidedDataTypes.Add(providedDataType);
            entry.Broadcasters.Add(source, new ButtonData() { EventCount = 0 });

            var subscribers = Tranzmit.GetSubscribersInSentEvent(tranzmitDelegate);

            foreach (object subscriber in subscribers)
            {
                entry.Subscribers.Add(subscriber, new ButtonData());
            }

            return entry;
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Handy function to find a matching error list
        /// </summary>
        /// <returns>Either the log entry with matching error list OR NUll</returns>
        public LogEntry ReturnFirstLogEntryWithMatchingErrorsList(List<LogEntry> logs, List<Tranzmit.Errors> errors)
        {
            var found = new List<LogEntry>();

            foreach (LogEntry log in logs)
            {
                if (log != null)
                {
                    var areEquivalent = (log.Errors.Count == errors.Count) && !log.Errors.Except(errors).Any();

                    if (areEquivalent == true)
                    {
                        found.Add(log);
                    }
                }
            }

            if(found.Count > 1)
            {
                Debug.LogError($"DEFENSIVE WARNING: More than 1 match when searching for matching Errors List were found! This should not have happened. Returing first entry in list!");
            }
            
            if(found.Count > 0)
            {
                return found[0];
            }
            else
            {
                return null;
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Used for when an Event has been removed from the EventNames Enums, and is called by a subscriber to Tranzmit buil-in event system.
        /// </summary>
        public void Remove_Debug_Data_For_Deleted_Event_Type_ENUM()
        {
            // SUCCESS DATA
            var deleteSentItems = new List<Tranzmit.EventNames>();

            foreach (Tranzmit.EventNames eventName in Success.Keys)
            {
                if (Tranzmit.EventNameExists(eventName))
                {
                    deleteSentItems.Add(eventName);
                }
            }

            foreach (Tranzmit.EventNames deleteKey in deleteSentItems)
            {
                Success.Remove(deleteKey);
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Used for when an Event has been removed from the Tranzmit Events Dictionary, and is called by a subscriber to Tranzmit buil-in event system.
        /// </summary>
        public void Remove_Debug_Data_For_Deleted_Event_Type_In_TRANZMIT()
        {
            // SENT DATA
            var deleteSentItems = new List<Tranzmit.EventNames>();
            foreach (Tranzmit.EventNames eventName in Success.Keys)
            {
                // If Event Type does not exist
                if (Tranzmit.CheckEventIsAllocated(eventName))
                {
                    deleteSentItems.Add(eventName);
                }
            }

            foreach (Tranzmit.EventNames deleteKey in deleteSentItems)
            {
                Success.Remove(deleteKey);
            }
        }
    }
}
