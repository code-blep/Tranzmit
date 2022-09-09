using UnityEngine;
using Sirenix.OdinInspector;
using Blep.Tranzmit;

/// <summary>
/// 
/// - This is an example script, showing how to subscribe to an event in Tranzmit. For example:
///      
/// - Don't forget to Unsubsribe from events when destroying an object! Otherwise you will suffer from memory leaks. This is standard coding practice, and not a limitation of this code.
///
/// </summary>

namespace Blep.Tranzmit.Demo
{
    // Make useable in the editor
    [ExecuteAlways]
    public class Subscriber : MonoBehaviour
    {
        [Required]
        public Tranzmit Tranzmit;

        [Tooltip("Used by the demo scene UI")]
        public EventsSentUI EventsSentUI;

        [Tooltip("Suppress output to the Output field")]
        public bool Silent = false;

        [Tooltip("How many times we subscribe to all 3 events setup in the test scene")]
        public int SubscribeIterations = 1;

        [TextArea(3, 30)]
        public string Output;

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Used instead of Start as using [ExecuteAlways]. Called after recompiles etc
        /// </summary>
        void OnEnable()
        {
            Subscribe();
            Output = "";
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// This is called by Unity when an Object is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            // Unsubscribe fromt the events. This is important to prevent memory leakage.
            Unsubscribe();
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Demonstrates on how to Subscribe to a Tranzmit Event.
        /// </summary>
        [ButtonGroup]
        public void Subscribe()
        {
            // Yep! Playing it safe, and try to keep things tidy.
            Unsubscribe();

            if (Tranzmit != null)
            {
                // Yes, you can subscribe in the same way, multiple times!
                for (int i = 0; i < SubscribeIterations; i++)
                {
                    // Rather than blindly trying to access the Tranzmit Event, I have created a helper to check to see if it will be valid.
                    // I recommend using this approach.
                    if (Tranzmit.CheckEventIsAvailable(Tranzmit.EventNames.PlayerStats))
                    {
                        Tranzmit.Events[Tranzmit.EventNames.PlayerStats].TranzmitEvent += PlayerStatsAction;
                    }

                    if (Tranzmit.CheckEventIsAvailable(Tranzmit.EventNames.Damage))
                    {
                        Tranzmit.Events[Tranzmit.EventNames.Damage].TranzmitEvent += DamageAction;
                    }

                    if (Tranzmit.CheckEventIsAvailable(Tranzmit.EventNames.SecretFound))
                    {
                        Tranzmit.Events[Tranzmit.EventNames.SecretFound].TranzmitEvent += SecretFoundAction;
                    }
                }
            }
            else
            {
                Debug.LogWarning("No Instance of Tranzmit has been found!");
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Demonstrates on how to Unsubscribe from a Tranzmit Event.
        /// </summary>
        [ButtonGroup]
        void Unsubscribe()
        {
            if (Tranzmit != null)
            {
                for (int i = 0; i < SubscribeIterations; i++)
                {
                    // Rather than blindly trying to access the Tranzmit Event, I have created a helper to check to see if it will be valid.
                    // I recommend using this approach.
                    if (Tranzmit.CheckEventIsAvailable(Tranzmit.EventNames.PlayerStats))
                    {
                        Tranzmit.Events[Tranzmit.EventNames.PlayerStats].TranzmitEvent -= PlayerStatsAction;
                    }

                    if (Tranzmit.CheckEventIsAvailable(Tranzmit.EventNames.Damage))
                    {
                        Tranzmit.Events[Tranzmit.EventNames.Damage].TranzmitEvent -= DamageAction;
                    }

                    if (Tranzmit.CheckEventIsAvailable(Tranzmit.EventNames.SecretFound))
                    {
                        Tranzmit.Events[Tranzmit.EventNames.SecretFound].TranzmitEvent -= SecretFoundAction;
                    }
                }
            }
        }

        // -----------------------------------------------------------------------------------------

        [ButtonGroup]
        private void Reset()
        {
            Output = "";
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Test function for when the specified Tranzmit Event has occured.
        /// </summary>
        /// <param name="source">The Object that is using Tranzmit to send an Event.</param>
        /// <param name="payload">The data being passed through Tranzmit to the subscribers of the event.</param>
        void PlayerStatsAction(object source, object payload)
        {
            if (EventsSentUI != null)
            {
                EventsSentUI.Total++;
            }

            var data = (Broadcaster.PlayerStatsData)payload;

            if (Silent == false)
                Output += "\n\n" + this.GetType().Name + "\n" + data.Name + " | " + data.Health;
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Test function for when the specified Tranzmit Event has occured.
        /// </summary>
        /// <param name="source">The Object that is using Tranzmit to send an Event.</param>
        /// <param name="payload">The data being passed through Tranzmit to the subscribers of the event.</param>
        void DamageAction(object source, object payload)
        {
            if (EventsSentUI != null)
            {
                EventsSentUI.Total++;
            }

            var data = (int)payload;

            if (Silent == false)
                Output += "\n\n" + GetType().Name + "\n" + data;
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Test function for when the specified Tranzmit Event has occured.
        /// </summary>
        /// <param name="source">The Object that is using Tranzmit to send an Event.</param>
        /// <param name="payload">The data being passed through Tranzmit to the subscribers of the event.</param>
        void SecretFoundAction(object source, object payload)
        {
            if (EventsSentUI != null)
            {
                EventsSentUI.Total++;
            }

            var data = (bool)payload;

            if (Silent == false)
                Output += "\n\n" + this.GetType().Name + " | " + data;
        }
    }
}