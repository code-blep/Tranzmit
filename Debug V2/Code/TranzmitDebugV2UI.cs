using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blep.Tranzmit;
using Blep.Tranzmit.Demo;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TranzmitDebugV2UI : SerializedMonoBehaviour
{
    public static TranzmitDebugV2UI Instance;

    [BoxGroup("CONFIG")] public bool IsEnabled = true;
    [BoxGroup("CONFIG")] public bool NewAtTop = true;
    [BoxGroup("CONFIG")] public bool FilterEnabled;

    [BoxGroup("FILTER")][HideLabel] public FilterData Filter;
    
    [BoxGroup("REQUIRED")][Required] public Tranzmit Tranzmit;
    [BoxGroup("REQUIRED")][Required] public TranzmitDebugV2 TranzmitDebugV2;
    [BoxGroup("REQUIRED")][Required] public GameObject DebugItemPrefab;
    [BoxGroup("REQUIRED")][Required] public RectTransform DebugScroll;
    [BoxGroup("REQUIRED")][Required] public RectTransform DebugScrollContent;
    [BoxGroup("REQUIRED")][Required] public RectTransform SelectorModal;
    [BoxGroup("REQUIRED")][Required] public RectTransform SelectorScrollContent;
    [BoxGroup("REQUIRED")][Required] public GameObject SelectorButtonPrefab;
    [BoxGroup("REQUIRED")][Required] public TMP_Text EventCountText;
    [BoxGroup("REQUIRED")][Required] public TMP_Text ErrorsCountText;
    [BoxGroup("REQUIRED")][Required] public TMP_Text FilterInfo;
    
    [BoxGroup("BUTTONS")][Required] public Button CollapseButton;
    [BoxGroup("BUTTONS")][Required] public Button ClearButton;
    [BoxGroup("BUTTONS")][Required] public Button ResetFilter;
    [BoxGroup("BUTTONS")][Required] public Button ModalCloseButton;

    [BoxGroup("TOGGLES")][Required] public Toggle EnabledToggle;
    [BoxGroup("TOGGLES")][Required] public Toggle ErrorsOnlyToggle;
    [BoxGroup("TOGGLES")][Required] public Toggle StorePayloadsToggle;
    [BoxGroup("TOGGLES")][Required] public Toggle PayloadValuesOnly;
    
    [BoxGroup("DATA")][ReadOnly] public bool FilterChanged;
    [BoxGroup("DATA")][ReadOnly] public List<DebugLUTData> DebugLUT = new List<DebugLUTData>();
    [BoxGroup("DATA")][ReadOnly] public List<DebugLUTData> FilteredList = new List<DebugLUTData>();


    // private FilterData PreviousFilter;
    
    [Serializable]
    public class DebugLUTData
    {
        public TranzmitDebugV2.SequentialLogData Log;
        public TranzmitDebugV2UIItemLocal TranzmitDebugV2UIItemLocal;
    }

    [ShowOdinSerializedPropertiesInInspector]
    [HideReferenceObjectPicker]
    public class FilterData
    {
        public int FrameNumber;
        public Tranzmit.EventNames EventName;
        public Tranzmit.DeliveryStatuses DeliveryStatus;
        public Tranzmit.Errors Error;
        public object Broadcaster;
        public object Subscriber;

        public void Reset()
        {
            FrameNumber = 0;
            EventName = Tranzmit.EventNames.None;
            DeliveryStatus = Tranzmit.DeliveryStatuses.None;
            Error = Tranzmit.Errors.None;
            Broadcaster = null;
            Subscriber = null;
        }

        public string GenerateFilterInfo()
        {
            var result = "";
            
            if (FrameNumber != 0)
            {
                result += $"[ Frame Number: {FrameNumber} ] ";
            }
            
            if (Broadcaster != null)
            {
                result += $"[ Broadcaster: {Broadcaster.ToString()} ] ";
            }
            
            if (Subscriber != null)
            {
                result += $"[ Subscriber: {Subscriber.ToString()} ] ";
            }

            if (EventName != Tranzmit.EventNames.None)
            {
                result += $"[ Event Name: {EventName.ToString()} ] ";
            }
            
            if (DeliveryStatus != Tranzmit.DeliveryStatuses.None)
            {
                result += $"[ Delivery Status: {DeliveryStatus.ToString()} ] ";
            }
            
            if (Error != Tranzmit.Errors.None)
            {
                result += $"[ Error: {Error.ToString()} ] ";
            }

            return result;
        }
    }
    
    // -----------------------------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;
        
        SelectorModal.gameObject.SetActive(false);
        
        CollapseButton.onClick.AddListener(Collapse);
        ClearButton.onClick.AddListener(ClearLogDataClicked);
        ResetFilter.onClick.AddListener(ResetFilterClicked);
        ModalCloseButton.onClick.AddListener(ModalClose);

        EnabledToggle.onValueChanged.AddListener(EnableToggleChanged);
        ErrorsOnlyToggle.onValueChanged.AddListener(ErrorsOnlyToggleChanged);
        StorePayloadsToggle.onValueChanged.AddListener(StorePayloadsToggleChanged);
        PayloadValuesOnly.onValueChanged.AddListener(PayloadValuesOnlyChanged);
    }
    
    // -----------------------------------------------------------------------------------------

    private void Start()
    {
        EnabledToggle.isOn = TranzmitDebugV2.Instance.enabled;
        ErrorsOnlyToggle.isOn = TranzmitDebugV2.Instance.SequentialLogErrorsOnly;
        StorePayloadsToggle.isOn = TranzmitDebugV2.Instance.SequentialLogStorePayloads;
        PayloadValuesOnly.isOn = TranzmitDebugV2.Instance.SequentialLogValuesOnlyPayloads;
    }

    // -----------------------------------------------------------------------------------------
    
    public void OnDestroy()
    {
        CollapseButton.onClick.RemoveListener(Collapse);
        ClearButton.onClick.RemoveListener(ClearLogDataClicked);
        ResetFilter.onClick.RemoveListener(ResetFilterClicked);
        ModalCloseButton.onClick.RemoveListener(ModalClose);
        
        EnabledToggle.onValueChanged.RemoveListener(EnableToggleChanged);
        ErrorsOnlyToggle.onValueChanged.RemoveListener(ErrorsOnlyToggleChanged);
        StorePayloadsToggle.onValueChanged.RemoveListener(StorePayloadsToggleChanged);
        PayloadValuesOnly.onValueChanged.RemoveListener(PayloadValuesOnlyChanged);
    }

    // -----------------------------------------------------------------------------------------

    private void Update()
    {
        if (FilterChanged)
        {
            FilterChanged = false;
            FilterRefresh();
        }
    }

    // -----------------------------------------------------------------------------------------
    
    public void AddNewEvent(TranzmitDebugV2.SequentialLogData log)
    {
        log.General.UIElement = Instantiate(DebugItemPrefab);
        log.General.UIElement.transform.SetParent(DebugScrollContent);

        if (NewAtTop)
        {
            log.General.UIElement.transform.SetAsFirstSibling();
        }
        
        log.General.UIElement.name = $"{log.General.FrameNumber}-{log.General.EventName}-{log.General.DeliveryStatus}";

        var local = log.General.UIElement.GetComponent<TranzmitDebugV2UIItemLocal>();
        local.Log = log;
        
        // Status
        ColorBlock cb = local.Status.colors;
        cb.normalColor = log.General.StatusColor();
        local.Status.colors = cb;
        
        // Frame Number
        local.FrameNumberText.text = log.General.FrameNumber.ToString();
        
        // Event Name
        local.EventNameText.text = log.General.EventName.ToString();

        // Broadcaster
        if (log.General.Broadcaster != null)
        {
            local.BroadcastorText.text = log.General.Broadcaster.ToString();
        }
        else
        {
            ColorBlock broadcasterButtonColors = local.Status.colors;
            broadcasterButtonColors.normalColor =  new Color(0.1226415f, 0.007520473f, 0.007520473f, 1f);
            local.Broadcastor.colors = broadcasterButtonColors;
            local.BroadcastorText.text = "Broadcaster Missing";
        }
        
        // Subscribers
        if (log.Subscribers.Count > 0)
        {
            local.SubscribersText.text = $"Subscribers ({log.Subscribers.Count})";
        }
        else
        {
            local.Errors.interactable = false;
            local.SubscribersText.text = $"No Subscribers";
        }
        
        // Errors
        if (log.General.Errors.Count > 0)
        {
            ColorBlock errorsButtonColorBlock = local.Errors.colors;
            errorsButtonColorBlock.normalColor =  new Color(0.1226415f, 0.007520473f, 0.007520473f, 1f);
            local.Errors.colors = errorsButtonColorBlock;
            local.ErrorsText.text = $"Errors ({log.General.Errors.Count})";
        }
        else
        {
            local.Errors.interactable = false;
            local.ErrorsText.text = $"No Errors";
        }

        // Payload
        if (TranzmitDebugV2.SequentialLogStorePayloads)
        {
            local.Payload.text = log.Payload.Replace("\n", "").Replace("\r", " || ");
        }
        else
        {
            local.Payload.text = "";
        }

        DebugLUT.Add(new DebugLUTData(){TranzmitDebugV2UIItemLocal = local, Log = log});

        FilterRefresh();
    }
    
    // -----------------------------------------------------------------------------------------

    [Button]
    public void FilterRefresh()
    {
        // Initial List
        FilteredList = DebugLUT;

        // Set all to inactive
        foreach (var show in FilteredList)
        {
            show.Log.General.UIElement.SetActive(false);
        }

        // Delivery Status
        if (Filter.DeliveryStatus != Tranzmit.DeliveryStatuses.None)
        {
            FilteredList = FilteredList.Where(x => x.Log.General.DeliveryStatus == Filter.DeliveryStatus).ToList();
        }

        // Frame Number
        if (Filter.FrameNumber != 0)
        {
            FilteredList = FilteredList.Where(x => x.Log.General.FrameNumber == Filter.FrameNumber).ToList();
        }

        // Broadcaster
        if (Filter.Broadcaster != null)
        {
            FilteredList = FilteredList.Where(x => x.Log.General.Broadcaster == Filter.Broadcaster).ToList();
        }
        
        // Subscriber
        if (Filter.Subscriber != null)
        {
            FilteredList = FilteredList.Where(x => x.Log.Subscribers.Contains(Filter.Subscriber)).ToList();
        }
        
        // Error
        if (Filter.Error != Tranzmit.Errors.None)
        {
            FilteredList = FilteredList.Where(x => x.Log.General.Errors.Contains(Filter.Error)).ToList();
        }

        // Event Name
        if (Filter.EventName != Tranzmit.EventNames.None)
        {
            FilteredList = FilteredList.Where(x => x.Log.General.EventName == Filter.EventName).ToList();
        }

        // Apply
        foreach (var show in FilteredList)
        {
            show.Log.General.UIElement.SetActive(true);
        }

        // Text Update
        TextFieldsUpdate();
    }
    
    // -----------------------------------------------------------------------------------------
    
    public void ShowSubscribers(List<object> objects)
    {
        foreach (Transform child in SelectorScrollContent.transform)
        {
            Destroy(child.gameObject);
        }
    
        if (objects.Count > 0)
        {
            foreach (var o in objects)
            {
                var button = Instantiate(SelectorButtonPrefab);
                button.transform.SetParent(SelectorScrollContent);
                button.transform.name = o.ToString();
                
                var local = button.GetComponent<TranzmitDebugV2UISelectorButtonLocal>();
                local.Buttontext.text = o.ToString();
                local.ButtonType = TranzmitDebugV2UISelectorButtonLocal.ButtonTypes.Subscriber;
                local.Subscriber = o;

                if (o == Filter.Subscriber)
                {
                    ColorBlock broadcasterButtonColors = local.Button.colors;
                    broadcasterButtonColors.normalColor =  Color.yellow;
                    local.Buttontext.color = Color.black;
                    local.Button.colors = broadcasterButtonColors; 
                }
            }
        }

        SelectorModal.gameObject.SetActive(true);
    }
    
    // -----------------------------------------------------------------------------------------
    
    public void CloseSelectionModal()
    {
        SelectorModal.gameObject.SetActive(false);
    }
    
    // -----------------------------------------------------------------------------------------

    public void ShowErrors(List<Tranzmit.Errors> errors)
    {
        foreach (Transform child in SelectorScrollContent.transform)
        {
            Destroy(child.gameObject);
        }
    
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                var button = Instantiate(SelectorButtonPrefab);
                button.transform.SetParent(SelectorScrollContent);
                button.transform.name = error.ToString();
                
                var local = button.GetComponent<TranzmitDebugV2UISelectorButtonLocal>();
                local.Buttontext.text = error.ToString();
                local.ButtonType = TranzmitDebugV2UISelectorButtonLocal.ButtonTypes.Error;
                local.Error = error;

                if (error == Filter.Error)
                {
                    ColorBlock errorButtonColors = local.Button.colors;
                    errorButtonColors.normalColor = Color.yellow;
                    local.Buttontext.color = Color.black;
                    local.Button.colors = errorButtonColors; 
                }
            }
        }

        SelectorModal.gameObject.SetActive(true);
    }
    
    // -----------------------------------------------------------------------------------------

    public void ClearLogDataClicked()
    {
        foreach (Transform child in DebugScrollContent.transform)
        {
            Destroy(child.gameObject);
        }
        
        DebugLUT.Clear();
        TranzmitDebugV2.Instance.InitializeDebugLog();
        TextFieldsUpdate();
    }

    // -----------------------------------------------------------------------------------------

    public void ModalClose()
    {
        CloseSelectionModal();
    }
    
    // -----------------------------------------------------------------------------------------

    public void ResetFilterClicked()
    {
        Filter = new FilterData();
        FilterRefresh();
    }
    
    // -----------------------------------------------------------------------------------------

    public void Collapse()
    {
        DebugScrollContent.gameObject.SetActive(!DebugScrollContent.gameObject.activeSelf);
    }
    
    // -----------------------------------------------------------------------------------------

    [Button]
    public void TextFieldsUpdate()
    {
        StartCoroutine(Go());

        IEnumerator Go()
        {
            // Allow for UI to refresh!
            yield return null;
            FilterInfo.text = Filter.GenerateFilterInfo();
            EventCountText.text = $"[{ChildCountActive(DebugScrollContent)} / {DebugScrollContent.childCount}]";
            ErrorsCountText.text = $"[{TranzmitDebugV2.Instance.GetErrorsCount()}]";
        }
    }
    
    // -----------------------------------------------------------------------------------------
    
    public static int ChildCountActive( Transform target )
    {
        int result = 0;
        
        foreach(Transform t in target)
        {
            if (t.gameObject.activeSelf)
            {
                result++;
            }
        }
        
        return result;
    }
    
    // -----------------------------------------------------------------------------------------

    public void EnableToggleChanged(bool status)
    {
        TranzmitDebugV2.Instance.SequentialLogEnabled = status;
    }
    
    // -----------------------------------------------------------------------------------------

    public void ErrorsOnlyToggleChanged(bool status)
    {
        TranzmitDebugV2.Instance.SequentialLogErrorsOnly = status;
    }
    
    // -----------------------------------------------------------------------------------------

    public void StorePayloadsToggleChanged(bool status)
    {
        TranzmitDebugV2.Instance.SequentialLogStorePayloads = status;
    }
    
    // -----------------------------------------------------------------------------------------

    public void PayloadValuesOnlyChanged(bool status)
    {
        TranzmitDebugV2.Instance.SequentialLogValuesOnlyPayloads = status;
    }
}