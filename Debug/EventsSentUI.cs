using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Blep.Tranzmit.Demo
{
    public class EventsSentUI : MonoBehaviour
    {
        public TMP_Text TotalUI;
        public int Total = 0;

        // -----------------------------------------------------------------------------------------

        void Update()
        {
            TotalUI.text = Total.ToString();
        }
    }
}