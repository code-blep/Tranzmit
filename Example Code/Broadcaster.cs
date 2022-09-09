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
        [Range(1, 100)]
        public int StressMultiplier = 10;

        [FoldoutGroup("Settings")]
        [Tooltip("Delay between broadcasts.")]
        [Range(0.01f, 10)]
        public float SendDelay = 1;

        [FoldoutGroup("Settings")]
        public PlayerStatsData PlayerStatsTestData;

        private DummyClassData DummyClass;

        [Serializable]
        public struct PlayerStatsData
        {
            public string Name;
            public float Health;
        }

        public class DummyClassData
        {
            public string Sometext = "Some Text for Dummy class";
        }

        // -----------------------------------------------------------------------------------------

        private void Start()
        {
            StartCoroutine(Main());
            
            if (StressTest)
            {
                for (int i = 0; i < StressMultiplier; i++)
                {
                    // The SendPlayerStats example is using an already created instance of SendPlayerStats. This is to reduce Garbage collection when load testing as it could potentially distort the results.
                    SendPlayerStats(Tranzmit.EventNames.PlayerStats);
                    SendDamage(Tranzmit.EventNames.Damage);
                    SendSecretFound(Tranzmit.EventNames.SecretFound);
                }
            }
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
                        SendDamage(Tranzmit.EventNames.Damage);
                        SendSecretFound(Tranzmit.EventNames.SecretFound);
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
                PlayerStatsTestData.Health = UnityEngine.Random.Range(0, 100);
                PlayerStatsTestData.Name = GenerateName(UnityEngine.Random.Range(5, 20));
                
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
        public void SendDamage(Tranzmit.EventNames eventName = Tranzmit.EventNames.Damage)
        {
            if (Tranzmit != null)
            {
                // BROADCAST!
                Tranzmit.BroadcastEvent(eventName, this, UnityEngine.Random.Range(0, 100));
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!");
            }
        }

        // -----------------------------------------------------------------------------------------

        [Button("Send Secret Found"), GUIColor(0.5f, 0.5f, 1)]
        [PropertySpace(10, 10)]
        public void SendSecretFound(Tranzmit.EventNames eventName = Tranzmit.EventNames.SecretFound)
        {
            if (Tranzmit != null)
            {
                // BROADCAST!
                Tranzmit.BroadcastEvent(eventName, this, RandomBoolean());
            }
            else
            {
                Debug.Log("No Instance of Tranzmit has been found!");
            }
        }
        
        // -----------------------------------------------------------------------------------------

        [Button("Generate No Source Errors"), GUIColor(0.5f, 0.5f, 1)]
        public void Test1()
        {
            foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
            {
                if (tranzmitEvent.Value.Info.EventName == Tranzmit.EventNames.Damage)
                {
                    // BROADCAST!
                    tranzmitEvent.Value.Send(null, UnityEngine.Random.Range(0, 100));
                }
                
                if (tranzmitEvent.Value.Info.EventName == Tranzmit.EventNames.SecretFound)
                {
                    // BROADCAST!
                    tranzmitEvent.Value.Send(null, true);
                }
                
                if (tranzmitEvent.Value.Info.EventName == Tranzmit.EventNames.PlayerStats)
                {
                    // BROADCAST!
                    tranzmitEvent.Value.Send(null, new PlayerStatsData(){ Name = GenerateName(UnityEngine.Random.Range(5, 20)), Health = UnityEngine.Random.Range(0, 100)});
                }
            }
        }

        // -----------------------------------------------------------------------------------------

        [Button("Generate No Payload Errors"), GUIColor(0.5f, 0.5f, 1)]
        public void Test2()
        {
            foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
            {
                // BROADCAST!
                tranzmitEvent.Value.Send(this, null);
            }
        }
        
        // -----------------------------------------------------------------------------------------

        [Button("Generate Wrong Data Type Errors"), GUIColor(0.5f, 0.5f, 1)]
        public void Test3()
        {
            foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
            {
                // BROADCAST!
                tranzmitEvent.Value.Send(this, new DummyClassData(){ Sometext = "Some text!"});
            }
        }
        
        // -----------------------------------------------------------------------------------------

        [Button("Generate Multiple Errors"), GUIColor(0.5f, 0.5f, 1)]
        public void Test4()
        {
            foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
            {
                // BROADCAST!
                tranzmitEvent.Value.Send(null, null);
            }
        }
        
        // -----------------------------------------------------------------------------------------
        
        public bool RandomBoolean ()
        {
            if (UnityEngine.Random.value >= 0.5)
            {
                return true;
            }
            return false;
        }
        
        // -----------------------------------------------------------------------------------------
        
        public string GenerateName(int len)
        { 
            System.Random r = new System.Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;
        }
    }
}