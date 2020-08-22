using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using Blep.Tranzmit;
using System.Collections.Generic;

/// <summary>
/// Example script that shows how you can broadcast an Event:
/// </summary>

namespace Blep.Tranzmit.Demo
{
    public class Broadcaster : MonoBehaviour
    {
        [FoldoutGroup("Settings")]
        [Required]
        public Tranzmit Tranzmit;

        [FoldoutGroup("Settings")]
        [Tooltip("Only works at Runtime. When true will cause a constant stream of Tranzmit Events to be sent.")]
        public bool StressTest = false;

        [FoldoutGroup("Settings")]
        [Tooltip("Multiplies the amount of broadcasts sent.")]
        public int StressMultiplier = 10;

        [FoldoutGroup("Settings")]
        [Tooltip("Delay between broadcasts.")]
        public float SendDelay = 1;

        [FoldoutGroup("Settings")]
        public PlayerStatsData PlayerStatsTestData;

#pragma warning disable 0649
        private DummyClassData DummyClass;
#pragma warning restore 0649

        [Serializable]
        public struct PlayerStatsData
        {
            public string Name;
            public float Health;
        }

        public struct DummyClassData
        {
            // Used to generate Event Data Type Mismatch Errors for Debugging.
        }

        // -----------------------------------------------------------------------------------------

        private void Start()
        {
            StartCoroutine(Main());
        }

        // -----------------------------------------------------------------------------------------

        IEnumerator Main()
        {
            while (true)
            {
                if (StressTest)
                {
                    yield return new WaitForSeconds(SendDelay);

                    for (int i = 0; i < StressMultiplier; i++)
                    {
                        // The SendPlayerStats example is using an alrerady created instance of SendPlayerStats. This is to reduce Garbage collection when load testing as it could potentially distort the results.
                        SendPlayerStats(Tranzmit.EventNames.PlayerStats);
                        SendDamage(10, Tranzmit.EventNames.Damage);
                        SendSecretFound(true, Tranzmit.EventNames.SecretFound);
                    }
                }

                yield return null;
            }
        }

        // -----------------------------------------------------------------------------------------


        [Button("Send Player Stats"), GUIColor(0.5f, 0.5f, 1)]
        [PropertySpace(20, 10)]
        public void SendPlayerStats(Tranzmit.EventNames eventName = Tranzmit.EventNames.PlayerStats)
        {
            if (Tranzmit != null)
            {
                // BROADCAST!
                Tranzmit.BroadcastEvent(eventName, this, PlayerStatsTestData);
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!");
            }
        }

        // -----------------------------------------------------------------------------------------

        [Button("Send Damage"), GUIColor(0.5f, 0.5f, 1)]
        [PropertySpace(10, 10)]
        public void SendDamage(float damage = 42, Tranzmit.EventNames eventName = Tranzmit.EventNames.Damage)
        {
            if (Tranzmit != null)
            {
                // BROADCAST!
                Tranzmit.BroadcastEvent(eventName, this, damage);
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!");
            }
        }

        // -----------------------------------------------------------------------------------------

        [Button("Send Secret Found"), GUIColor(0.5f, 0.5f, 1)]
        [PropertySpace(10, 10)]
        public void SendSecretFound(bool SecretFound = true, Tranzmit.EventNames eventName = Tranzmit.EventNames.SecretFound)
        {
            if (Tranzmit != null)
            {
                // BROADCAST!
                Tranzmit.BroadcastEvent(eventName, this, SecretFound);
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!");
            }
        }

        // -----------------------------------------------------------------------------------------


        [Button("Generate Wrong Data Type Errors"), GUIColor(0.5f, 0.5f, 1)]
        public void Test1()
        {
            foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
            {
                // BROADCAST!
                tranzmitEvent.Value.Send(this, DummyClass);
            }
        }

        // -----------------------------------------------------------------------------------------


        [Button("Generate No Source & Wrong Data Type Errors"), GUIColor(0.5f, 0.5f, 1)]
        public void Test2()
        {
            foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
            {
                // BROADCAST!
                tranzmitEvent.Value.Send(null, DummyClass);
            }
        }

        // -----------------------------------------------------------------------------------------


        [Button("Generate No Source & No Payload & Wrong Data Type Errors"), GUIColor(0.5f, 0.5f, 1)]
        public void Test3()
        {
            foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
            {
                // BROADCAST!
                tranzmitEvent.Value.Send(null, null);
            }
        }
    }
}