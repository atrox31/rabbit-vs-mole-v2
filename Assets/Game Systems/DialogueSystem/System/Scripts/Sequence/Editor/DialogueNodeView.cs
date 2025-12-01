// DialogueNodeView.cs (w Assets/Editor)
using DialogueSystem.Nodes;
using DialogueSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    // Custom Node class for the visual representation of DialogueNode data
    public class DialogueNodeView : Node
    {
        public DialogueNode NodeData;
        public Port InputPort;
        public Port OutputPort;

        private TextField _dialogueText;
        private ObjectField _actorField;
        private DropdownField _poseDropdown;
        private EnumField _sideField;
        private DialogueGraphEditor _editor;
        private SerializedProperty _nodeProperty;
        private bool _isLoading = true; // Flag to prevent SetDirty during loading

        public DialogueNodeView(DialogueNode nodeData, DialogueGraphEditor editor)
        {
            NodeData = nodeData;
            _editor = editor;
            title = "Dialogue Node";

            // Set the position using the data from the ScriptableObject
            SetPosition(new Rect(NodeData.EditorPosition, new Vector2(300, 200)));

            CreateInputPorts();
            CreateOutputPorts();
            SetupCustomDataFields();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            // Mark loading as complete after a frame to allow initial geometry setup
            schedule.Execute(() => { _isLoading = false; });
        }

        private void CreateInputPorts()
        {
            // Always create one input port (previous node)
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);
        }

        private void CreateOutputPorts()
        {
            // For now, only one output port (for simple dialogue flow)
            OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            //   TODO: przerobi� na r�ne opcje z warunkami jakimi� najlpeijej              /\
            //                                trza bedzie zmieni� �eby da�o si� r�ne robi� ||
        }

        private void SetupCustomDataFields()
        {
            // Get SerializedProperty for this node
            _nodeProperty = _editor.GetNodeProperty(NodeData);
            if (_nodeProperty == null)
            {
                Debug.LogError("Failed to find SerializedProperty for DialogueNode.");
                return;
            }

            // Update SerializedProperty before reading values
            _nodeProperty.serializedObject.Update();

            SerializedProperty poseProperty = _nodeProperty.FindProperty("_poseName");
            if (poseProperty == null)
            {
                Debug.LogError("Failed to find _poseName property.");
                return;
            }

            // 1. Actor Field (ObjectField for your Actor ScriptableObject)
            SerializedProperty actorProperty = _nodeProperty.FindProperty("_actor");
            _actorField = new ObjectField("Actor:")
            {
                objectType = typeof(Actor),
                allowSceneObjects = false,
                value = actorProperty != null ? actorProperty.objectReferenceValue as Actor : NodeData._actor
            };
            _actorField.RegisterValueChangedCallback(evt =>
            {
                if (actorProperty != null)
                {
                    actorProperty.objectReferenceValue = evt.newValue;
                    actorProperty.serializedObject.ApplyModifiedProperties();
                }
                NodeData._actor = evt.newValue as Actor;
                _editor.SetDirty();
                UpdatePoseDropdownOptions();
            });

            // 2. Pose Field
            _poseDropdown = new DropdownField("Pose:");
            _poseDropdown.style.width = 250; 
            UpdatePoseDropdownOptions();

            _poseDropdown.RegisterValueChangedCallback(evt =>
            {
                if (poseProperty != null)
                {
                    poseProperty.stringValue = evt.newValue;
                    poseProperty.serializedObject.ApplyModifiedProperties();
                }
                NodeData._poseName = evt.newValue;
                _editor.SetDirty();
            });

            // 3. Screen Position Field (EnumField for the Side enum)
            SerializedProperty screenPositionProperty = _nodeProperty.FindProperty("ScreenPosition");
            _sideField = new EnumField("Screen Position:", ((ActorSideOnScreen)0).GetFirstValue())
            {
                value = screenPositionProperty != null ? (ActorSideOnScreen)screenPositionProperty.enumValueIndex : NodeData.ScreenPosition
            };
            _sideField.RegisterValueChangedCallback(evt =>
            {
                if (screenPositionProperty != null)
                {
                    screenPositionProperty.enumValueIndex = (int)(ActorSideOnScreen)evt.newValue;
                    screenPositionProperty.serializedObject.ApplyModifiedProperties();
                }
                NodeData.ScreenPosition = (DialogueSystem.ActorSideOnScreen)evt.newValue;
                _editor.SetDirty();
            });

            // 4. Dialogue Text Field
            SerializedProperty textProperty = _nodeProperty.FindProperty("text");
            _dialogueText = new TextField("Dialogue:", -1, true, false, '\0')
            {
                value = textProperty != null ? textProperty.stringValue : (NodeData.text ?? "New dialogue text..."),
                multiline = true
            };
            _dialogueText.style.minHeight = 80;
            _dialogueText.style.width = 250;
            _dialogueText.style.whiteSpace = WhiteSpace.Normal;
            _dialogueText.Q<TextElement>().style.whiteSpace = WhiteSpace.Normal;

            _dialogueText.RegisterValueChangedCallback(evt =>
            {
                if (textProperty != null)
                {
                    textProperty.stringValue = evt.newValue;
                    textProperty.serializedObject.ApplyModifiedProperties();
                }
                NodeData.text = evt.newValue;
                _editor.SetDirty();
            });

            // Add fields to the node's main body
            extensionContainer.Add(_actorField); 
            extensionContainer.Add(_poseDropdown); 
            extensionContainer.Add(_sideField);
            extensionContainer.Add(_dialogueText);

            RefreshPorts();
            RefreshExpandedState();
        }
        private void UpdatePoseDropdownOptions()
        {
            List<string> poseNames = new List<string> { "(No Pose Selected)" };
            string currentValue = NodeData._poseName;

            if (NodeData._actor != null && NodeData._actor.poses != null)
            {
                // Add actual pose names
                poseNames.AddRange(NodeData._actor.poses.Select(p => p.name));
            }
            _poseDropdown.choices = poseNames;
            if (poseNames.Contains(currentValue))
            {
                _poseDropdown.value = currentValue;
            }
            else
            {
                _poseDropdown.value = poseNames[0];
                NodeData._poseName = "";
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Don't mark as dirty during initial loading
            if (_isLoading) return;
            
            // The new position is stored in the newRect of the GeometryChangedEvent
            Vector2 newPosition = GetPosition().position;
            NodeData.EditorPosition = newPosition;
            
            // Update SerializedProperty for position
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
            
            // 1. Kopiuj zawartość
            evt.menu.AppendAction(
                "Copy Content",
                (action) => CopyNodeContent(),
                DropdownMenuAction.AlwaysEnabled
            );
            
            // 2. Wklej zawartość
            evt.menu.AppendAction(
                "Paste Content",
                (action) => PasteNodeContent(),
                DropdownMenuAction.AlwaysEnabled
            );
            
            evt.menu.AppendSeparator();
            
            // 3. Duplikuj
            evt.menu.AppendAction(
                "Duplicate",
                (action) => DuplicateNode(),
                DropdownMenuAction.AlwaysEnabled
            );
            
            // 4. Usuń
            evt.menu.AppendAction(
                "Delete",
                (action) => DeleteNode(),
                DropdownMenuAction.AlwaysEnabled
            );
        }

        private static DialogueNode _copiedNodeContent = null;

        private void CopyNodeContent()
        {
            // Create a copy of the node data (without GUID and position)
            _copiedNodeContent = new DialogueNode(Vector2.zero)
            {
                _actor = NodeData._actor,
                _poseName = NodeData._poseName,
                ScreenPosition = NodeData.ScreenPosition,
                text = NodeData.text
            };
            Debug.Log("Node content copied.");
        }

        private void PasteNodeContent()
        {
            if (_copiedNodeContent == null)
            {
                Debug.LogWarning("No node content to paste.");
                return;
            }

            // Update SerializedProperty
            if (_nodeProperty != null)
            {
                _nodeProperty.serializedObject.Update();
                
                SerializedProperty actorProperty = _nodeProperty.FindProperty("_actor");
                if (actorProperty != null)
                {
                    actorProperty.objectReferenceValue = _copiedNodeContent._actor;
                }
                
                SerializedProperty poseProperty = _nodeProperty.FindProperty("_poseName");
                if (poseProperty != null)
                {
                    poseProperty.stringValue = _copiedNodeContent._poseName;
                }
                
                SerializedProperty screenPositionProperty = _nodeProperty.FindProperty("ScreenPosition");
                if (screenPositionProperty != null)
                {
                    screenPositionProperty.enumValueIndex = (int)_copiedNodeContent.ScreenPosition;
                }
                
                SerializedProperty textProperty = _nodeProperty.FindProperty("text");
                if (textProperty != null)
                {
                    textProperty.stringValue = _copiedNodeContent.text;
                }
                
                _nodeProperty.serializedObject.ApplyModifiedProperties();
            }

            // Update NodeData
            NodeData._actor = _copiedNodeContent._actor;
            NodeData._poseName = _copiedNodeContent._poseName;
            NodeData.ScreenPosition = _copiedNodeContent.ScreenPosition;
            NodeData.text = _copiedNodeContent.text;

            // Update UI fields
            if (_actorField != null) _actorField.value = NodeData._actor;
            if (_poseDropdown != null)
            {
                UpdatePoseDropdownOptions();
                _poseDropdown.value = NodeData._poseName;
            }
            if (_sideField != null) _sideField.value = NodeData.ScreenPosition;
            if (_dialogueText != null) _dialogueText.value = NodeData.text;

            _editor.SetDirty();
            Debug.Log("Node content pasted.");
        }

        private void DuplicateNode()
        {
            if (_editor == null || _editor.CurrentGraph == null) return;

            // Get the graph view to add the new node
            var graphView = _editor.GetGraphView();
            if (graphView == null) return;

            // Create new node with offset position
            Vector2 newPosition = NodeData.EditorPosition + new Vector2(50, 50);
            var newNodeData = new DialogueNode(newPosition)
            {
                _actor = NodeData._actor,
                _poseName = NodeData._poseName,
                ScreenPosition = NodeData.ScreenPosition,
                text = NodeData.text
            };

            // Add to graph using the editor method (updates SerializedObject)
            _editor.AddNodeToGraph(newNodeData);
            
            // Create view and add to graph view
            var newNodeView = new DialogueNodeView(newNodeData, _editor);
            graphView.AddElement(newNodeView);

            _editor.SetDirty();
            Debug.Log("Node duplicated.");
        }

        private void DeleteNode()
        {
            if (_editor == null || _editor.CurrentGraph == null) return;

            // Get the graph view
            var graphView = _editor.GetGraphView();
            if (graphView == null) return;

            // Remove all edges connected to this node
            var edgesToRemove = graphView.edges.Where(e => 
                e.output.node == this || e.input.node == this).ToList();
            
            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }

            // Remove from graph data
            _editor.CurrentGraph.Nodes.Remove(NodeData);

            // Remove from view
            graphView.RemoveElement(this);

            _editor.SetDirty();
            Debug.Log("Node deleted.");
        }
    }
}