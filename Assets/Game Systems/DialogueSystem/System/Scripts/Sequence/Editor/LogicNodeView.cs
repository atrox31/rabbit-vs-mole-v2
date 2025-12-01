using DialogueSystem.Nodes;
using DialogueSystem;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Visual representation of a LogicNode in the graph editor.
    /// </summary>
    public class LogicNodeView : Node
    {
        public LogicNode NodeData;
        public Port InputPort;
        public Port ConditionInputPort;
        public Port OutputPortTrue;
        public Port OutputPortFalse;

        private DialogueGraphEditor _editor;
        private SerializedProperty _nodeProperty;
        private bool _isLoading = true;

        public LogicNodeView(LogicNode nodeData, DialogueGraphEditor editor)
        {
            NodeData = nodeData;
            _editor = editor;
            title = "Logic";
            
            // Set node color
            titleContainer.style.backgroundColor = DialogueGraphColors.LogicNodeColor;

            // Set the position using the data from the ScriptableObject
            SetPosition(new Rect(NodeData.EditorPosition, new Vector2(200, 150)));

            CreateInputPorts();
            CreateOutputPorts();
            SetupCustomDataFields();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            // Mark loading as complete after a frame
            schedule.Execute(() => { _isLoading = false; });
        }

        private void CreateInputPorts()
        {
            // Input port for flow control
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);

            // Condition input port (bool)
            ConditionInputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            ConditionInputPort.portName = "Condition";
            inputContainer.Add(ConditionInputPort);
        }

        private void CreateOutputPorts()
        {
            // True output port
            OutputPortTrue = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPortTrue.portName = "True";
            outputContainer.Add(OutputPortTrue);

            // False output port
            OutputPortFalse = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPortFalse.portName = "False";
            outputContainer.Add(OutputPortFalse);
        }

        private void SetupCustomDataFields()
        {
            // Get SerializedProperty for this node
            _nodeProperty = _editor.GetLogicNodeProperty(NodeData);
            if (_nodeProperty == null)
            {
                Debug.LogError("Failed to find SerializedProperty for LogicNode.");
                return;
            }

            // Update SerializedProperty before reading values
            _nodeProperty.serializedObject.Update();

            // Create condition field (only visible if not connected)
            SerializedProperty conditionProperty = _nodeProperty.FindProperty("Condition");
            if (conditionProperty != null)
            {
                var graphView = GetFirstAncestorOfType<DialogueGraphView>();
                bool hasConditionConnection = false;
                if (graphView != null)
                {
                    hasConditionConnection = graphView.edges.ToList().Any(e => 
                        e.input != null && 
                        e.input.node == this && 
                        e.input == ConditionInputPort);
                }

                if (!hasConditionConnection)
                {
                    var toggle = new Toggle("Condition")
                    {
                        bindingPath = conditionProperty.propertyPath
                    };
                    toggle.BindProperty(conditionProperty);
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        _nodeProperty.serializedObject.ApplyModifiedProperties();
                        _editor.SetDirty();
                    });
                    extensionContainer.Add(toggle);
                }
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_isLoading || _editor.IsLoading) return;

            Vector2 newPosition = GetPosition().position;
            
            // Only update if position actually changed
            if (NodeData.EditorPosition == newPosition)
                return;
            
            NodeData.EditorPosition = newPosition;

            if (_nodeProperty != null)
            {
                SerializedProperty positionProperty = _nodeProperty.FindProperty("EditorPosition");
                if (positionProperty != null)
                {
                    positionProperty.vector2Value = newPosition;
                    positionProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            _editor.SetDirty();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Duplicate",
                (action) => DuplicateNode(),
                DropdownMenuAction.AlwaysEnabled
            );

            evt.menu.AppendAction(
                "Delete",
                (action) => DeleteNode(),
                DropdownMenuAction.AlwaysEnabled
            );
        }

        private void DuplicateNode()
        {
            if (_editor == null || _editor.CurrentGraph == null) return;

            var graphView = _editor.GetGraphView();
            if (graphView == null) return;

            Vector2 newPosition = NodeData.EditorPosition + new Vector2(50, 50);
            var newNodeData = new LogicNode(newPosition);
            newNodeData.Condition = NodeData.Condition;

            // Add to graph using the editor method (updates SerializedObject)
            _editor.AddLogicNodeToGraph(newNodeData);

            var newNodeView = new LogicNodeView(newNodeData, _editor);
            graphView.AddElement(newNodeView);

            _editor.SetDirty();
            Debug.Log("Logic node duplicated.");
        }

        private void DeleteNode()
        {
            if (_editor == null || _editor.CurrentGraph == null) return;

            var graphView = _editor.GetGraphView();
            if (graphView == null) return;

            var edgesToRemove = graphView.edges.Where(e =>
                e.output.node == this || e.input.node == this).ToList();

            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }

            _editor.CurrentGraph.LogicNodes.Remove(NodeData);
            graphView.RemoveElement(this);

            _editor.SetDirty();
            Debug.Log("Logic node deleted.");
        }
    }
}


