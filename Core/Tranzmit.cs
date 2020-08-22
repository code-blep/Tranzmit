using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using System.Collections;

/// <summary>
/// - WORKS IN EDITOR and RUNTIME
/// 
/// Features:
/// - Error checking and handling at all stages.
/// - Virtually Zero GC.
/// - Debug Module to allow tracing of Tranzmit Events that are not formed correctly.
/// - Unity Graphview Editor Window to visualize the Events in Realtime.
/// - Typesafe implementation of Tranzmit Event names.
/// - Passes any Type of Object
/// 
/// - When working within the editor be aware that all sorts of Unity serialzation and compile events can break the Subscriptions! There is only so much that I can do to defend against this ;)
///  
/// </summary>

namespace Blep.Tranzmit
{
    /// <summary>
    /// This is the main component of Tranzmit. It will operate independant of the Debug Module and Graphview Ediotr window.
    /// </summary>
    [ExecuteAlways]
    public partial class Tranzmit : SerializedMonoBehaviour
    {
        /// <summary>
        /// Used by the Tranzmit event we are trying to send.
        /// </summary>
        public enum DeliveryStatuses
        {
            Success,
            Failed
        }

        /// <summary>
        /// The types of errors being handled / caught by Tranzmit. If any of these errors occur, the Tranzmit event will not be sent.
        /// </summary>
        public enum Errors
        {
            MissingSource,
            NoSubscribers,
            MissingPayload,
            MissingDataType,
            WrongDataType
        }

        /// <summary>
        /// Handles the process of adding a new Event 
        /// </summary>
        [BoxGroup]
        [HideReferenceObjectPicker]
        [HideLabel]
        public AddNewEventToEventsData AddEvent = new AddNewEventToEventsData();

        /// <summary>
        /// This is where all user created Tranzmit Events are stored.
        /// </summary>
        [Tooltip("The Created Events. Subscribers respond to events in this Dictionary.")]
        [DictionaryDrawerSettings(IsReadOnly = true)]
        [Title("EVENTS", "The core of Tranzmit! All your events are here. To create a new Event Type, edit the EventNames.cs script.")]
        [PropertySpace(SpaceBefore = 20, SpaceAfter = 20)]
        public Dictionary<EventNames, EventData> Events = new Dictionary<EventNames, EventData>();


        // Most Data you require is in here.
        [Serializable]
        [HideReferenceObjectPicker]
        public class EventData
        {
            // 1 - Define the Delegate
            public delegate void TranzmitDelegate(object source, object payload);

            [ReadOnly]
            public InfoData Info = new InfoData();

            public class InfoData
            {
                // Handy reference to the Tranzmit Instance that this Event resides in.
                public Tranzmit Tranzmit;

                [Required]
                public EventNames EventName;

                [Required]
                public Type DataType;
            }

            // 2 - Define The Event based on the delegate
            public event TranzmitDelegate TranzmitEvent;


            /// <summary>
            /// Send the Event. Handles all error checking before we try to send the Event.
            /// </summary>
            /// <param name="source">The object that is sending a Tranzmit Event</param>
            /// <param name="payload">The object that contains the data being sent with the Tranzmit event</param>
            // 3 - Send The Event
            public void Send(object source, object payload)
            {
                List<Errors> Errors = new List<Errors>();
                Type dataType = null;

                if (source == null)
                    Errors.Add(Tranzmit.Errors.MissingSource);

                if (TranzmitEvent == null)
                    Errors.Add(Tranzmit.Errors.NoSubscribers);

                if (payload == null)
                {
                    Errors.Add(Tranzmit.Errors.MissingPayload);
                }
                else
                {
                    dataType = payload.GetType();
                    if (dataType != Info.DataType)
                        Errors.Add(Tranzmit.Errors.WrongDataType);
                }

                if(dataType == null)
                    Errors.Add(Tranzmit.Errors.MissingDataType);

                DeliveryStatuses delieveryStatus;

                if (Errors.Count == 0)
                {
                    delieveryStatus = DeliveryStatuses.Success;
                }
                else
                {
                    delieveryStatus = DeliveryStatuses.Failed;
                }

                if (delieveryStatus == DeliveryStatuses.Success)
                {
                    TranzmitEvent.Invoke(source, payload);
                }

                Info.Tranzmit.Broadcast_Event_Sent(source, delieveryStatus, Errors, Info.EventName, Info.DataType, dataType, TranzmitEvent);
            }

            /// <summary>
            /// Changes the Data Type of the specified Event
            /// </summary>
            /// <param name="type"></param>
            [Button]
            public void ChangeDataType(Type type)
            {
                if (type != null)
                {
                    Info.DataType = type;
                }
            }

            /// <summary>
            /// Removes all subscribers from the specified Event
            /// </summary>
            [ButtonGroup, GUIColor(1f, 0.5f, 0.0f)]
            public void RemoveAllSubscribers()
            {
                TranzmitEvent = null;
            }

            /// <summary>
            /// Deletes the specified Event
            /// </summary>
            [ButtonGroup, GUIColor(0.8f, 0.1f, 0.1f)]
            public void DeleteEvent()
            {
                IEnumerator Delete(EventNames EventName)
                {
                    // If we don't wait, deleting Causes an error in Odin when deleting anything other than last entry in dictionary.
                    yield return null;

                    Info.Tranzmit.Events.Remove(EventName);
                    Info.Tranzmit.Broadcast_Event_Deleted(EventName);

                    // ERRORS: InvalidOperationException: Sequence contains no elements
                    // When removing EVENT after ENUM EVENT TYPE HAS BEEN DELETED!

                    var newList = Info.Tranzmit.AddEvent.GenerateListOfUnusedEventNames();

                    if (newList.Count > 0)
                    {
                        Info.Tranzmit.AddEvent.EventName = Info.Tranzmit.AddEvent.GenerateListOfUnusedEventNames().First();
                    }
                }

                Info.Tranzmit.StartCoroutine(Delete(Info.EventName));
            }

            /// <summary>
            /// Fetches the Subscribers (Deleagtes) 'attached' to the Specified Event. This is the only way I can find to get delgates directly from this class.
            /// </summary>
            /// <returns>Event Delegates</returns>
            public List<Delegate> GetSubscribers()
            {
                List<Delegate> subscribers = new List<Delegate>();
                
                if (TranzmitEvent != null)
                {
                    foreach (Delegate subscriber in TranzmitEvent.GetInvocationList())
                    {
                        subscribers.Add(subscriber);
                    }
                }

                return subscribers;
            }
        }

        // -----------------------------------------------------------------------------------------

        public class AddNewEventToEventsData
        {
            // Used for reference to parent class instance.
            private Tranzmit _Tranzmit;
            public AddNewEventToEventsData(Tranzmit _Tranzmit = null)
            {
                this._Tranzmit = _Tranzmit;
            }


            [ValueDropdown(nameof(GenerateListOfUnusedEventNames), SortDropdownItems = true, DrawDropdownForListElements = true)]
            public EventNames EventName;
            public Type DataType = null;


            /// <summary>
            /// Adds an Event to the Events Dictionary
            /// </summary>
            [ButtonGroup]
            [Button("Add Event", ButtonStyle.Box, Expanded = true), GUIColor(0f, 1f, 0f)]
            public void Add()
            {
                if (!_Tranzmit.CheckEventIsAllocated(EventName))
                {
                    if (DataType != null)
                    {
                        var newEventData = new EventData();

                        newEventData.Info = new EventData.InfoData();
                        newEventData.Info.Tranzmit = _Tranzmit;
                        newEventData.Info.EventName = EventName;
                        newEventData.Info.DataType = DataType;

                        _Tranzmit.Events.Add(EventName, newEventData);
               
                        _Tranzmit.Broadcast_Event_Added(EventName);

                        if (GenerateListOfUnusedEventNames().Count() != 0)
                        {
                            EventName = GenerateListOfUnusedEventNames().First();
                        }

                    }
                    else
                    {
                        Debug.LogError(TranzmitDebug.DebugTypes.Error + " - No Data Type has been specified when trying to create the Event. Aborted.");
                    }
                }
                else
                {
                    Debug.LogError(TranzmitDebug.DebugTypes.Error + " - An entry to the Event Type of " + EventName + "Already exists! Aborted.");
                }
            }


            /// <summary>
            /// Used by ODIN - [ValueDropdown(nameof(GetAvailableList)] - For generating a list of Unused Event Types
            /// </summary>
            /// <returns>The list used for ODIN Event Names drop down</returns>
            public List<EventNames> GenerateListOfUnusedEventNames()
            {
                List<EventNames> result = new List<EventNames>();

                if (_Tranzmit != null && _Tranzmit.Events != null && _Tranzmit.Events.Count > 0)
                {
                    foreach (EventNames val in Enum.GetValues(typeof(EventNames)))
                    {
                        if (_Tranzmit.CheckEventIsAllocated(val) == false)
                        {
                            result.Add(val);
                        }
                    }
                }
                else
                {
                    foreach (EventNames val in Enum.GetValues(typeof(EventNames)))
                    {
                        result.Add(val);
                    }
                }

                return result;
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// This script is using [ExecuteAlways] as such OnEnable is called after recompiles etc. This allow for use with UNity Editor as well as Runtime Compile.
        /// </summary>
        private void OnEnable()
        {
            // Added here so that we can pass in the reference to Tranzmit (this)
            AddEvent = new AddNewEventToEventsData(this);

            if (AddEvent.GenerateListOfUnusedEventNames().Count() != 0)
            {
                AddEvent.EventName = AddEvent.GenerateListOfUnusedEventNames().First();
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Fetches a list of Subscriber Objects in the Event that was sent
        /// </summary>
        /// <param name="TranzmitEvent">The event which we are querying for Subscribers</param>
        /// <returns>List of Subscriber Objects</returns>
        public List<object> GetSubscribersInSentEvent(EventData.TranzmitDelegate TranzmitEvent)
        {
            List<object> result = new List<object>();

            if (TranzmitEvent != null)
            {
                foreach (Delegate subscriber in TranzmitEvent.GetInvocationList())
                {
                    result.Add(subscriber.Target);
                }
            }

            return result;
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Checks if the provided Event Name exists within the code.
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns>True if exists</returns>
        public bool EventNameExists(EventNames eventName)
        {
            if(Enum.IsDefined(typeof(EventNames), eventName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Checks if the provided Event is being used in the Events Dictionary.
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns>True if the Event Name is found</returns>
        public bool CheckEventIsAllocated(EventNames eventName)
        {
            if (Events != null && Events.Count != 0)
            {
                if (Events.ContainsKey(eventName))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A handy function that makes Subscribing (most common use) to an event easy, as all checking is handled here. Massively reduces code elsewhere.
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns>The Valid Event if it exists</returns>
        public bool CheckEventIsAvailable(EventNames eventName)
        {
            if (Events != null && Events.Count != 0)
            {
                if (CheckEventIsAllocated(eventName))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Recommended way to use Tranzmit to send Events. Various checks are carried out from this point forward.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="source">The object that is broadcasting this event.</param>
        /// <param name="payload">The data payload to send.</param>
        public void BroadcastEvent(EventNames eventName, object source, object payload)
        {
            if (CheckEventIsAllocated(eventName))
            {
                // BROADCAST!
                Events[eventName].Send(source, payload);
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!");
            }
        }
    }
}