using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blep.Tranzmit
{
    public class Tranzmit_Graph : EditorWindow
    {
        /// <summary>
        /// Hurrah for the video below. Pretty much the only concise resource at this time.
        /// I wanted to share/give credit so that others can start using this rather good UI!
        /// https://www.youtube.com/watch?v=7KHGH0fPL84
        /// </summary>

        // The reference to the graph that we will create
        public static Tranzmit_Graph_View _TranzmitGraphView;
  
        // 'Duration' and 'Done' are used to delay the update of the graph to allow the rest of Unity and Tranzmit to do their thing.
        private float Duration;
        private bool Done = false;

        // Menu Item to open the graph
        [MenuItem("Tranzmit/Tranzmit Graph")]
        public static void OpenTranzmitGraphWindow()
        {
            var window = GetWindow<Tranzmit_Graph>();
            window.titleContent = new GUIContent("Tranzmit Graph");
        }

        // -----------------------------------------------------------------------------------------

        private void OnEnable()
        {
            Done = false;
            Subscribe();

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // -----------------------------------------------------------------------------------------

        private void OnDisable()
        {
            //Remove from editor window
            rootVisualElement.Remove(_TranzmitGraphView);

            Unsubscribe();

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        // -----------------------------------------------------------------------------------------

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Re-establish Graph View to work in Edit Mode
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                _TranzmitGraphView.Generate();
                Subscribe();
            }
        }

        // -----------------------------------------------------------------------------------------

        protected virtual void OnEditorUpdate()
        {
            // We delay the graph creation to allow ALL Subscribers to register. Ugly but effective.
            if (Duration > 0.01f && Done == false)
            {
                Done = true;
       
                ConstructGraphView();  
                GenerateBlackBoard();
                GenerateToolbar();
            }
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Builds the Graph
        /// </summary>
        private void ConstructGraphView()
        {
            _TranzmitGraphView = new Tranzmit_Graph_View
            {
                // ASssign the name of the graph
                name = "Tranzmit Graph"
            };

            GenerateMiniMap();

            // Stretch the graphview fully over editor window
            _TranzmitGraphView.StretchToParentSize();

            //Add to the editor window
            rootVisualElement.Add(_TranzmitGraphView);

            Subscribe();
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Subscribe to the in-built Tranzmit Events
        /// </summary>
        public void Subscribe()
        {
            // Play it safe!
            Unsubscribe();

            if (_TranzmitGraphView != null && _TranzmitGraphView.Tranzmit != null)
            {
                _TranzmitGraphView.Tranzmit.EventAdded += _TranzmitGraphView.EventAdded;
                _TranzmitGraphView.Tranzmit.EventDeleted += _TranzmitGraphView.EventDeleted;
                _TranzmitGraphView.Tranzmit.TranzmitDebugReset += _TranzmitGraphView.EventTranzmitDebugReset;
                _TranzmitGraphView.Tranzmit.TranzmitDebugLogUpdated += _TranzmitGraphView.TranzmitDebugLogUpdated;
            }

#if UNITY_EDITOR
            Duration = Time.realtimeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
#endif
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Unsubscribe to the in-built Tranzmit Events
        /// </summary>
        public void Unsubscribe()
        {
            // Unsubcribe from event
            if (_TranzmitGraphView != null && _TranzmitGraphView.Tranzmit != null)
            {
                _TranzmitGraphView.Tranzmit.EventAdded -= _TranzmitGraphView.EventAdded;
                _TranzmitGraphView.Tranzmit.EventDeleted -= _TranzmitGraphView.EventDeleted;
                _TranzmitGraphView.Tranzmit.TranzmitDebugReset -= _TranzmitGraphView.EventTranzmitDebugReset;
                _TranzmitGraphView.Tranzmit.TranzmitDebugLogUpdated -= _TranzmitGraphView.TranzmitDebugLogUpdated;
            }

#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
#endif
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// used to create the floating 'in Gapgh' minimap for easy naviagtion when there are lots of nodes.
        /// </summary>
        private void GenerateMiniMap()
        {
            var miniMap = new MiniMap { anchored = false };
            var cords = _TranzmitGraphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x -10, 32));
            miniMap.SetPosition(new Rect(cords.x, cords.y, 200, 140));
            _TranzmitGraphView.Add(miniMap);
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Generate the toolbar found at the top of the graph. 
        /// </summary>
        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var buildButton = new Button(clickEvent: () => { _TranzmitGraphView.Generate(); Subscribe(); });
            buildButton.text = "BUILD / REFRESH";
            toolbar.Add(buildButton);

            var verticalButton = new Button(clickEvent: () => { 
                _TranzmitGraphView.Arrange_Subscriber_Results(Tranzmit_Graph_View.ArrangementTypes.Vertical);
                _TranzmitGraphView.Arrange_Broadcaster_Results(Tranzmit_Graph_View.ArrangementTypes.Vertical);
            });
            verticalButton.text = "Vertical";
            toolbar.Add(verticalButton);

            var gridButton = new Button(clickEvent: () => {
                _TranzmitGraphView.Arrange_Subscriber_Results(Tranzmit_Graph_View.ArrangementTypes.Grid);
                _TranzmitGraphView.Arrange_Broadcaster_Results(Tranzmit_Graph_View.ArrangementTypes.Grid);
            });
            gridButton.text = "Grid";
            toolbar.Add(gridButton);

            // Adds toolbar to the graph
            rootVisualElement.Add(toolbar);
        }

        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Generate the BlackBoard that will hold Various Settings for this Graph
        /// </summary>
        private void GenerateBlackBoard()
        {
            var blackBoard = new Blackboard(_TranzmitGraphView);
            blackBoard.Add(new BlackboardSection { title = "Graph Settings" });
            blackBoard.SetPosition(new Rect(10, 30, 300, 300));

            _TranzmitGraphView.Add(blackBoard);

            _TranzmitGraphView.Blackboard = blackBoard;

            Generate_Subscriber_Grid_Spacing_Field();
            Generate_Broadcaster_Grid_Spacing_Field();
            Generate_Subscriber_Vertical_Spacing_Field();
            Generate_Broadcaster_Vertical_Spacing_Field();
            Generate_BlackBoard_Success_Color_Field();
            Generate_BlackBoard_Error_Color_Field();
        }

        // -----------------------------------------------------------------------------------------

        public void Generate_Subscriber_Vertical_Spacing_Field()
        {
            var floatProp = new FloatProperty();
            floatProp.PropertyName = _TranzmitGraphView.VerticalYSpacing_Subscriber.PropertyName;
            floatProp.PropertyValue = _TranzmitGraphView.VerticalYSpacing_Subscriber.PropertyValue;

            var visualElement = new VisualElement();
            var blackboardField = new BlackboardField { text = floatProp.PropertyName, typeText = "" };
            visualElement.Add(blackboardField);

            var field = new FloatField("Value:");
            field.value = floatProp.PropertyValue;

            field.RegisterValueChangedCallback(ChangeEvent =>
            {
                _TranzmitGraphView.VerticalYSpacing_Subscriber.PropertyValue = field.value;

                if (_TranzmitGraphView.CurrentArrangementType == Tranzmit_Graph_View.ArrangementTypes.Vertical)
                {
                    _TranzmitGraphView.Arrange_Subscriber_Results(Tranzmit_Graph_View.ArrangementTypes.Vertical);
                }
            });

            var blackBoardValueRow = new BlackboardRow(blackboardField, field);
            visualElement.Add(blackBoardValueRow);

            _TranzmitGraphView.Blackboard.Add(visualElement);
        }

        // -----------------------------------------------------------------------------------------

        public void Generate_Broadcaster_Vertical_Spacing_Field()
        {
            var floatProp = new FloatProperty();
            floatProp.PropertyName = _TranzmitGraphView.VerticalYSpacing_Broadcaster.PropertyName;
            floatProp.PropertyValue = _TranzmitGraphView.VerticalYSpacing_Broadcaster.PropertyValue;

            var visualElement = new VisualElement();
            var blackboardField = new BlackboardField { text = floatProp.PropertyName, typeText = "" };
            visualElement.Add(blackboardField);

            var field = new FloatField("Value:");
            field.value = floatProp.PropertyValue;

            field.RegisterValueChangedCallback(ChangeEvent =>
            {
                _TranzmitGraphView.VerticalYSpacing_Broadcaster.PropertyValue = field.value;

                if (_TranzmitGraphView.CurrentArrangementType == Tranzmit_Graph_View.ArrangementTypes.Vertical)
                {
                    _TranzmitGraphView.Arrange_Broadcaster_Results(Tranzmit_Graph_View.ArrangementTypes.Vertical);
                }
            });

            var blackBoardValueRow = new BlackboardRow(blackboardField, field);
            visualElement.Add(blackBoardValueRow);

            _TranzmitGraphView.Blackboard.Add(visualElement);
        }

        // -----------------------------------------------------------------------------------------

        public void Generate_Broadcaster_Grid_Spacing_Field()
        {
            var v2 = new Vector2Property();
            v2.PropertyName = _TranzmitGraphView.GridLayoutSpacing_Broadcaster.PropertyName;
            v2.PropertyValue = _TranzmitGraphView.GridLayoutSpacing_Broadcaster.PropertyValue;

            var visualElement = new VisualElement();
            var blackboardField = new BlackboardField { text = v2.PropertyName, typeText = "" };
            visualElement.Add(blackboardField);

            var field = new Vector2Field("Value:");
            field.value = v2.PropertyValue;

            field.RegisterValueChangedCallback(ChangeEvent =>
            {
                _TranzmitGraphView.GridLayoutSpacing_Broadcaster.PropertyValue = field.value;

                if (_TranzmitGraphView.CurrentArrangementType == Tranzmit_Graph_View.ArrangementTypes.Grid)
                {
                    _TranzmitGraphView.Arrange_Broadcaster_Results(Tranzmit_Graph_View.ArrangementTypes.Grid);
                }
            });

            var blackBoardValueRow = new BlackboardRow(blackboardField, field);
            visualElement.Add(blackBoardValueRow);

            _TranzmitGraphView.Blackboard.Add(visualElement);
        }

        // -----------------------------------------------------------------------------------------

        public void Generate_Subscriber_Grid_Spacing_Field()
        {
            var v2 = new Vector2Property();
            v2.PropertyName = _TranzmitGraphView.GridLayoutSpacing_Subscriber.PropertyName;
            v2.PropertyValue = _TranzmitGraphView.GridLayoutSpacing_Subscriber.PropertyValue;

            var visualElement = new VisualElement();
            var blackboardField = new BlackboardField { text = v2.PropertyName, typeText = "" };
            visualElement.Add(blackboardField);

            var field = new Vector2Field("Value:");
            field.value = v2.PropertyValue;

            field.RegisterValueChangedCallback(ChangeEvent =>
            {
                _TranzmitGraphView.GridLayoutSpacing_Subscriber.PropertyValue = field.value;

                if (_TranzmitGraphView.CurrentArrangementType == Tranzmit_Graph_View.ArrangementTypes.Grid)
                {
                    _TranzmitGraphView.Arrange_Subscriber_Results(Tranzmit_Graph_View.ArrangementTypes.Grid);
                }
            });

            var blackBoardValueRow = new BlackboardRow(blackboardField, field);
            visualElement.Add(blackBoardValueRow);

            _TranzmitGraphView.Blackboard.Add(visualElement);
        }

        // -----------------------------------------------------------------------------------------

        public void Generate_BlackBoard_Success_Color_Field()
        {
            var color = new ColorProperty();
            color.PropertyName = _TranzmitGraphView.SuccessColor.PropertyName;
            color.PropertyValue = _TranzmitGraphView.SuccessColor.PropertyValue;

            var visualElement = new VisualElement();
            var blackboardField = new BlackboardField { text = color.PropertyName, typeText = "" };
            visualElement.Add(blackboardField);

            var field = new ColorField("Value:");
            field.value = color.PropertyValue;

            field.RegisterValueChangedCallback(ChangeEvent =>
            {
                _TranzmitGraphView.SuccessColor.PropertyValue = field.value;
                _TranzmitGraphView.UpdateSuccessColorsOnGraphElements();
            });

            var blackBoardValueRow = new BlackboardRow(blackboardField, field);
            visualElement.Add(blackBoardValueRow);

            _TranzmitGraphView.Blackboard.Add(visualElement);
        }

        // -----------------------------------------------------------------------------------------

        public void Generate_BlackBoard_Error_Color_Field()
        {
            var color = new ColorProperty();
            color.PropertyName = _TranzmitGraphView.ErrorColor.PropertyName;
            color.PropertyValue = _TranzmitGraphView.ErrorColor.PropertyValue;

            var visualElement = new VisualElement();
            var blackboardField = new BlackboardField { text = color.PropertyName, typeText = "" };
            visualElement.Add(blackboardField);

            var field = new ColorField("Value:");
            field.value = color.PropertyValue;

            field.RegisterValueChangedCallback(ChangeEvent =>
            {
                _TranzmitGraphView.ErrorColor.PropertyValue = field.value;
                _TranzmitGraphView.UpdateErrorColorsOnGraphElements();
            });

            var blackBoardValueRow = new BlackboardRow(blackboardField, field);
            visualElement.Add(blackBoardValueRow);

            _TranzmitGraphView.Blackboard.Add(visualElement);
        }
    }
}