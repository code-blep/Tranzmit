using System;
using System.Collections.Generic;

namespace Blep.Tranzmit
{
    /// <summary>
    /// We could use Tranzmit for these events. However, if Tranzmit breaks for some reason, it might break the whole debug process.
    /// </summary>

    public partial class Tranzmit
    {
        public delegate void EventCreatedDelegate(EventNames eventName);
        public event EventCreatedDelegate EventAdded;

        /// <summary>
        /// Called when a new Event has been edded to Tranzmit Events.
        /// </summary>
        /// <param name="eventName">The name of the new event added.</param>
        public void Broadcast_Event_Added(EventNames eventName)
        {
            // Subscribers?
            if (EventAdded != null)
            {
                EventAdded(eventName);
            }
        }

        // -----------------------------------------------------------------------------------------

        public delegate void EventDeletedDelegate(EventNames eventName);
        public event EventDeletedDelegate EventDeleted;

        /// <summary>
        /// Called when a new Event has been deleted from Tranzmit Events.
        /// </summary>
        /// <param name="eventName">The name of the new event deleted.</param>
        public void Broadcast_Event_Deleted(EventNames eventName)
        {
            // Subscribers?
            if (EventDeleted != null)
            {
                EventDeleted(eventName);
            }
        }

        // -----------------------------------------------------------------------------------------

        public delegate void EventSentDelegate(object payload, object source, DeliveryStatuses status, List<Errors> errorTypes, EventNames eventName, Type requiredDataType, Type providedDataType, EventData.TranzmitDelegate tranzmitDelegate);
        public event EventSentDelegate EventSent;

        /// <summary>
        /// Called when an Event has been sent by an Object via Tranzmit. The information here will provide an insight into whether the send was successful or not. 
        /// </summary>
        /// <param name="eventName">The name of the Event sent.</param>
        public void Broadcast_Event_Sent(object payload, object source, DeliveryStatuses status, List<Errors> errors, EventNames eventName, Type requiredDataType, Type providedDataType, EventData.TranzmitDelegate tranzmitDelegate)
        {
            // Subscribers?
            if (tranzmitDelegate != null && EventSent != null)
            {
                EventSent(payload, source, status, errors, eventName, requiredDataType, providedDataType, tranzmitDelegate);
            }
        }

        // -----------------------------------------------------------------------------------------

        public delegate void TranzmitDebugResetDelegate();
        public event TranzmitDebugResetDelegate TranzmitDebugReset;

        /// <summary>
        /// This event is triggered when Tranzmit Debug clears it's debug data, and allows other modules to react accordingly. For example Tranzmit Graph uses this to refresh displayed data.
        /// </summary>
        public void Broadcast_Tranzmit_Debug_Reset()
        {
            // Subscribers?
            if (TranzmitDebugReset != null)
            {
                TranzmitDebugReset();
            }
        }

        // -----------------------------------------------------------------------------------------

        public delegate void TranzmitDebugLogUpdateDelegate(TranzmitDebug.LogEntry updatedLog);
        public event TranzmitDebugLogUpdateDelegate TranzmitDebugLogUpdated;

        /// <summary>
        /// Used to notify when a Debug Log has been updated, and makes for efficient direct updating of information, rather than trying to locate an changes in debug data. Used by Tranzmit Graph.
        /// </summary>
        /// <param name="updatedLog">The debug log that has been updated</param>
        public void Broadcast_Tranzmit_Log_Update(TranzmitDebug.LogEntry updatedLog)
        {
            // Subscribers?
            if (TranzmitDebugLogUpdated != null)
            {
                TranzmitDebugLogUpdated(updatedLog);
            }
        }
    }
}
