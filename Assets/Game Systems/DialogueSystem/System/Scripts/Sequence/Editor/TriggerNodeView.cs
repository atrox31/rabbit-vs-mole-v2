using DialogueSystem.Nodes;
using DialogueSystem;
using DialogueSystem.Trigger;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Visual representation of a TriggerNode in the graph editor.
    /// </summary>
    public class TriggerNodeView : Node
    {
        public TriggerNode NodeData;
        public Port InputPort;
        public Port OutputPort;
        public Dictionary<string, Port> ArgumentInputPorts = new Dictionary<string, Port>();

        private Label _triggerTypeLabel;
        private VisualElement _argumentsContainer;
        private DialogueGraphEditor _editor;
        private SerializedProperty _nodeProperty;
        private SerializedProperty _triggerTypeNameProperty;
        private SerializedProperty _argumentsProperty;
        private bool _isLoading = true;
        private Dictionary<FieldInfo, VisualElement> _argumentFields = new Dictionary<FieldInfo, VisualElement>();

        public TriggerNodeView(TriggerNode nodeData, DialogueGraphEditor editor)
        {
            NodeData = nodeData;
            _editor = editor;
            
            // Set node color to distinguish from dialogue nodes
            titleContainer.style.backgroundColor = DialogueGraphColors.TriggerNodeColor;

            // Set the position using the data from the ScriptableObject
            SetPosition(new Rect(NodeData.EditorPosition, new Vector2(300, 150)));

            CreateInputPorts();
            CreateOutputPorts();
            SetupCustomDataFields();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            // Listen for edge connections/disconnections to update field states
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            
            // Mark loading as complete after a frame
            schedule.Execute(() => { _isLoading = false; });
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // No periodic checking - fields will be shown/hidden based on connections
        }

        public void OnEdgeConnected(Edge edge)
        {
            // Check if this edge connects to one of our argument input ports
            if (edge.input != null && ArgumentInputPorts.ContainsValue(edge.input))
            {
                RefreshTriggerDisplay();
                _editor.SetDirty();
            }
        }

        public void OnEdgeDisconnected(Edge edge)
        {
            // Check if this edge was connected to one of our argument input ports
            if (edge.input != null && ArgumentInputPorts.ContainsValue(edge.input))
            {
                RefreshTriggerDisplay();
                _editor.SetDirty();
            }
        }

        private void CreateInputPorts()
        {
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);
        }

        private void CreateOutputPorts()
        {
            OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        private void UpdateTitle()
        {
            if (_triggerTypeNameProperty == null)
            {
                title = "Trigger Node";
                return;
            }

            string triggerTypeName = _triggerTypeNameProperty.stringValue;
            if (string.IsNullOrEmpty(triggerTypeName))
            {
                title = "Trigger Node";
            }
            else
            {
                var triggerType = TriggerTypeHelper.GetTypeFromFullName(triggerTypeName);
                if (triggerType != null)
                {
                    title = $"Trigger Type: {TriggerTypeHelper.GetTriggerDisplayName(triggerType)}";
                }
                else
                {
                    title = $"Trigger Type: {triggerTypeName}";
                }
            }
        }

        private void SetupCustomDataFields()
        {
            // Get SerializedProperty for this node
            _nodeProperty = _editor.GetTriggerNodeProperty(NodeData);
            if (_nodeProperty == null)
            {
                Debug.LogError("Failed to find SerializedProperty for TriggerNode.");
                return;
            }

            // Update SerializedProperty before reading values
            _nodeProperty.serializedObject.Update();

            _triggerTypeNameProperty = _nodeProperty.FindProperty("_triggerTypeName");
            _argumentsProperty = _nodeProperty.FindProperty("_arguments");

            // Create trigger type label
            _triggerTypeLabel = new Label("Trigger Type: None");
            _triggerTypeLabel.style.marginTop = 5;
            _triggerTypeLabel.style.marginBottom = 5;
            extensionContainer.Add(_triggerTypeLabel);

            // Create arguments container
            _argumentsContainer = new VisualElement();
            extensionContainer.Add(_argumentsContainer);

            // Load current trigger if any
            RefreshTriggerDisplay();

            RefreshPorts();
            RefreshExpandedState();
        }

        private void RefreshTriggerDisplay()
        {
            _argumentsContainer.Clear();
            _argumentFields.Clear();

            // Remove old argument input ports (but keep the main InputPort)
            foreach (var port in ArgumentInputPorts.Values)
            {
                if (port != InputPort)
                {
                    inputContainer.Remove(port);
                }
            }
            ArgumentInputPorts.Clear();

            if (_triggerTypeNameProperty == null || _argumentsProperty == null)
                return;

            _nodeProperty.serializedObject.Update();

            string triggerTypeName = _triggerTypeNameProperty.stringValue;
            
            if (string.IsNullOrEmpty(triggerTypeName))
            {
                _triggerTypeLabel.text = "Trigger Type: None (Right-click to select)";
                return;
            }

            var triggerType = TriggerTypeHelper.GetTypeFromFullName(triggerTypeName);
            if (triggerType == null)
            {
                _triggerTypeLabel.text = $"Trigger Type: Unknown ({triggerTypeName})";
                return;
            }

            _triggerTypeLabel.text = $"Trigger Type: {TriggerTypeHelper.GetTriggerDisplayName(triggerType)}";
            UpdateTitle(); // Update the node title

            // Get arguments type and create fields
            var argsType = TriggerTypeHelper.GetArgumentsType(triggerType);
            if (argsType == null)
            {
                _argumentsContainer.Add(new Label("No arguments type found for this trigger."));
                return;
            }

            // Ensure arguments object exists
            if (_argumentsProperty.managedReferenceValue == null)
            {
                var newArgs = TriggerTypeHelper.CreateArgumentsInstance(triggerType);
                _argumentsProperty.managedReferenceValue = newArgs;
                _nodeProperty.serializedObject.ApplyModifiedProperties();
            }

            // Create fields for all public fields in arguments type
            var fields = argsType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Skip the Name field (it's handled separately)
                if (field.Name == "Name")
                    continue;

                var fieldProperty = _argumentsProperty.FindPropertyRelative(field.Name);
                if (fieldProperty == null)
                    continue;

                // Check if this field has a data connection by checking actual edges in graph
                bool hasConnection = false;
                var graphView = GetFirstAncestorOfType<DialogueGraphView>();
                if (graphView != null)
                {
                    hasConnection = graphView.edges.ToList().Any(e => 
                        e.input != null && 
                        e.input.node == this && 
                        e.input.portName == field.Name);
                }

                // Create input port for this field
                var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, field.FieldType);
                inputPort.portName = field.Name;
                inputContainer.Add(inputPort);
                ArgumentInputPorts[field.Name] = inputPort;

                // Create field element (hidden if connected, otherwise visible)
                // For DT_SetActive, GameObject field should always be hidden (only via input)
                bool shouldHideField = hasConnection;
                if (NodeData.TriggerTypeName != null && 
                    NodeData.TriggerTypeName.Contains("DT_SetActive") && 
                    field.FieldType == typeof(GameObject) && 
                    field.Name == "Object")
                {
                    shouldHideField = true; // Always hide GameObject field for DT_SetActive
                }

                if (!shouldHideField)
                {
                    VisualElement fieldElement = CreateFieldForProperty(fieldProperty, field, false);
                    if (fieldElement != null)
                    {
                        _argumentsContainer.Add(fieldElement);
                        _argumentFields[field] = fieldElement;
                    }
                }
            }
        }

        private VisualElement CreateFieldForProperty(SerializedProperty property, FieldInfo fieldInfo, bool isConnected = false)
        {
            VisualElement fieldElement = null;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                // Configure allowSceneObjects based on trigger type
                bool allowSceneObjects = true;
                if (fieldInfo.FieldType == typeof(GameObject) && 
                    !string.IsNullOrEmpty(NodeData.TriggerTypeName))
                {
                    // DT_Instantiate - only assets (prefabs)
                    if (NodeData.TriggerTypeName.Contains("DT_Instantiate"))
                {
                        allowSceneObjects = false;
                    }
                    // DT_SetActive - allow scene objects
                    else if (NodeData.TriggerTypeName.Contains("DT_SetActive"))
                    {
                        allowSceneObjects = true;
                    }
                    // DT_PlaySound, DT_PlaySound3D - only assets (AudioClip)
                    else if (NodeData.TriggerTypeName.Contains("DT_PlaySound"))
                    {
                        allowSceneObjects = false;
                    }
                    // TD_GameObject - allow scene objects
                    else if (fieldInfo.FieldType == typeof(GameObject))
                    {
                        allowSceneObjects = true;
                    }
                }

                var objectField = new ObjectField(fieldInfo.Name)
                    {
                    objectType = fieldInfo.FieldType,
                    allowSceneObjects = allowSceneObjects,
                    bindingPath = property.propertyPath
                };
                objectField.SetEnabled(!isConnected); // Disable if connected to data node
                objectField.BindProperty(property);
                objectField.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                    _editor.SetDirty();
                });
                fieldElement = objectField;
                    }
            else if (property.propertyType == SerializedPropertyType.Boolean)
            {
                var toggle = new Toggle(fieldInfo.Name)
                    {
                    bindingPath = property.propertyPath
                };
                toggle.SetEnabled(!isConnected);
                toggle.BindProperty(property);
                toggle.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                    _editor.SetDirty();
                });
                fieldElement = toggle;
                }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                var intField = new IntegerField(fieldInfo.Name)
                {
                    bindingPath = property.propertyPath
                };
                intField.SetEnabled(!isConnected);
                intField.BindProperty(property);
                intField.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                    _editor.SetDirty();
                });
                fieldElement = intField;
                    }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                var floatField = new FloatField(fieldInfo.Name)
                    {
                    bindingPath = property.propertyPath
                };
                floatField.SetEnabled(!isConnected);
                floatField.BindProperty(property);
                floatField.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                    _editor.SetDirty();
                });
                fieldElement = floatField;
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                var textField = new TextField(fieldInfo.Name)
                {
                    bindingPath = property.propertyPath
                };
                textField.SetEnabled(!isConnected);
                textField.BindProperty(property);
                textField.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                    _editor.SetDirty();
                });
                fieldElement = textField;
            }
            else if (property.propertyType == SerializedPropertyType.Vector3)
            {
                var vectorField = new Vector3Field(fieldInfo.Name)
                {
                    bindingPath = property.propertyPath
                };
                vectorField.SetEnabled(!isConnected);
                vectorField.BindProperty(property);
                vectorField.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                    _editor.SetDirty();
                });
                fieldElement = vectorField;
            }
            else if (property.propertyType == SerializedPropertyType.Quaternion)
            {
                var quaternionField = new Vector4Field(fieldInfo.Name)
                {
                    bindingPath = property.propertyPath
                };
                quaternionField.SetEnabled(!isConnected);
                quaternionField.BindProperty(property);
                quaternionField.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                _editor.SetDirty();
            });
                fieldElement = quaternionField;
            }

            return fieldElement;
        }

        private void SetTriggerType(Type triggerType)
        {
            if (_triggerTypeNameProperty == null || _argumentsProperty == null)
                return;

            _nodeProperty.serializedObject.Update();

            if (triggerType == null)
            {
                _triggerTypeNameProperty.stringValue = string.Empty;
                _argumentsProperty.managedReferenceValue = null;
            }
            else
            {
                _triggerTypeNameProperty.stringValue = TriggerTypeHelper.GetFullTypeName(triggerType);
                var args = TriggerTypeHelper.CreateArgumentsInstance(triggerType);
                _argumentsProperty.managedReferenceValue = args;
            }

            _nodeProperty.serializedObject.ApplyModifiedProperties();
            UpdateTitle(); // Update the node title
            RefreshTriggerDisplay();
            _editor.SetDirty();
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

            // Add trigger type selection submenu
            var triggerTypes = TriggerTypeHelper.GetTriggerTypes();
            if (triggerTypes.Count > 0)
            {
                var triggerMenu = evt.menu;
                triggerMenu.AppendAction(
                    "Set Trigger Type/None",
                    (action) => SetTriggerType(null),
                    DropdownMenuAction.AlwaysEnabled
                );
                triggerMenu.AppendSeparator("Set Trigger Type/");

                foreach (var triggerType in triggerTypes)
                {
                    var displayName = TriggerTypeHelper.GetTriggerDisplayName(triggerType);
                    var type = triggerType; // Capture for closure
                    triggerMenu.AppendAction(
                        $"Set Trigger Type/{displayName}",
                        (action) => SetTriggerType(type),
                        DropdownMenuAction.AlwaysEnabled
                    );
                }
            }

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
            var newNodeData = new TriggerNode(newPosition);

            // Copy trigger
            if (!string.IsNullOrEmpty(NodeData.TriggerTypeName) && NodeData.Arguments != null)
            {
                // Deep copy arguments
                var argsType = NodeData.Arguments.GetType();
                var newArgs = Activator.CreateInstance(argsType) as DialogueTriggerArguments;
                if (newArgs != null)
                {
                    // Copy all fields
                    var fields = argsType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(NodeData.Arguments);
                        field.SetValue(newArgs, value);
                    }
                    newNodeData.SetTrigger(NodeData.TriggerTypeName, newArgs);
                }
            }

            // Copy data connections (but with new GUIDs - connections will need to be reconnected manually)
            // Note: We don't copy ArgumentDataConnections as they reference other nodes by GUID
            // The user will need to reconnect data inputs manually after duplication

            // Add to graph using the editor method (updates SerializedObject)
            _editor.AddTriggerNodeToGraph(newNodeData);

            var newNodeView = new TriggerNodeView(newNodeData, _editor);
            graphView.AddElement(newNodeView);

            _editor.SetDirty();
            Debug.Log("Trigger node duplicated.");
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

            _editor.CurrentGraph.TriggerNodes.Remove(NodeData);
            graphView.RemoveElement(this);

            _editor.SetDirty();
            Debug.Log("Trigger node deleted.");
        }
    }
}

