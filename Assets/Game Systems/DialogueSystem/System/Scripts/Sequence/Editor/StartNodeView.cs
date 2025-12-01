using DialogueSystem.Nodes;
using DialogueSystem;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Visual representation of a StartNode in the graph editor.
    /// </summary>
    public class StartNodeView : Node
    {
        public StartNode NodeData;
        public Port OutputPort;

        private DialogueGraphEditor _editor;
        private SerializedProperty _nodeProperty;
        private bool _isLoading = true;

        public StartNodeView(StartNode nodeData, DialogueGraphEditor editor)
        {
            NodeData = nodeData;
            _editor = editor;
            title = "Start";
            
            // Set node color to distinguish from other nodes
            titleContainer.style.backgroundColor = DialogueGraphColors.StartNodeColor;

            // Set the position using the data from the ScriptableObject
            SetPosition(new Rect(NodeData.EditorPosition, new Vector2(150, 100)));

            CreateOutputPorts();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            // Get SerializedProperty for position updates
            _nodeProperty = _editor.GetStartNodeProperty();
            
            // Mark loading as complete after a frame
            schedule.Execute(() => { _isLoading = false; });
        }

        private void CreateOutputPorts()
        {
            OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
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
            // Start node cannot be deleted, so don't show delete option
            // Only allow basic node operations
            base.BuildContextualMenu(evt);
            
            // Remove delete option if it exists
            evt.menu.MenuItems().RemoveAll(item => 
            {
                if (item is DropdownMenuAction action)
                {
                    return action.name == "Delete" || action.name.Contains("Delete");
                }
                return false;
            });
        }
    }
}

