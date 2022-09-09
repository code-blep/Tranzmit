using System;
using System.Collections;
using System.Collections.Generic;
using Blep.Tranzmit;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TranzmitDebugV2UISelectorButtonLocal : SerializedMonoBehaviour
{
    [Required] public Button Button;
    [Required] public TMP_Text Buttontext;
    public ButtonTypes ButtonType;
    public object Subscriber;
    public Tranzmit.Errors Error;
    
    // -----------------------------------------------------------------------------------------

    public enum ButtonTypes { None, Subscriber, Error }

    // -----------------------------------------------------------------------------------------

    public void Awake()
    {
        Button.onClick.AddListener(ButtonClicked);
    }
    
    // -----------------------------------------------------------------------------------------

    public void OnDestroy()
    {
        Button.onClick.RemoveListener(ButtonClicked);
    }

    // -----------------------------------------------------------------------------------------

    public void ButtonClicked()
    {
        if (ButtonType == ButtonTypes.Subscriber)
        {
            if (TranzmitDebugV2UI.Instance.Filter.Subscriber != Subscriber)
            {
                TranzmitDebugV2UI.Instance.Filter.Subscriber = Subscriber;
            }
            else
            {
                TranzmitDebugV2UI.Instance.Filter.Subscriber = null;
            }

            TranzmitDebugV2UI.Instance.CloseSelectionModal();
            TranzmitDebugV2UI.Instance.FilterChanged = true;
        }
        
        if (ButtonType == ButtonTypes.Error)
        {
            if (TranzmitDebugV2UI.Instance.Filter.Error != Error)
            {
                TranzmitDebugV2UI.Instance.Filter.Error = Error;
            }
            else
            {
                TranzmitDebugV2UI.Instance.Filter.Error = Tranzmit.Errors.None;
            }

            TranzmitDebugV2UI.Instance.CloseSelectionModal();
            TranzmitDebugV2UI.Instance.FilterChanged = true;
        }
    }
}
