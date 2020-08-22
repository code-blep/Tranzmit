using Blep.Tranzmit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class Tranzmit_Graph_View : GraphView
{
    /// <summary>
    /// HIGHLY EXPERIMENTAL!.... Unity are also classing this as experimental.
    /// I wanted to learn and thought Tranmit would be useful in this environment.
    /// </summary>

    public Tranzmit Tranzmit;
    public TranzmitDebug TranzmitDebug;
    public Blackboard Blackboard;

    private Rect TranzmitCorePosition = new Rect(600, 200, 600, 600);
    private Rect TranzmitSubscriberBasePosition = new Rect(1000, 200, 500, 500);
    private Rect TranzmitBroadcastBasePosition = new Rect(200, 200, 500, 500);

    public ColorProperty SilentColor = new ColorProperty() { PropertyName = "Silent Color", PropertyValue = Color.gray};
    public ColorProperty SuccessColor = new ColorProperty() { PropertyName = "Success Color", PropertyValue = new Color(0.27f, 0.64f, 1.00f)};
    public ColorProperty ErrorColor = new ColorProperty() { PropertyName = "Error Color", PropertyValue = new Color(.8f, .2f, 0f) };

    public Vector2Property GridLayoutSpacing_Subscriber = new Vector2Property() { PropertyName = "Subscriber Grid Spacing", PropertyValue = new Vector2(400, 180) };
    public Vector2Property GridLayoutSpacing_Broadcaster = new Vector2Property() { PropertyName = "Broadcaster Grid Spacing", PropertyValue = new Vector2(400, 180) };

    public FloatProperty VerticalYSpacing_Subscriber = new FloatProperty() { PropertyName = "Subscriber Vertical Y Spacing", PropertyValue = 90 };
    public FloatProperty VerticalYSpacing_Broadcaster = new FloatProperty() { PropertyName = "Broadcaster Vertical Y Spacing", PropertyValue = 90 };

    private float SubscriberPortsSpacing = 24;
    private float BroadcasterSpacing = 24;

    private List<SubscriberNodeData> SubscriberNodes = new List<SubscriberNodeData>();

    private Dictionary<Tranzmit.EventNames, BroadcasterNodeData> BroadcasterNodes = new Dictionary<Tranzmit.EventNames, BroadcasterNodeData>();

    public ArrangementTypes CurrentArrangementType = ArrangementTypes.Grid;

    public class SubscriberNodeData
    {
        public object Subscriber;
        public Tranzmit_Node Node;
        public List<PortEdgeEventData> PortEdgeEvent = new List<PortEdgeEventData>();
    }

    public class BroadcasterNodeData
    {
        public Tranzmit_Node TranzmitNodeEventNode;
        public Port Port;
        public Port TranzmitPort;
        public Edge Edge;
        public int TotalButtons;
    }

    public class PortEdgeEventData
    {
        public Tranzmit.EventNames EventName;
        public Port SubscriberPort;
        public Port TranzmitPort;
        public Edge Edge;
    }

    public enum ArrangementTypes
    {
        Vertical = 0,
        Inline = 1,
        Grid = 2
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// UNITY - Creates the graph instance and relevant settings.
    /// </summary>
    public Tranzmit_Graph_View()
    {
        // Load style sheet
        styleSheets.Add(Resources.Load<StyleSheet>("Tranzmit_Graph"));

        // Enable common graph interactions
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Setup Zoom
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        // Setup Grid
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        Generate();
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Manual way of forcing a refresh of Tranzmit Graph. Used on startup.
    /// </summary>
    public void SyncGraphWithTranzmitDebug()
    {
        foreach (KeyValuePair<Tranzmit.EventNames, List<TranzmitDebug.LogEntry>> entry in TranzmitDebug.Success)
        {
            foreach (TranzmitDebug.LogEntry log in entry.Value)
            {
                UpdateButtonsOnSubscriberNodes(log);
                UpdateButtonsAndPortsOnBroadcasterNodes(log, true);
            }
        }

        foreach (KeyValuePair<Tranzmit.EventNames, List<TranzmitDebug.LogEntry>> entry in TranzmitDebug.Failed)
        {
            foreach (TranzmitDebug.LogEntry log in entry.Value)
            {
                UpdateButtonsAndPortsOnBroadcasterNodes(log, true);
            }
        }
    }

    // -----------------------------------------------------------------------------------------

    // This section deals with events recieved from Tranzmit.

    public void EventAdded(Tranzmit.EventNames eventName)
    {
        Generate();
    }

    public void EventDeleted(Tranzmit.EventNames eventName)
    {
        Generate();
    }

    public void EventTranzmitDebugReset()
    {
        Generate();
    }

    public void TranzmitDebugLogUpdated(TranzmitDebug.LogEntry logEntry)
    {
        UpdateButtonsOnSubscriberNodes(logEntry);
        UpdateButtonsAndPortsOnBroadcasterNodes(logEntry, false);
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// This handles the whole process of generating the graph
    /// </summary>
    public void Generate()
    {
        RemoveAllGraphElements();

        TranzmitDebug = GameObject.FindObjectOfType<TranzmitDebug>();
        Tranzmit = GameObject.FindObjectOfType<Tranzmit>();

        if (Tranzmit != null)
        {
            if (TranzmitDebug != null)
            {
                GenerateTranzmitNodes();

                SyncGraphWithTranzmitDebug();

                Arrange_Subscriber_Results(CurrentArrangementType);

                Arrange_Broadcaster_Results(CurrentArrangementType);
            }
            else
            {
                // Debug.Log("No Instance of TranzmitDebug has been found!\nTranzmit Graph will not be generated");
            }
        }
        else
        {
            // Debug.Log("No Instance of Tranzmit has been found!\nTranzmit Graph will not be generated");
        }
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Removes all graph elements. Due to the poor documentation, I am not aware of a better way to do this. I can't help feeling there is a better way!
    /// </summary>
    private void RemoveAllGraphElements()
    {
        ports.ForEach((port) =>
        {
            port.Clear();
        });

        nodes.ForEach((node) =>
        {
            node.Clear();
        });

        edges.ForEach((edge) =>
        {
            edge.Clear();
        });
    }

    // -----------------------------------------------------------------------------------------

    // TRANZMIT NODE OUT Port Generation
    private Port Port_Generate_TRANZMIT_Out(Tranzmit_Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Multi)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(int));
    }

    // -----------------------------------------------------------------------------------------

    // TRANZMIT NODE OUT Port Generation
    private Port Port_Generate_TRANZMIT_In(Tranzmit_Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(int));
    }

    // -----------------------------------------------------------------------------------------

    // SUBSCRIBER NODE Port Generation
    private Port Port_Generate_SUBSCRIBER(Tranzmit_Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(int));
    }

    // -----------------------------------------------------------------------------------------

    // EVENT TYPE NODE Port Generation
    private Port Port_Generate_EVENT_TYPE(Tranzmit_Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(int));
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Launcher for generating all required nodes for the graph to work
    /// </summary>
    private void GenerateTranzmitNodes()
    {
        SubscriberNodes.Clear();
        BroadcasterNodes.Clear();

        // TRANZMIT -------------------------------
        var tranzmitNode = GenerateTranzmitNode("TRANZMIT");

        // OTHER NODES / PORTS
        foreach (KeyValuePair<Tranzmit.EventNames, Tranzmit.EventData> tranzmitEvent in Tranzmit.Events)
        {
            // Tranzmit Event Type Port
            var outPort = Port_Add_Tranzmit_OUT(tranzmitNode, tranzmitEvent.Key);
            var inPort = Port_Add_Tranzmit_IN(tranzmitNode, tranzmitEvent.Key);

            // Create and connect Subscriber Nodes for this Port / Tranzmit Event Type
            Generate_Nodes_Subscriber(tranzmitNode, outPort, tranzmitEvent.Value);

            // TRANZMIT EVENT TYPE NODES-------------------
            Generate_Broadcaster_Node(inPort, tranzmitEvent.Value);
        }
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Generates a new Tranzmit Node
    /// </summary>
    /// <param name="nodeName">The displayed name of the node.</param>
    /// <returns>The generated Node</returns>
    private Tranzmit_Node GenerateTranzmitNode(string nodeName)
    {
        var node = new Tranzmit_Node
        {
            title = nodeName,
            Dialogue = nodeName,
            GUID = Guid.NewGuid().ToString()
        };

        var button = new Button(clickEvent: () => { SelectObject(Tranzmit); });
        button.text = "Find Tranzmit";
        node.mainContainer.Add(button);

        var button2 = new Button(clickEvent: () => { SelectObject(TranzmitDebug); });
        button2.text = "Find Tranzmit Debug";
        node.mainContainer.Add(button2);

        node.SetPosition(TranzmitCorePosition);

        AddElement(node);

        return node;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Generates a new Subscriber Node
    /// </summary>
    /// <param name="nodeName">The displayed name of the node.</param>
    /// <returns>The generated Node</returns>
    private Tranzmit_Node GenerateTranzmitSubscriberNode(string nodeName)
    {
        var node = new Tranzmit_Node
        {
            title = nodeName,
            Dialogue = nodeName,
            GUID = Guid.NewGuid().ToString()
        };

        node.SetPosition(TranzmitSubscriberBasePosition);

        AddElement(node);

        return node;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Generates a new Broadcaster Node
    /// </summary>
    /// <param name="nodeName">The displayed name of the node.</param>
    /// <returns>The generated Node</returns>
    private Tranzmit_Node GenerateTranzmitBoroadcasterNode(string nodeName)
    {
        var node = new Tranzmit_Node
        {
            title = nodeName,
            Dialogue = nodeName,
            GUID = Guid.NewGuid().ToString()
        };

        node.SetPosition(TranzmitBroadcastBasePosition);

        AddElement(node);

        return node;
    }

    // -----------------------------------------------------------------------------------------

    // Adds an Event Type Out port on the Core Node
    /// <summary>
    /// Adds an In port on the Tranzmit Node
    /// </summary>
    /// <param name="node">The Tranzmit Node</param>
    /// <param name="eventName">The associated event</param>
    /// <returns>The Generated Port</returns>
    private Port Port_Add_Tranzmit_OUT(Tranzmit_Node node, Tranzmit.EventNames eventName)
    {
        // OUT
        var outPort = Port_Generate_TRANZMIT_Out(node, Direction.Output);
        outPort.portName = eventName.ToString(); ;
        outPort.portColor = SilentColor.PropertyValue;
        node.outputContainer.Add(outPort);

        return outPort;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Adds an Out port on the Tranzmit Node
    /// </summary>
    /// <param name="node">The Tranzmit Node</param>
    /// <param name="eventName">The associated event</param>
    /// <returns>The Generated Port</returns>
    private Port Port_Add_Tranzmit_IN(Tranzmit_Node node, Tranzmit.EventNames eventName)
    {
        // IN
        var inPort = Port_Generate_TRANZMIT_In(node, Direction.Input);
        inPort.portName = eventName.ToString(); ;
        inPort.portColor = SilentColor.PropertyValue;
        node.inputContainer.Add(inPort);

        return inPort;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Adds an Event Type Out port on the Core Node
    /// </summary>
    /// <param name="node">The Node to which the Port will be attached to</param>
    /// <returns>The Generated Port</returns>
    private Port Port_Add_Event_Type_Node(Tranzmit_Node node)
    {
        var port = Port_Generate_EVENT_TYPE(node, Direction.Output);
        port.portName = "";
        port.portColor = SilentColor.PropertyValue;
        node.inputContainer.Add(port);

        return port;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Generates ALL Subscriber Nodes
    /// </summary>
    /// <param name="tranzmitNode">The Tanzmit Node to connect to</param>
    /// <param name="coreNodePort">The Tanzmit Port to connect to</param>
    /// <param name="tranzmitEvent">The associated event</param>
    private void Generate_Nodes_Subscriber(Tranzmit_Node tranzmitNode, Port coreNodePort, Tranzmit.EventData tranzmitEvent)
    {
        if (tranzmitEvent != null)
        {
            // Iterate Subscribers in the Event
            foreach (Delegate subscriber in tranzmitEvent.GetSubscribers())
            {
                bool found = false;

                // Iterate through our list of generated Subscriber Nodes
                foreach (SubscriberNodeData subNode in SubscriberNodes)
                {
                    // Check if the subscription objects are the same
                    if (subscriber.Target == subNode.Subscriber)
                    {
                        found = true;

                        // We just add a port onto the existing graph node
                        var SubscriberPort = GenerateTranzmitSubscriberInputPorts(subNode.Node, tranzmitEvent);
                        var edge = SubscriberPort.ConnectTo(coreNodePort);
                        SubscriberPort.edgeConnector.target.Add(edge);

                        subNode.PortEdgeEvent.Add(new PortEdgeEventData() { SubscriberPort = SubscriberPort, TranzmitPort = coreNodePort, Edge = edge, EventName = tranzmitEvent.Info.EventName });
                    }
                }

                // No graph node exists for the Subscription object so we create it and add / connect the edge.
                if (found == false)
                {
                    var subscriberNode = GenerateTranzmitSubscriberNode(subscriber.Target.ToString());

                    SubscriberNodeData newSubscriberNode = new SubscriberNodeData();
                    newSubscriberNode.Node = subscriberNode;
                    newSubscriberNode.Subscriber = subscriber.Target;

                    var button = new Button(clickEvent: () => { SelectObject(subscriber.Target); });
                    button.text = "Find";
                    newSubscriberNode.Node.mainContainer.Add(button);


                    // Connect Ports
                    var SubscriberPort = GenerateTranzmitSubscriberInputPorts(subscriberNode, tranzmitEvent);

                    // Build Edge
                    var edge = SubscriberPort.ConnectTo(coreNodePort);
                    SubscriberPort.edgeConnector.target.Add(edge);

                    // Add connected port to new entry
                    newSubscriberNode.PortEdgeEvent.Add(new PortEdgeEventData() { SubscriberPort = SubscriberPort, TranzmitPort = coreNodePort, Edge = edge, EventName = tranzmitEvent.Info.EventName });

                    // Add Entry
                    SubscriberNodes.Add(newSubscriberNode);
                }
            }

            // Update the node when we are done with it
            tranzmitNode.RefreshExpandedState();
            tranzmitNode.RefreshPorts();
        }
        else
        {
            Debug.Log("No subscribers were found!");
        }
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Generate the all parts of the Broadcaster Node including ports etc, connects it the Tranzmit Node, and adds it to the BroadcasterNodes Dictionary for easy reference later on.
    /// </summary>
    /// <param name="tranzmitPort">The port on the Tranzmit Node to which this new Broadcaster node will be connected to.</param>
    /// <param name="tranzmitEvent">The associated event</param>
    private void Generate_Broadcaster_Node(Port tranzmitPort, Tranzmit.EventData tranzmitEvent)
    {
        var node = GenerateTranzmitBoroadcasterNode(tranzmitEvent.Info.EventName.ToString());

        var port = Port_Add_Event_Type_Node(node);
        var edge = port.ConnectTo(tranzmitPort);
        port.edgeConnector.target.Add(edge);

        var newEventTypeNodeData = new BroadcasterNodeData();
        newEventTypeNodeData.TranzmitNodeEventNode = node;
        newEventTypeNodeData.Port = port;
        newEventTypeNodeData.TranzmitPort = tranzmitPort;
        newEventTypeNodeData.Edge = edge;

        BroadcasterNodes.Add(tranzmitEvent.Info.EventName, newEventTypeNodeData);

        node.RefreshExpandedState();
        node.RefreshPorts();
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Creates the ports on the Subscriber Nodes.
    /// </summary>
    /// <param name="subscriberNode">The Node to which we want to attach this port to.</param>
    /// <param name="tranzmitEvent">The Event that will be associated with this port.</param>
    /// <returns>The created port</returns>
    private Port GenerateTranzmitSubscriberInputPorts(Tranzmit_Node subscriberNode, Tranzmit.EventData tranzmitEvent)
    {
        var inputPort = Port_Generate_SUBSCRIBER(subscriberNode, Direction.Input, Port.Capacity.Single);
        inputPort.portName = tranzmitEvent.Info.EventName.ToString();
        inputPort.portColor = SilentColor.PropertyValue;
        subscriberNode.inputContainer.Add(inputPort);
        subscriberNode.RefreshExpandedState();
        subscriberNode.RefreshPorts();

        return inputPort;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Used to enable Edge Connections this is a Unity in-built callback. Without this you cannot manually connect ports.
    /// NOTICE: This is not actually needed as we do not manually attached ports, but seeing as this was a learning project, I am leaving it in for reference and for anyone who wishes to extend this.
    /// </summary>
    /// <returns></returns>
    public override List<Port> GetCompatiblePorts(Port StartPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        ports.ForEach((port) =>
        {
            // make sure edge is not trying to connect with it's own port OR the node it is attached too.
            if (StartPort != port && StartPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });

        return compatiblePorts;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Updates the RELEAVNT buttons in Subscriber nodes, when a SUCCESSFUL Event has occured.
    /// </summary>
    /// <param name="status">Checks if Event Broadcast was succefull or not.</param>
    /// <param name="eventName">The associated event</param>
    public void UpdateButtonsOnSubscriberNodes(TranzmitDebug.LogEntry logEntry)
    {
        // They only react to Success Logs - Which only have index[0] in LogEntries List.
        if (logEntry.Status == Tranzmit.DeliveryStatuses.Success)
        {
            foreach (SubscriberNodeData subNode in SubscriberNodes)
            {
                foreach (PortEdgeEventData portNodeEvent in subNode.PortEdgeEvent)
                {
                    if (portNodeEvent.EventName == logEntry.TranzmitEvent)
                    {
                        portNodeEvent.TranzmitPort.portColor = SuccessColor.PropertyValue;
                        portNodeEvent.TranzmitPort.visualClass = ""; // HACK: Force Refresh. Vary the input to trigger otherwise only works once.

                        portNodeEvent.SubscriberPort.portName = $"{logEntry.TranzmitEvent.ToString()} [{TranzmitDebug.Success[logEntry.TranzmitEvent][0].Subscribers[subNode.Subscriber].EventCount}]"; // Always index 0!
                        portNodeEvent.SubscriberPort.portColor = SuccessColor.PropertyValue;
                        portNodeEvent.SubscriberPort.visualClass = "";  // HACK: Force Refresh. Vary the input to trigger otherwise only works once.

                        portNodeEvent.Edge.UpdateEdgeControl();
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Updates the RELEVANT buttons and Ports assicoated with the Event that occured.
    /// </summary>
    /// <param name="logEntry">The log entry that was added or modified.</param>
    /// <param name="forceRefresh">Use full refresh all buttons when no event has occured.</param>
    public void UpdateButtonsAndPortsOnBroadcasterNodes(TranzmitDebug.LogEntry logEntry, bool forceRefresh)
    {
        if (BroadcasterNodes.ContainsKey(logEntry.TranzmitEvent))
        {
            // BUTTONS
            foreach (KeyValuePair<object, TranzmitDebug.ButtonData> buttonData in logEntry.Broadcasters)
            {
                // IF Button does not exist, create it.
                if (buttonData.Value.GraphButton == null || forceRefresh == true)
                {
                    buttonData.Value.GraphButton = new Button(clickEvent: () => { SelectObject(buttonData.Key); });

                    if (logEntry.Errors.Contains(Tranzmit.Errors.MissingSource))
                    {
                        buttonData.Value.GraphButton.text = $"Source Missing! [{buttonData.Value.EventCount}]";
                    }
                    else
                    {
                        buttonData.Value.GraphButton.text = $"{buttonData.Key.ToString()} [{buttonData.Value.EventCount}]";
                    }

                    if (logEntry.Status == Tranzmit.DeliveryStatuses.Failed)
                    {
                        buttonData.Value.GraphButton.style.backgroundColor = ErrorColor.PropertyValue;
                    }

                    BroadcasterNodes[logEntry.TranzmitEvent].TranzmitNodeEventNode.mainContainer.Add(buttonData.Value.GraphButton);
                }
                else // Button Exists
                {
                    if (logEntry.Errors.Contains(Tranzmit.Errors.MissingSource))
                    {
                        buttonData.Value.GraphButton.text = $"Source Missing! [{buttonData.Value.EventCount}]";
                    }
                    else
                    {
                        buttonData.Value.GraphButton.text = $"{buttonData.Key.ToString()} [{buttonData.Value.EventCount}]";
                    }
                }

                BroadcasterNodes[logEntry.TranzmitEvent].TotalButtons = BroadcasterNodes[logEntry.TranzmitEvent].TranzmitNodeEventNode.mainContainer.childCount;
            }

            // PORTS
            if (logEntry.Status == Tranzmit.DeliveryStatuses.Success)
            {
                if (BroadcasterNodes[logEntry.TranzmitEvent].Port.portColor != ErrorColor.PropertyValue)
                {
                    BroadcasterNodes[logEntry.TranzmitEvent].TranzmitPort.portColor = SuccessColor.PropertyValue;
                    BroadcasterNodes[logEntry.TranzmitEvent].TranzmitPort.visualClass = ""; // HACK: Force Refresh.

                    BroadcasterNodes[logEntry.TranzmitEvent].Port.portColor = SuccessColor.PropertyValue;
                    BroadcasterNodes[logEntry.TranzmitEvent].Port.visualClass = ""; // HACK: Force Refresh.
                }
            }
            else
            {
                if (BroadcasterNodes[logEntry.TranzmitEvent].Port.portColor != ErrorColor.PropertyValue)
                {
                    BroadcasterNodes[logEntry.TranzmitEvent].TranzmitPort.portColor = ErrorColor.PropertyValue;
                    BroadcasterNodes[logEntry.TranzmitEvent].TranzmitPort.visualClass = "1"; // HACK: Force Refresh.

                    BroadcasterNodes[logEntry.TranzmitEvent].Port.portColor = ErrorColor.PropertyValue;
                    BroadcasterNodes[logEntry.TranzmitEvent].Port.visualClass = "1"; // HACK: Force Refresh.
                }
            }


            Arrange_Broadcaster_Results(CurrentArrangementType);
        }
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Handles the  actual layout of Subscriber nodes in the Graph View
    /// </summary>
    public void Arrange_Subscriber_Results(ArrangementTypes arrangementType)
    {
        CurrentArrangementType = ArrangementTypes.Vertical;

        if (arrangementType == ArrangementTypes.Vertical)
        {
            // Node index ;)
            var count = 0;

            Vector2 position = new Vector2();

            for (int col = 0; col < SubscriberNodes.Count(); col++)
            {
                if (count > 0)
                {
                    position += new Vector2(
                    0,
                    VerticalYSpacing_Subscriber.PropertyValue + (SubscriberPortsSpacing * SubscriberNodes[count - 1].PortEdgeEvent.Count())
                    );
                }
                else
                {
                    position = new Vector2(
                    TranzmitSubscriberBasePosition.x,
                    TranzmitSubscriberBasePosition.y
                    );
                }

                SubscriberNodes[count].Node.SetPosition(new Rect(position, Vector2.zero));
                count++;
                if (count == SubscriberNodes.Count())
                {
                    return;
                }
            }
        }

        if (arrangementType == ArrangementTypes.Grid)
        {
            CurrentArrangementType = ArrangementTypes.Grid;

            float portSpacing = GetMostSubscribersInASingleSubscriberNode() * SubscriberPortsSpacing;

            int columns = (int)Math.Sqrt(SubscriberNodes.Count());
            int rows = (int)Math.Ceiling(SubscriberNodes.Count() / (float)columns);

            // Node index ;)
            var count = 0;

            for (int col = 0; col < columns; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    Vector2 position = new Vector2(
                        TranzmitSubscriberBasePosition.x + (row * GridLayoutSpacing_Subscriber.PropertyValue.x),
                        TranzmitSubscriberBasePosition.y + (col * GridLayoutSpacing_Subscriber.PropertyValue.y) + (col * portSpacing));

                    SubscriberNodes[count].Node.SetPosition(new Rect(position, Vector2.zero));

                    count++;

                    if(count == SubscriberNodes.Count())
                    {
                        return;
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Handles the  actual layout of Broadcaster nodes in the Graph View
    /// </summary>
    /// <param name="arrangementType"></param>
    public void Arrange_Broadcaster_Results(ArrangementTypes arrangementType)
    {
        CurrentArrangementType = ArrangementTypes.Vertical;

        if (arrangementType == ArrangementTypes.Vertical)
        {
            var count = 0;

            Vector2 position = new Vector2();

            List<BroadcasterNodeData> eventTypeNodesList = BroadcasterNodes.Values.ToList();

            for (int col = 0; col < eventTypeNodesList.Count(); col++)
            {
                if (count > 0)
                {
                    position += new Vector2(
                    0,
                    VerticalYSpacing_Broadcaster.PropertyValue + (BroadcasterSpacing * eventTypeNodesList[count - 1].TotalButtons)
                    );
                }
                else
                {
                    position = new Vector2(
                    TranzmitBroadcastBasePosition.x,
                    TranzmitBroadcastBasePosition.y
                    );
                }

                eventTypeNodesList[count].TranzmitNodeEventNode.SetPosition(new Rect(position, Vector2.zero));
                count++;
                if (count == eventTypeNodesList.Count())
                {
                    return;
                }
            }
        }

        if (arrangementType == ArrangementTypes.Grid)
        {
            CurrentArrangementType = ArrangementTypes.Grid;

            float broadcasterSpacing = GetMostBroadcastersInASingleEventtypeNode() * BroadcasterSpacing;

            int columns = (int)Math.Sqrt(BroadcasterNodes.Count());
            int rows = (int)Math.Ceiling(BroadcasterNodes.Count() / (float)columns);

            // Node index ;)
            var count = 0;

            List<BroadcasterNodeData> eventTypeNodesList = BroadcasterNodes.Values.ToList();

            for (int col = 0; col < columns; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    Vector2 position = new Vector2(
                        TranzmitBroadcastBasePosition.x - (row * GridLayoutSpacing_Broadcaster.PropertyValue.x),
                        TranzmitBroadcastBasePosition.y + (col * GridLayoutSpacing_Broadcaster.PropertyValue.y) + (col * broadcasterSpacing));

                    eventTypeNodesList[count].TranzmitNodeEventNode.SetPosition(new Rect(position, Vector2.zero));

                    count++;

                    if (count == BroadcasterNodes.Count())
                    {
                        return;
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Finds the most Ports in a single instance of a Subscriber Node
    /// </summary>
    /// <returns>Number of Ports in the node with most ports</returns>
    private int GetMostSubscribersInASingleSubscriberNode()
    {
        int max = 0;

        foreach (SubscriberNodeData node in SubscriberNodes)
        {
            if (node.PortEdgeEvent.Count > max)
            {
                max = node.PortEdgeEvent.Count;
            }
        }

        return max;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Finds the most Broadcasters in a single instance of an Event Type Node
    /// </summary>
    /// <returns>Number of Ports in the node with most ports</returns>
    private int GetMostBroadcastersInASingleEventtypeNode()
    {
        int max = 0;

        foreach (KeyValuePair<Tranzmit.EventNames, BroadcasterNodeData> node in BroadcasterNodes)
        {
            if (node.Value.TotalButtons > max)
            {
                max = node.Value.TotalButtons;
            }
        }

        return max;
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Focus on specified Object in the Hierarchy
    /// </summary>
    /// <param name="target">The target object which we are looking for</param>
    private void SelectObject(object target)
    {
        Selection.activeObject = target as UnityEngine.Object;
    }

    // -----------------------------------------------------------------------------------------

    public void UpdateErrorColorsOnGraphElements()
    {
        foreach (KeyValuePair<Tranzmit.EventNames, List<TranzmitDebug.LogEntry>> entry in TranzmitDebug.Failed)
        {
            foreach (TranzmitDebug.LogEntry log in entry.Value)
            {
                // BROADCASTERS
                foreach (KeyValuePair<object, TranzmitDebug.ButtonData> buttonData in log.Broadcasters)
                {
                    buttonData.Value.GraphButton.style.backgroundColor = ErrorColor.PropertyValue;

                    BroadcasterNodes[log.TranzmitEvent].TranzmitPort.portColor = ErrorColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].TranzmitPort.style.color = ErrorColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].TranzmitPort.visualClass = "2"; // HACK: Force Refresh.

                    BroadcasterNodes[log.TranzmitEvent].Port.portColor = ErrorColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].Port.style.color = ErrorColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].Port.visualClass = "2"; // HACK: Force Refresh.

                    BroadcasterNodes[log.TranzmitEvent].Edge.style.color = ErrorColor.PropertyValue;
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------------

    public void UpdateSuccessColorsOnGraphElements()
    {
        foreach (KeyValuePair<Tranzmit.EventNames, List<TranzmitDebug.LogEntry>> entry in TranzmitDebug.Success)
        {
            foreach (TranzmitDebug.LogEntry log in entry.Value)
            {
                // BROADCASTERS
                foreach (KeyValuePair<object, TranzmitDebug.ButtonData> buttonData in log.Broadcasters)
                {
                    buttonData.Value.GraphButton.style.backgroundColor = SuccessColor.PropertyValue;

                    BroadcasterNodes[log.TranzmitEvent].TranzmitPort.portColor = SuccessColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].TranzmitPort.style.color = SuccessColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].TranzmitPort.visualClass = "2"; // HACK: Force Refresh.

                    BroadcasterNodes[log.TranzmitEvent].Port.portColor = SuccessColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].Port.style.color = SuccessColor.PropertyValue;
                    BroadcasterNodes[log.TranzmitEvent].Port.visualClass = "2"; // HACK: Force Refresh.

                    BroadcasterNodes[log.TranzmitEvent].Edge.style.color = SuccessColor.PropertyValue;
                }
            }
        }

        foreach (SubscriberNodeData subNode in SubscriberNodes)
        {
            foreach (PortEdgeEventData portNodeEvent in subNode.PortEdgeEvent)
            {
                // If it has been changed in color previously then we can safely update it as they change color only on the first successful event.
                if (portNodeEvent.SubscriberPort.portColor != SilentColor.PropertyValue)
                {
                    portNodeEvent.TranzmitPort.portColor = SuccessColor.PropertyValue;
                    portNodeEvent.TranzmitPort.style.color = SuccessColor.PropertyValue;
                    portNodeEvent.TranzmitPort.visualClass = ""; // HACK: Force Refresh. Vary the input to trigger otherwise only works once.

                    portNodeEvent.SubscriberPort.style.color = SuccessColor.PropertyValue;
                    portNodeEvent.SubscriberPort.portColor = SuccessColor.PropertyValue;
                    portNodeEvent.SubscriberPort.visualClass = "";  // HACK: Force Refresh. Vary the input to trigger otherwise only works once.

                    portNodeEvent.Edge.style.color = SuccessColor.PropertyValue;
                }
            }
        }
    }
}
