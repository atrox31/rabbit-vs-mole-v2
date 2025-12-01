using DialogueSystem.Nodes;
using DialogueSystem;
using DialogueSystem.TriggerData;
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
    /// Visual representation of a TriggerDataNode in the graph editor.
    /// </summary>
    public class TriggerDataNodeView : Node
    {
        public TriggerDataNode NodeData;
        public Dictionary<string, Port> OutputPorts = new Dictionary<string, Port>();

        private Label _dataTypeLabel;
        private VisualElement _dataContainer;
        private DialogueGraphEditor _editor;
        private SerializedProperty _nodeProperty;
        private SerializedProperty _dataTypeNameProperty;
        private SerializedProperty _dataProperty;
        private bool _isLoading = true;
        private bool _isRefreshing = false;
        private Dictionary<FieldInfo, VisualElement> _dataFields = new Dictionary<FieldInfo, VisualElement>();

        public TriggerDataNodeView(TriggerDataNode nodeData, DialogueGraphEditor editor)
        {
            NodeData = nodeData;
            _editor = editor;
            UpdateTitle();
            
            // Set node color to distinguish from other nodes
            titleContainer.style.backgroundColor = DialogueGraphColors.TriggerDataNodeColor;

            // Set the position using the data from the ScriptableObject
            SetPosition(new Rect(NodeData.EditorPosition, new Vector2(300, 150)));

            SetupCustomDataFields();
            RefreshOutputPorts();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            // Mark loading as complete after a frame
            schedule.Execute(() => { _isLoading = false; });
        }

        private void SetupCustomDataFields()
        {
            // Get SerializedProperty for this node
            _nodeProperty = _editor.GetTriggerDataNodeProperty(NodeData);
            if (_nodeProperty == null)
            {
                Debug.LogError("Failed to find SerializedProperty for TriggerDataNode.");
                return;
            }

            // Update SerializedProperty before reading values
            _nodeProperty.serializedObject.Update();

            _dataTypeNameProperty = _nodeProperty.FindProperty("_dataTypeName");
            _dataProperty = _nodeProperty.FindProperty("_data");

            // Create data type label
            _dataTypeLabel = new Label("Data Type: None");
            _dataTypeLabel.style.marginTop = 5;
            _dataTypeLabel.style.marginBottom = 5;
            extensionContainer.Add(_dataTypeLabel);

            // Create data container
            _dataContainer = new VisualElement();
            extensionContainer.Add(_dataContainer);

            // Load current data if any
            RefreshDataDisplay();

            RefreshPorts();
            RefreshExpandedState();
        }

        private void UpdateTitle()
        {
            if (_dataTypeNameProperty == null)
            {
                title = "Trigger Data";
                return;
            }

            string dataTypeName = _dataTypeNameProperty.stringValue;
            if (string.IsNullOrEmpty(dataTypeName))
            {
                title = "Trigger Data";
            }
            else
            {
                var triggerDataType = TriggerDataTypeHelper.GetTypeFromFullName(dataTypeName);
                if (triggerDataType != null)
                {
                    title = $"Data Type: {TriggerDataTypeHelper.GetDataDisplayName(triggerDataType)}";
                }
                else
                {
                    title = $"Data Type: {dataTypeName}";
                }
            }
        }

        private void RefreshDataDisplay()
        {
            UpdateTitle();
            
            _dataContainer.Clear();
            _dataFields.Clear();

            if (_dataTypeNameProperty == null || _dataProperty == null)
                return;

            _nodeProperty.serializedObject.Update();

            string dataTypeName = _dataTypeNameProperty.stringValue;
            
            if (string.IsNullOrEmpty(dataTypeName))
            {
                _dataTypeLabel.text = "Data Type: None (Right-click to select)";
                return;
            }

            var triggerDataType = TriggerDataTypeHelper.GetTypeFromFullName(dataTypeName);
            if (triggerDataType == null)
            {
                _dataTypeLabel.text = $"Data Type: Unknown ({dataTypeName})";
                return;
            }

            _dataTypeLabel.text = $"Data Type: {TriggerDataTypeHelper.GetDataDisplayName(triggerDataType)}";

            // Get data type and create fields
            var dataType = TriggerDataTypeHelper.GetDataType(triggerDataType);
            if (dataType == null)
            {
                _dataContainer.Add(new Label("No data type found for this trigger data."));
                return;
            }

            // Ensure data object exists
            if (_dataProperty.managedReferenceValue == null)
            {
                var newData = TriggerDataTypeHelper.CreateDataInstance(triggerDataType);
                _dataProperty.managedReferenceValue = newData;
                _nodeProperty.serializedObject.ApplyModifiedProperties();
            }

            // Create fields for all public fields in data type
            var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var fieldProperty = _dataProperty.FindPropertyRelative(field.Name);
                if (fieldProperty == null)
                    continue;

                VisualElement fieldElement = CreateFieldForProperty(fieldProperty, field);
                if (fieldElement != null)
                {
                    _dataContainer.Add(fieldElement);
                    _dataFields[field] = fieldElement;
                }
            }

            // Always refresh output ports when data type changes
            RefreshOutputPorts();
        }

        /// <summary>
        /// Refreshes the GameObjectName dropdown if this is a TD_GameObject node.
        /// Call this when GameObjectReferences list changes in DialogueSequence.
        /// </summary>
        public void RefreshGameObjectNameDropdown()
        {
            if (NodeData == null || NodeData.DataTypeName == null || 
                !NodeData.DataTypeName.Contains("TD_GameObject"))
                return;

            // Find the GameObjectName field and refresh it
            var dataType = TriggerDataTypeHelper.GetDataType(
                TriggerDataTypeHelper.GetTypeFromFullName(NodeData.DataTypeName));
            if (dataType == null) return;

            var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name == "GameObjectName" && _dataFields.ContainsKey(field))
                {
                    // Remove old field
                    var oldField = _dataFields[field];
                    _dataContainer.Remove(oldField);
                    _dataFields.Remove(field);

                    // Create new field with updated dropdown
                    var fieldProperty = _dataProperty.FindPropertyRelative(field.Name);
                    if (fieldProperty != null)
                    {
                        var newField = CreateFieldForProperty(fieldProperty, field);
                        if (newField != null)
                        {
                            _dataContainer.Add(newField);
                            _dataFields[field] = newField;
                        }
                    }
                    break;
                }
            }
        }

        private Type GetOutputPortTypeForGameManagerGet()
        {
            if (_dataProperty == null)
                return typeof(object);

            var dataValue = _dataProperty.managedReferenceValue;
            if (dataValue == null)
                return typeof(object);

            var dataType = dataValue.GetType();
            var field = dataType.GetField("DataType");
            if (field == null)
                return typeof(object);

            var dataTypeEnum = field.GetValue(dataValue);
            if (dataTypeEnum == null)
                return typeof(object);

            switch (dataTypeEnum.ToString())
            {
                case "String": return typeof(string);
                case "Int": return typeof(int);
                case "Float": return typeof(float);
                case "Bool": return typeof(bool);
                default: return typeof(object);
            }
        }

        private void RefreshOutputPorts()
        {
            // Prevent refresh during user interaction or if already refreshing
            if (_isRefreshing)
                return;

            _isRefreshing = true;

            try
            {
                // Get graph view to check for active connections
                var graphView = GetFirstAncestorOfType<DialogueGraphView>();
                bool hasActiveConnections = false;
                Dictionary<string, Edge> portConnections = new Dictionary<string, Edge>();

                if (graphView != null)
                {
                    // Check if any ports have active connections
                    foreach (var port in OutputPorts.Values)
                    {
                        var edges = graphView.edges.Where(e => e.output == port).ToList();
                        if (edges.Count > 0)
                        {
                            hasActiveConnections = true;
                            portConnections[port.portName] = edges[0];
                        }
                    }
                }

                // Remove old output ports (but preserve connections if they exist)
                foreach (var port in OutputPorts.Values)
                {
                    outputContainer.Remove(port);
                }
                OutputPorts.Clear();

                if (_dataTypeNameProperty == null)
                {
                    _isRefreshing = false;
                    return;
                }

                string dataTypeName = _dataTypeNameProperty.stringValue;
                if (string.IsNullOrEmpty(dataTypeName))
                {
                    _isRefreshing = false;
                    return;
                }

                var triggerDataType = TriggerDataTypeHelper.GetTypeFromFullName(dataTypeName);
                if (triggerDataType == null)
                {
                    _isRefreshing = false;
                    return;
                }

                var portNames = TriggerDataTypeHelper.GetOutputPortNames(triggerDataType);
                if (portNames == null || portNames.Length == 0)
                {
                    Debug.LogWarning($"TriggerDataNodeView: No output ports found for {dataTypeName}");
                    _isRefreshing = false;
                    return;
                }

                foreach (var portName in portNames)
                {
                    Type portType = null;
                    
                    // For TD_GameManagerGet, always get type from actual data
                    if (dataTypeName.Contains("TD_GameManagerGet"))
                    {
                        if (_dataProperty != null)
                        {
                            portType = GetOutputPortTypeForGameManagerGet();
                        }
                        else
                        {
                            portType = typeof(object); // Default if no data yet
                        }
                    }
                    else
                    {
                        // For other types, try helper first
                        portType = TriggerDataTypeHelper.GetOutputPortType(triggerDataType, portName);
                        
                        // If helper returns null, try to get from actual data instance
                        if (portType == null && _dataProperty != null)
                        {
                            try
                            {
                                var dataValue = _dataProperty.managedReferenceValue;
                                if (dataValue != null && typeof(ITriggerData).IsAssignableFrom(triggerDataType))
                                {
                                    // Create instance of ITriggerData and set its data
                                    var triggerData = Activator.CreateInstance(triggerDataType) as ITriggerData;
                                    if (triggerData != null)
                                    {
                                        triggerData.Data = dataValue;
                                        portType = triggerData.GetOutputPortType(portName);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"TriggerDataNodeView: Failed to get port type from data instance: {ex.Message}");
                            }
                        }
                    }

                    // Skip if still no type found
                    if (portType == null)
                    {
                        Debug.LogWarning($"TriggerDataNodeView: Could not determine port type for {portName} in {dataTypeName}");
                        continue;
                    }

                    var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, portType);
                    port.portName = portName;
                    outputContainer.Add(port);
                    OutputPorts[portName] = port;

                    // Restore connection if it existed and types are compatible
                    if (hasActiveConnections && portConnections.TryGetValue(portName, out var oldEdge))
                    {
                        if (oldEdge.input != null && oldEdge.input.portType == portType)
                        {
                            var newEdge = port.ConnectTo(oldEdge.input);
                            if (graphView != null)
                            {
                                graphView.AddElement(newEdge);
                                graphView.RemoveElement(oldEdge);
                            }
                        }
                    }
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private VisualElement CreateFieldForProperty(SerializedProperty property, FieldInfo fieldInfo)
        {
            VisualElement fieldElement = null;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                var objectField = new ObjectField(fieldInfo.Name)
                {
                    objectType = fieldInfo.FieldType,
                    allowSceneObjects = true,
                    bindingPath = property.propertyPath
                };
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
                // For TD_GameObject GameObjectName field, use DropdownField with list from DialogueSequence
                if (NodeData != null && NodeData.DataTypeName != null && 
                    NodeData.DataTypeName.Contains("TD_GameObject") && 
                    fieldInfo.Name == "GameObjectName")
                {
                    // Get list of GameObject names from DialogueSequence
                    var sequence = _editor.CurrentGraph;
                    List<string> gameObjectNames = new List<string>();
                    if (sequence != null && sequence.GameObjectReferences != null)
                    {
                        foreach (var reference in sequence.GameObjectReferences)
                        {
                            if (reference != null && !string.IsNullOrEmpty(reference.Name))
                            {
                                gameObjectNames.Add(reference.Name);
                            }
                        }
                    }
                    
                    // Add empty option
                    if (!gameObjectNames.Contains(""))
                    {
                        gameObjectNames.Insert(0, "");
                    }

                    var dropdownField = new DropdownField(fieldInfo.Name, gameObjectNames, 0)
                    {
                        bindingPath = property.propertyPath
                    };
                    
                    // Set current value if it exists
                    string currentValue = property.stringValue;
                    if (!string.IsNullOrEmpty(currentValue) && gameObjectNames.Contains(currentValue))
                    {
                        dropdownField.index = gameObjectNames.IndexOf(currentValue);
                    }
                    else
                    {
                        dropdownField.index = 0; // Empty
                    }
                    
                    dropdownField.RegisterValueChangedCallback(evt =>
                    {
                        property.stringValue = evt.newValue;
                        _nodeProperty.serializedObject.ApplyModifiedProperties();
                        _editor.SetDirty();
                    });
                    
                    fieldElement = dropdownField;
                }
                else
                {
                    var textField = new TextField(fieldInfo.Name)
                    {
                        bindingPath = property.propertyPath
                    };
                    textField.BindProperty(property);
                    // Use RegisterCallback with TrickleDown to avoid interrupting typing
                    textField.RegisterCallback<BlurEvent>(evt =>
                    {
                        _nodeProperty.serializedObject.ApplyModifiedProperties();
                        _editor.SetDirty();
                    });
                    // Also update on value change but don't refresh UI
                    textField.RegisterValueChangedCallback(evt => 
                    {
                        // Only mark as dirty, don't refresh UI during typing
                        _editor.SetDirty();
                    });
                    fieldElement = textField;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Enum)
            {
                var enumField = new EnumField(fieldInfo.Name)
                {
                    bindingPath = property.propertyPath
                };
                enumField.BindProperty(property);
                enumField.RegisterValueChangedCallback(evt => 
                {
                    _nodeProperty.serializedObject.ApplyModifiedProperties();
                    _editor.SetDirty();
                    
                    // Schedule refresh to avoid interrupting user interaction
                    schedule.Execute(() =>
                    {
                        // If port type changed, disconnect incompatible connections first
                        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
                        if (graphView != null)
                        {
                            var edgesToRemove = new List<Edge>();
                            foreach (var edge in graphView.edges.ToList())
                            {
                                if (edge.output != null && edge.output.node == this)
                                {
                                    // Check if the port type is still compatible
                                    if (edge.input != null && edge.input.node is TriggerNodeView)
                                    {
                                        // Get the new port type that will be created
                                        var newPortType = GetOutputPortTypeForGameManagerGet();
                                        if (newPortType != null && edge.output.portType != newPortType)
                                        {
                                            edgesToRemove.Add(edge);
                                        }
                                    }
                                }
                            }
                            foreach (var edge in edgesToRemove)
                            {
                                graphView.RemoveElement(edge);
                            }
                            if (edgesToRemove.Count > 0)
                            {
                                _editor.SetDirty();
                            }
                        }
                        
                        // Refresh output ports after disconnecting incompatible connections
                        RefreshOutputPorts();
                        UpdateTitle(); // Update title without full refresh
                    });
                });
                fieldElement = enumField;
            }

            return fieldElement;
        }

        private void SetDataType(Type triggerDataType)
        {
            if (_dataTypeNameProperty == null || _dataProperty == null)
                return;

            _nodeProperty.serializedObject.Update();

            if (triggerDataType == null)
            {
                _dataTypeNameProperty.stringValue = string.Empty;
                _dataProperty.managedReferenceValue = null;
            }
            else
            {
                _dataTypeNameProperty.stringValue = TriggerDataTypeHelper.GetFullTypeName(triggerDataType);
                var data = TriggerDataTypeHelper.CreateDataInstance(triggerDataType);
                _dataProperty.managedReferenceValue = data;
            }

            _nodeProperty.serializedObject.ApplyModifiedProperties();
            RefreshDataDisplay();
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

            // Add data type selection submenu
            var dataTypes = TriggerDataTypeHelper.GetDataTypes();
            if (dataTypes.Count > 0)
            {
                var dataMenu = evt.menu;
                dataMenu.AppendAction(
                    "Set Data Type/None",
                    (action) => SetDataType(null),
                    DropdownMenuAction.AlwaysEnabled
                );
                dataMenu.AppendSeparator("Set Data Type/");

                foreach (var dataType in dataTypes)
                {
                    var displayName = TriggerDataTypeHelper.GetDataDisplayName(dataType);
                    var type = dataType; // Capture for closure
                    dataMenu.AppendAction(
                        $"Set Data Type/{displayName}",
                        (action) => SetDataType(type),
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
            var newNodeData = new TriggerDataNode(newPosition);

            // Copy data
            if (!string.IsNullOrEmpty(NodeData.DataTypeName) && NodeData.Data != null)
            {
                // Deep copy data
                var dataType = NodeData.Data.GetType();
                var newData = Activator.CreateInstance(dataType);
                if (newData != null)
                {
                    // Copy all fields
                    var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(NodeData.Data);
                        field.SetValue(newData, value);
                    }
                    newNodeData.SetData(NodeData.DataTypeName, newData);
                }
            }

            // Add to graph using the editor method (updates SerializedObject)
            _editor.AddTriggerDataNodeToGraph(newNodeData);

            var newNodeView = new TriggerDataNodeView(newNodeData, _editor);
            graphView.AddElement(newNodeView);

            _editor.SetDirty();
            Debug.Log("Trigger data node duplicated.");
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

            _editor.CurrentGraph.TriggerDataNodes.Remove(NodeData);
            graphView.RemoveElement(this);

            _editor.SetDirty();
            Debug.Log("Trigger data node deleted.");
        }
    }
}

