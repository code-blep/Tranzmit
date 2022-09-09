using System;
using System.Collections;
using System.Collections.Generic;
using Blep.Tranzmit;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class TranzmitDebugV2UIItemLocal : MonoBehaviour
{
    [Required] public UnityEngine.UI.Button Status;
    
    [Required] public UnityEngine.UI.Button FrameNumber;
    [Required] public TMP_Text FrameNumberText;
   
    [Required] public UnityEngine.UI.Button Broadcastor;
    [Required] public TMP_Text BroadcastorText;
    
    [Required] public UnityEngine.UI.Button Subscribers;
    [Required] public TMP_Text SubscribersText;
    
    [Required] public UnityEngine.UI.Button Errors;
    [Required] public TMP_Text ErrorsText;
    
    [Required] public UnityEngine.UI.Button EventName;
    [Required] public TMP_Text EventNameText;
    
    [Required] public TMP_Text Payload;

    [ReadOnly] public TranzmitDebugV2.SequentialLogData Log;
    
    // ---------------------------------------------------------------------------

    public void Start()
    {
        Status.onClick.AddListener(StatusButtonClicked);
        FrameNumber.onClick.AddListener(FrameButtonClicked);
        EventName.onClick.AddListener(EventNameButtonClicked);
        Broadcastor.onClick.AddListener(BroadcastorButtonClicked);
        Subscribers.onClick.AddListener(SubscribersButtonClicked);
        Errors.onClick.AddListener(ErrorsButtonClicked);
    }
    
    // ---------------------------------------------------------------------------

    public void OnDestroy()
    {
        Status.onClick.RemoveListener(StatusButtonClicked);
        FrameNumber.onClick.RemoveListener(FrameButtonClicked);
        EventName.onClick.RemoveListener(EventNameButtonClicked);
        Broadcastor.onClick.RemoveListener(BroadcastorButtonClicked);
        Subscribers.onClick.RemoveListener(SubscribersButtonClicked);
        Errors.onClick.RemoveListener(ErrorsButtonClicked);
    }
    
    // ---------------------------------------------------------------------------

    public void StatusButtonClicked()
    {
        if (TranzmitDebugV2UI.Instance.Filter.DeliveryStatus != Log.General.DeliveryStatus)
        {
            TranzmitDebugV2UI.Instance.Filter.DeliveryStatus = Log.General.DeliveryStatus;
        }
        else
        {
            TranzmitDebugV2UI.Instance.Filter.DeliveryStatus = Tranzmit.DeliveryStatuses.None;
        }

        TranzmitDebugV2UI.Instance.FilterChanged = true;
    }
    
    // ---------------------------------------------------------------------------

    public void FrameButtonClicked()
    {
        if (TranzmitDebugV2UI.Instance.Filter.FrameNumber != Log.General.FrameNumber)
        {
            TranzmitDebugV2UI.Instance.Filter.FrameNumber = Log.General.FrameNumber;
        }
        else
        {
            TranzmitDebugV2UI.Instance.Filter.FrameNumber = 0;
        }
        
        TranzmitDebugV2UI.Instance.FilterChanged = true;
    }
    
    // ---------------------------------------------------------------------------

    public void BroadcastorButtonClicked()
    {
        if (TranzmitDebugV2UI.Instance.Filter.Broadcaster != Log.General.Broadcaster)
        {
            TranzmitDebugV2UI.Instance.Filter.Broadcaster = Log.General.Broadcaster;     
        }
        else
        {
            TranzmitDebugV2UI.Instance.Filter.Broadcaster = null;
        }
        
        TranzmitDebugV2UI.Instance.FilterChanged = true;
    }
    
    // ---------------------------------------------------------------------------

    public void SubscribersButtonClicked()
    {
        if (Log.Subscribers.Count > 0)
        {
            TranzmitDebugV2UI.Instance.ShowSubscribers(Log.Subscribers);
        }
    }
    
    // ---------------------------------------------------------------------------

    public void ErrorsButtonClicked()
    {
        if (Log.General.Errors.Count > 0)
        {
            TranzmitDebugV2UI.Instance.ShowErrors(Log.General.Errors);
        }
    }
    
    // ---------------------------------------------------------------------------

    public void EventNameButtonClicked()
    {
        if (TranzmitDebugV2UI.Instance.Filter.EventName != Log.General.EventName)
        {
            TranzmitDebugV2UI.Instance.Filter.EventName = Log.General.EventName;
        }
        else
        {
            TranzmitDebugV2UI.Instance.Filter.EventName = Tranzmit.EventNames.None;
        }
        
        TranzmitDebugV2UI.Instance.FilterChanged = true;
    }
}
