using DialogueSystem.Nodes;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    public class DialogueGraphEditor : EditorWindow
    {
        private DialogueSequence _currentGraph;
        public DialogueSequence CurrentGraph => _currentGraph;
        private DialogueGraphView _graphView; // Now store the graph view instance
        private bool _isDirty = false;
        private bool _isLoading = false; // Flag to prevent SetDirty during loading
        private SerializedObject _serializedObject;
        public SerializedObject CurrentGraphSerializedObject => _serializedObject;


        public new void SetDirty()
        {
            // Don't set dirty flag during loading
            if (_isLoading) return;
            
            if (_isDirty) return;

            _isDirty = true;
            UpdateTitle();

            if (_currentGraph != null)
            {
                EditorUtility.SetDirty(_currentGraph);
            }
        }
        
        public bool IsLoading => _isLoading;

        [MenuItem("Tools/Dialogue System/Open Editor Window")]
        public static void Open()
        {
            GetWindow<DialogueGraphEditor>().Show();
        }

        private void UpdateTitle()
        {
            string title = "Dialogue Graph: ";
            if (_currentGraph != null)
            {
                title += _currentGraph.name;
                if (_isDirty)
                {
                    title += "*";
                }
            }
            else
            {
                title += " (No Graph Loaded)";
            }

            titleContent = new GUIContent(title);
        }
        public SerializedProperty GetNodeProperty(DialogueNode nodeData)
        {
            if (_currentGraph == null || _serializedObject == null) return null;

            var nodesProperty = _serializedObject.FindProperty("Nodes");

            if (nodesProperty == null)
            {
                Debug.LogError($"Property 'Nodes' not found in DialogueSequence. Check the field name.");
                return null;
            }

            if (!nodesProperty.isArray)
            {
                Debug.LogError($"Property 'Nodes' is not an array/list. Check the field type.");
                return null;
            }

            // Krok 2: Wyszukujemy w�a�ciwo��.
            for (int i = 0; i < nodesProperty.arraySize; i++)
            {
                var element = nodesProperty.GetArrayElementAtIndex(i);
                if (element == null) continue;

                SerializedProperty guidProperty = element.FindProperty("GUID");
                if (guidProperty != null && guidProperty.stringValue == nodeData.GUID)
                {
                    return element;
                }
            }
            return null;
        }

        public SerializedProperty GetTriggerNodeProperty(TriggerNode nodeData)
        {
            if (_currentGraph == null || _serializedObject == null) return null;

            var triggerNodesProperty = _serializedObject.FindProperty("TriggerNodes");

            if (triggerNodesProperty == null)
            {
                Debug.LogError($"Property 'TriggerNodes' not found in DialogueSequence. Check the field name.");
                return null;
            }

            if (!triggerNodesProperty.isArray)
            {
                Debug.LogError($"Property 'TriggerNodes' is not an array/list. Check the field type.");
                return null;
            }

            for (int i = 0; i < triggerNodesProperty.arraySize; i++)
            {
                var element = triggerNodesProperty.GetArrayElementAtIndex(i);
                if (element == null) continue;

                SerializedProperty guidProperty = element.FindProperty("GUID");
                if (guidProperty != null && guidProperty.stringValue == nodeData.GUID)
                {
                    return element;
                }
            }
            return null;
        }

        public SerializedProperty GetStartNodeProperty()
        {
            if (_currentGraph == null || _serializedObject == null) return null;

            var startNodeProperty = _serializedObject.FindProperty("StartNode");
            return startNodeProperty;
        }

        public SerializedProperty GetTriggerDataNodeProperty(TriggerDataNode nodeData)
        {
            if (_currentGraph == null || _serializedObject == null) return null;

            var triggerDataNodesProperty = _serializedObject.FindProperty("TriggerDataNodes");

            if (triggerDataNodesProperty == null)
            {
                Debug.LogError($"Property 'TriggerDataNodes' not found in DialogueSequence. Check the field name.");
                return null;
            }

            if (!triggerDataNodesProperty.isArray)
            {
                Debug.LogError($"Property 'TriggerDataNodes' is not an array/list. Check the field type.");
                return null;
            }

            for (int i = 0; i < triggerDataNodesProperty.arraySize; i++)
            {
                var element = triggerDataNodesProperty.GetArrayElementAtIndex(i);
                if (element == null) continue;

                SerializedProperty guidProperty = element.FindProperty("GUID");
                if (guidProperty != null && guidProperty.stringValue == nodeData.GUID)
                {
                    return element;
                }
            }
            return null;
        }

        public SerializedProperty GetLogicNodeProperty(LogicNode nodeData)
        {
            if (_currentGraph == null || _serializedObject == null) return null;

            var logicNodesProperty = _serializedObject.FindProperty("LogicNodes");

            if (logicNodesProperty == null)
            {
                Debug.LogError($"Property 'LogicNodes' not found in DialogueSequence. Check the field name.");
                return null;
            }

            if (!logicNodesProperty.isArray)
            {
                Debug.LogError($"Property 'LogicNodes' is not an array/list. Check the field type.");
                return null;
            }

            for (int i = 0; i < logicNodesProperty.arraySize; i++)
            {
                var element = logicNodesProperty.GetArrayElementAtIndex(i);
                if (element == null) continue;

                SerializedProperty guidProperty = element.FindProperty("GUID");
                if (guidProperty != null && guidProperty.stringValue == nodeData.GUID)
                {
                    return element;
                }
            }
            return null;
        }

        public void CreateGUI()
        {
            // 1. Setup GraphView
            _graphView = new DialogueGraphView(this);
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);

            // 2. Setup Toolbar
            var toolbar = new Toolbar();

            // Save Button
            Button saveButton = new Button(SaveGraph) { text = "Save Graph" };
            toolbar.Add(saveButton);

            rootVisualElement.Add(toolbar);

            // Load the last selected graph if the window was previously open
            if (_currentGraph != null)
            {
                LoadGraph(_currentGraph);
            }
        }

        public void SetTarget(DialogueSequence graph)
        {
            // If the graph is the same, do nothing
            if (_currentGraph == graph) return;

            _currentGraph = graph;
            titleContent = new GUIContent("Dialogue Graph: " + graph.name);

            if (_currentGraph != null)
            {
                _serializedObject = new SerializedObject(_currentGraph);
            }
            else
            {
                _serializedObject = null;
            }

            // If the GUI is already built, load the graph immediately
            if (_graphView != null)
            {
                LoadGraph(graph);
            }
        }

        private void LoadGraph(DialogueSequence graph)
        {
            _isLoading = true; // Set loading flag to prevent SetDirty calls
            
            // Clear the existing elements from the view
            _graphView.ClearGraph();

            // Dictionary for fast access to node views by GUID (both types)
            var nodeViewMap = new Dictionary<string, Node>();

            // Ensure StartNode exists
            if (graph.StartNode == null)
            {
                graph.StartNode = new StartNode();
            }

            // Load StartNode
            var startNodeView = new StartNodeView(graph.StartNode, this);
            _graphView.AddElement(startNodeView);
            nodeViewMap.Add(graph.StartNode.GUID, startNodeView);

            // Load DialogueNodes
            foreach (var nodeData in graph.Nodes)
            {
                var nodeView = new DialogueNodeView(nodeData, this);
                _graphView.AddElement(nodeView);
                nodeViewMap.Add(nodeData.GUID, nodeView);
            }

            // Load TriggerNodes
            foreach (var triggerNodeData in graph.TriggerNodes)
            {
                var triggerNodeView = new TriggerNodeView(triggerNodeData, this);
                _graphView.AddElement(triggerNodeView);
                nodeViewMap.Add(triggerNodeData.GUID, triggerNodeView);
            }

            // Load TriggerDataNodes
            foreach (var triggerDataNodeData in graph.TriggerDataNodes)
            {
                var triggerDataNodeView = new TriggerDataNodeView(triggerDataNodeData, this);
                _graphView.AddElement(triggerDataNodeView);
                nodeViewMap.Add(triggerDataNodeData.GUID, triggerDataNodeView);
            }

            // Load LogicNodes
            foreach (var logicNodeData in graph.LogicNodes)
            {
                var logicNodeView = new LogicNodeView(logicNodeData, this);
                _graphView.AddElement(logicNodeView);
                nodeViewMap.Add(logicNodeData.GUID, logicNodeView);
            }

            // Connect StartNode
            if (graph.StartNode != null && graph.StartNode.ExitPorts != null)
            {
                foreach (var link in graph.StartNode.ExitPorts)
                {
                    if (nodeViewMap.TryGetValue(link.TargetNodeGUID, out var targetNode))
                    {
                        Port inputPort = null;
                        if (targetNode is DialogueNodeView dialogueNodeView)
                        {
                            inputPort = dialogueNodeView.InputPort;
                        }
                        else if (targetNode is TriggerNodeView triggerNodeView)
                        {
                            inputPort = triggerNodeView.InputPort;
                        }

                        if (inputPort != null)
                        {
                            Edge edge = startNodeView.OutputPort.ConnectTo(inputPort);
                            _graphView.AddElement(edge);
                        }
                    }
                }
            }

            // Connect DialogueNodes
            foreach (var nodeData in graph.Nodes)
            {
                if (!nodeViewMap.TryGetValue(nodeData.GUID, out var sourceNode)) continue;

                DialogueNodeView sourceNodeView = sourceNode as DialogueNodeView;
                if (sourceNodeView == null) continue;

                foreach (var link in nodeData.ExitPorts)
                {
                    if (nodeViewMap.TryGetValue(link.TargetNodeGUID, out var targetNode))
                    {
                        Port inputPort = null;
                        if (targetNode is DialogueNodeView dialogueNodeView)
                        {
                            inputPort = dialogueNodeView.InputPort;
                        }
                        else if (targetNode is TriggerNodeView triggerNodeView)
                        {
                            inputPort = triggerNodeView.InputPort;
                        }
                        else if (targetNode is LogicNodeView logicNodeView)
                        {
                            inputPort = logicNodeView.InputPort;
                        }

                        if (inputPort != null)
                        {
                            Edge edge = sourceNodeView.OutputPort.ConnectTo(inputPort);
                            _graphView.AddElement(edge);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Target node with GUID {link.TargetNodeGUID} not found during graph loading.");
                    }
                }
            }

            // Connect TriggerNodes
            foreach (var triggerNodeData in graph.TriggerNodes)
            {
                if (!nodeViewMap.TryGetValue(triggerNodeData.GUID, out var sourceNode)) continue;

                TriggerNodeView sourceNodeView = sourceNode as TriggerNodeView;
                if (sourceNodeView == null) continue;

                foreach (var link in triggerNodeData.ExitPorts)
                {
                    if (nodeViewMap.TryGetValue(link.TargetNodeGUID, out var targetNode))
                    {
                        Port inputPort = null;
                        if (targetNode is DialogueNodeView dialogueNodeView)
                        {
                            inputPort = dialogueNodeView.InputPort;
                        }
                        else if (targetNode is TriggerNodeView triggerNodeView)
                        {
                            inputPort = triggerNodeView.InputPort;
                        }
                        else if (targetNode is LogicNodeView logicNodeView)
                        {
                            inputPort = logicNodeView.InputPort;
                        }

                        if (inputPort != null)
                        {
                            Edge edge = sourceNodeView.OutputPort.ConnectTo(inputPort);
                            _graphView.AddElement(edge);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Target node with GUID {link.TargetNodeGUID} not found during graph loading.");
                    }
                }
            }

            // Connect LogicNodes
            foreach (var logicNodeData in graph.LogicNodes)
            {
                if (!nodeViewMap.TryGetValue(logicNodeData.GUID, out var sourceNode)) continue;

                LogicNodeView sourceNodeView = sourceNode as LogicNodeView;
                if (sourceNodeView == null) continue;

                // Connect True output
                if (logicNodeData.ExitPortsTrue != null)
                {
                    foreach (var link in logicNodeData.ExitPortsTrue)
                    {
                        if (nodeViewMap.TryGetValue(link.TargetNodeGUID, out var targetNode))
                        {
                            Port inputPort = null;
                            if (targetNode is DialogueNodeView dialogueNodeView)
                            {
                                inputPort = dialogueNodeView.InputPort;
                            }
                            else if (targetNode is TriggerNodeView triggerNodeView)
                            {
                                inputPort = triggerNodeView.InputPort;
                            }
                            else if (targetNode is LogicNodeView logicNodeView)
                            {
                                inputPort = logicNodeView.InputPort;
                            }

                            if (inputPort != null)
                            {
                                Edge edge = sourceNodeView.OutputPortTrue.ConnectTo(inputPort);
                                _graphView.AddElement(edge);
                            }
                        }
                    }
                }

                // Connect False output
                if (logicNodeData.ExitPortsFalse != null)
                {
                    foreach (var link in logicNodeData.ExitPortsFalse)
                    {
                        if (nodeViewMap.TryGetValue(link.TargetNodeGUID, out var targetNode))
                        {
                            Port inputPort = null;
                            if (targetNode is DialogueNodeView dialogueNodeView)
                            {
                                inputPort = dialogueNodeView.InputPort;
                            }
                            else if (targetNode is TriggerNodeView triggerNodeView)
                            {
                                inputPort = triggerNodeView.InputPort;
                            }
                            else if (targetNode is LogicNodeView logicNodeView)
                            {
                                inputPort = logicNodeView.InputPort;
                            }

                            if (inputPort != null)
                            {
                                Edge edge = sourceNodeView.OutputPortFalse.ConnectTo(inputPort);
                                _graphView.AddElement(edge);
                            }
                        }
                    }
                }
            }

            // Connect TriggerDataNodes to TriggerNodes (data connections)
            foreach (var triggerNodeData in graph.TriggerNodes)
            {
                if (!nodeViewMap.TryGetValue(triggerNodeData.GUID, out var targetNode)) continue;

                TriggerNodeView targetNodeView = targetNode as TriggerNodeView;
                if (targetNodeView == null) continue;

                if (triggerNodeData.ArgumentDataConnections != null)
                {
                    foreach (var connection in triggerNodeData.ArgumentDataConnections)
                    {
                        if (!nodeViewMap.TryGetValue(connection.SourceDataNodeGUID, out var sourceNode)) continue;

                        TriggerDataNodeView sourceNodeView = sourceNode as TriggerDataNodeView;
                        if (sourceNodeView == null) continue;

                        // Find the output port on source node
                        if (sourceNodeView.OutputPorts.TryGetValue(connection.SourcePortName, out var outputPort))
                        {
                            // Find the input port on target node
                            if (targetNodeView.ArgumentInputPorts.TryGetValue(connection.TargetFieldName, out var inputPort))
                            {
                                Edge edge = outputPort.ConnectTo(inputPort);
                                _graphView.AddElement(edge);
                            }
                        }
                    }
                }
            }

            // Connect TriggerDataNodes to LogicNodes (Condition input connections)
            foreach (var logicNodeData in graph.LogicNodes)
            {
                if (logicNodeData.ConditionDataConnection != null)
                {
                    if (!nodeViewMap.TryGetValue(logicNodeData.GUID, out var targetNode)) continue;

                    LogicNodeView targetNodeView = targetNode as LogicNodeView;
                    if (targetNodeView == null) continue;

                    if (!nodeViewMap.TryGetValue(logicNodeData.ConditionDataConnection.SourceDataNodeGUID, out var sourceNode)) continue;

                    TriggerDataNodeView sourceNodeView = sourceNode as TriggerDataNodeView;
                    if (sourceNodeView == null) continue;

                    // Find the output port on source node
                    if (sourceNodeView.OutputPorts.TryGetValue(logicNodeData.ConditionDataConnection.SourcePortName, out var outputPort))
                    {
                        // Connect to Condition input port
                        Edge edge = outputPort.ConnectTo(targetNodeView.ConditionInputPort);
                        _graphView.AddElement(edge);
                    }
                }
            }
            
            // Keep loading flag true for a bit longer to catch late callbacks
            // Reset dirty flag immediately since we just loaded
            _isDirty = false;
            
            // Clear loading flag after a delay to allow all async callbacks to complete
            EditorApplication.delayCall += () =>
            {
                _isLoading = false;
                // Reset dirty flag again in case any late callbacks set it
                if (_isDirty)
                {
                    _isDirty = false;
                    UpdateTitle();
                }
            };
        }

        private void SaveGraph()
        {
            if (_currentGraph == null || _serializedObject == null) return;
            
            // Update SerializedObject to get latest changes
            _serializedObject.Update();
            
            // Get all node views currently in the graph
            var currentNodeViews = _graphView.nodes.ToList()
                .OfType<DialogueNodeView>()
                .ToList();
            var currentNodeGuids = new HashSet<string>(
                currentNodeViews.Select(nv => nv.NodeData.GUID));
            
            // Sync the Nodes list in SerializedProperty with the actual nodes in the view
            var nodesProperty = _serializedObject.FindProperty("Nodes");
            if (nodesProperty != null && nodesProperty.isArray)
            {
                // First, remove nodes that are no longer in the view
                for (int i = nodesProperty.arraySize - 1; i >= 0; i--)
                {
                    var nodeProperty = nodesProperty.GetArrayElementAtIndex(i);
                    if (nodeProperty == null) continue;
                    
                    SerializedProperty guidProperty = nodeProperty.FindProperty("GUID");
                    if (guidProperty != null && !currentNodeGuids.Contains(guidProperty.stringValue))
                    {
                        // This node was deleted, remove it from the array
                        nodesProperty.DeleteArrayElementAtIndex(i);
                    }
                }
                
                // Now update existing nodes and add new ones
                foreach (var nodeView in currentNodeViews)
                {
                    // Try to find existing node property
                    SerializedProperty nodeProperty = null;
                    int existingIndex = -1;
                    
                    for (int i = 0; i < nodesProperty.arraySize; i++)
                    {
                        var element = nodesProperty.GetArrayElementAtIndex(i);
                        if (element == null) continue;
                        
                        SerializedProperty guidProperty = element.FindProperty("GUID");
                        if (guidProperty != null && guidProperty.stringValue == nodeView.NodeData.GUID)
                        {
                            nodeProperty = element;
                            existingIndex = i;
                            break;
                        }
                    }
                    
                    // If node doesn't exist in SerializedProperty, add it
                    if (nodeProperty == null)
                    {
                        nodesProperty.arraySize++;
                        nodeProperty = nodesProperty.GetArrayElementAtIndex(nodesProperty.arraySize - 1);
                        // Set GUID for new node
                        SerializedProperty newGuidProperty = nodeProperty.FindProperty("GUID");
                        if (newGuidProperty != null)
                        {
                            newGuidProperty.stringValue = nodeView.NodeData.GUID;
                        }
                    }
                    
                    if (nodeProperty != null)
                    {
                        // Sync position
                        SerializedProperty positionProperty = nodeProperty.FindProperty("EditorPosition");
                        if (positionProperty != null)
                        {
                            positionProperty.vector2Value = nodeView.NodeData.EditorPosition;
                        }
                        
                        // Sync other properties
                        SerializedProperty actorProperty = nodeProperty.FindProperty("_actor");
                        if (actorProperty != null)
                        {
                            actorProperty.objectReferenceValue = nodeView.NodeData._actor;
                        }
                        
                        SerializedProperty poseProperty = nodeProperty.FindProperty("_poseName");
                        if (poseProperty != null)
                        {
                            poseProperty.stringValue = nodeView.NodeData._poseName;
                        }
                        
                        SerializedProperty screenPositionProperty = nodeProperty.FindProperty("ScreenPosition");
                        if (screenPositionProperty != null)
                        {
                            screenPositionProperty.enumValueIndex = (int)nodeView.NodeData.ScreenPosition;
                        }
                        
                        SerializedProperty textProperty = nodeProperty.FindProperty("text");
                        if (textProperty != null)
                        {
                            textProperty.stringValue = nodeView.NodeData.text;
                        }
                    }
                }
            }
            
            // Apply all property changes
            _serializedObject.ApplyModifiedProperties();
            
            // Get all trigger node views currently in the graph
            var currentTriggerNodeViews = _graphView.nodes.ToList()
                .OfType<TriggerNodeView>()
                .ToList();
            var currentTriggerNodeGuids = new HashSet<string>(
                currentTriggerNodeViews.Select(nv => nv.NodeData.GUID));

            // Sync the TriggerNodes list in SerializedProperty
            var triggerNodesProperty = _serializedObject.FindProperty("TriggerNodes");
            if (triggerNodesProperty != null && triggerNodesProperty.isArray)
            {
                // Remove trigger nodes that are no longer in the view
                for (int i = triggerNodesProperty.arraySize - 1; i >= 0; i--)
                {
                    var nodeProperty = triggerNodesProperty.GetArrayElementAtIndex(i);
                    if (nodeProperty == null) continue;

                    SerializedProperty guidProperty = nodeProperty.FindProperty("GUID");
                    if (guidProperty != null && !currentTriggerNodeGuids.Contains(guidProperty.stringValue))
                    {
                        triggerNodesProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                // Update existing trigger nodes and add new ones
                foreach (var triggerNodeView in currentTriggerNodeViews)
                {
                    SerializedProperty nodeProperty = null;

                    for (int i = 0; i < triggerNodesProperty.arraySize; i++)
                    {
                        var element = triggerNodesProperty.GetArrayElementAtIndex(i);
                        if (element == null) continue;

                        SerializedProperty guidProperty = element.FindProperty("GUID");
                        if (guidProperty != null && guidProperty.stringValue == triggerNodeView.NodeData.GUID)
                        {
                            nodeProperty = element;
                            break;
                        }
                    }

                    if (nodeProperty == null)
                    {
                        triggerNodesProperty.arraySize++;
                        nodeProperty = triggerNodesProperty.GetArrayElementAtIndex(triggerNodesProperty.arraySize - 1);
                        SerializedProperty newGuidProperty = nodeProperty.FindProperty("GUID");
                        if (newGuidProperty != null)
                        {
                            newGuidProperty.stringValue = triggerNodeView.NodeData.GUID;
                        }
                    }

                    if (nodeProperty != null)
                    {
                        SerializedProperty positionProperty = nodeProperty.FindProperty("EditorPosition");
                        if (positionProperty != null)
                        {
                            positionProperty.vector2Value = triggerNodeView.NodeData.EditorPosition;
                        }
                    }
                }
            }

            // Apply all property changes
            _serializedObject.ApplyModifiedProperties();

            // Update the actual Nodes list in the ScriptableObject to match SerializedProperty
            _currentGraph.Nodes.Clear();
            _currentGraph.Nodes.AddRange(currentNodeViews.Select(nv => nv.NodeData));

            // Update the actual TriggerNodes list
            _currentGraph.TriggerNodes.Clear();
            _currentGraph.TriggerNodes.AddRange(currentTriggerNodeViews.Select(nv => nv.NodeData));

            // Get all trigger data node views currently in the graph
            var currentTriggerDataNodeViews = _graphView.nodes.ToList()
                .OfType<TriggerDataNodeView>()
                .ToList();
            var currentTriggerDataNodeGuids = new HashSet<string>(
                currentTriggerDataNodeViews.Select(nv => nv.NodeData.GUID));

            // Sync the TriggerDataNodes list in SerializedProperty
            var triggerDataNodesProperty = _serializedObject.FindProperty("TriggerDataNodes");
            if (triggerDataNodesProperty != null && triggerDataNodesProperty.isArray)
            {
                // Remove trigger data nodes that are no longer in the view
                for (int i = triggerDataNodesProperty.arraySize - 1; i >= 0; i--)
                {
                    var nodeProperty = triggerDataNodesProperty.GetArrayElementAtIndex(i);
                    if (nodeProperty == null) continue;

                    SerializedProperty guidProperty = nodeProperty.FindProperty("GUID");
                    if (guidProperty != null && !currentTriggerDataNodeGuids.Contains(guidProperty.stringValue))
                    {
                        triggerDataNodesProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                // Update existing trigger data nodes and add new ones
                foreach (var triggerDataNodeView in currentTriggerDataNodeViews)
                {
                    SerializedProperty nodeProperty = null;

                    for (int i = 0; i < triggerDataNodesProperty.arraySize; i++)
                    {
                        var element = triggerDataNodesProperty.GetArrayElementAtIndex(i);
                        if (element == null) continue;

                        SerializedProperty guidProperty = element.FindProperty("GUID");
                        if (guidProperty != null && guidProperty.stringValue == triggerDataNodeView.NodeData.GUID)
                        {
                            nodeProperty = element;
                            break;
                        }
                    }

                    if (nodeProperty == null)
                    {
                        triggerDataNodesProperty.arraySize++;
                        nodeProperty = triggerDataNodesProperty.GetArrayElementAtIndex(triggerDataNodesProperty.arraySize - 1);
                        SerializedProperty newGuidProperty = nodeProperty.FindProperty("GUID");
                        if (newGuidProperty != null)
                        {
                            newGuidProperty.stringValue = triggerDataNodeView.NodeData.GUID;
                        }
                    }

                    if (nodeProperty != null)
                    {
                        SerializedProperty positionProperty = nodeProperty.FindProperty("EditorPosition");
                        if (positionProperty != null)
                        {
                            positionProperty.vector2Value = triggerDataNodeView.NodeData.EditorPosition;
                        }
                    }
                }
            }

            // Apply all property changes
            _serializedObject.ApplyModifiedProperties();

            // Update the actual TriggerDataNodes list
            _currentGraph.TriggerDataNodes.Clear();
            _currentGraph.TriggerDataNodes.AddRange(currentTriggerDataNodeViews.Select(nv => nv.NodeData));

            // Get all logic node views currently in the graph
            var currentLogicNodeViews = _graphView.nodes.ToList()
                .OfType<LogicNodeView>()
                .ToList();
            var currentLogicNodeGuids = new HashSet<string>(
                currentLogicNodeViews.Select(nv => nv.NodeData.GUID));

            // Sync the LogicNodes list in SerializedProperty
            var logicNodesProperty = _serializedObject.FindProperty("LogicNodes");
            if (logicNodesProperty != null && logicNodesProperty.isArray)
            {
                // Remove logic nodes that are no longer in the view
                for (int i = logicNodesProperty.arraySize - 1; i >= 0; i--)
                {
                    var nodeProperty = logicNodesProperty.GetArrayElementAtIndex(i);
                    if (nodeProperty == null) continue;

                    SerializedProperty guidProperty = nodeProperty.FindProperty("GUID");
                    if (guidProperty != null && !currentLogicNodeGuids.Contains(guidProperty.stringValue))
                    {
                        logicNodesProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                // Update existing logic nodes and add new ones
                foreach (var logicNodeView in currentLogicNodeViews)
                {
                    SerializedProperty nodeProperty = null;

                    for (int i = 0; i < logicNodesProperty.arraySize; i++)
                    {
                        var element = logicNodesProperty.GetArrayElementAtIndex(i);
                        if (element == null) continue;

                        SerializedProperty guidProperty = element.FindProperty("GUID");
                        if (guidProperty != null && guidProperty.stringValue == logicNodeView.NodeData.GUID)
                        {
                            nodeProperty = element;
                            break;
                        }
                    }

                    if (nodeProperty == null)
                    {
                        logicNodesProperty.arraySize++;
                        nodeProperty = logicNodesProperty.GetArrayElementAtIndex(logicNodesProperty.arraySize - 1);
                        SerializedProperty newGuidProperty = nodeProperty.FindProperty("GUID");
                        if (newGuidProperty != null)
                        {
                            newGuidProperty.stringValue = logicNodeView.NodeData.GUID;
                        }
                    }

                    if (nodeProperty != null)
                    {
                        SerializedProperty positionProperty = nodeProperty.FindProperty("EditorPosition");
                        if (positionProperty != null)
                        {
                            positionProperty.vector2Value = logicNodeView.NodeData.EditorPosition;
                        }

                        SerializedProperty conditionProperty = nodeProperty.FindProperty("Condition");
                        if (conditionProperty != null)
                        {
                            conditionProperty.boolValue = logicNodeView.NodeData.Condition;
                        }
                    }
                }
            }

            // Apply all property changes
            _serializedObject.ApplyModifiedProperties();

            // Update the actual LogicNodes list
            _currentGraph.LogicNodes.Clear();
            _currentGraph.LogicNodes.AddRange(currentLogicNodeViews.Select(nv => nv.NodeData));

            // Clear all existing connections in the ScriptableObject data 
            // to save only the current ones.
            if (_currentGraph.StartNode != null)
            {
                _currentGraph.StartNode.ExitPorts.Clear();
            }
            _currentGraph.Nodes.ForEach(node => node.ExitPorts.Clear());
            _currentGraph.TriggerNodes.ForEach(node => node.ExitPorts.Clear());
            _currentGraph.TriggerNodes.ForEach(node => node.ArgumentDataConnections.Clear());
            _currentGraph.LogicNodes.ForEach(node => node.ExitPortsTrue.Clear());
            _currentGraph.LogicNodes.ForEach(node => node.ExitPortsFalse.Clear());
            _currentGraph.LogicNodes.ForEach(node => node.ConditionDataConnection = null);

            // Save Edges (Connections)
            var edges = _graphView.edges.ToList(); 

            foreach (var edge in edges)
            {
                // Handle StartNode -> any node
                if (edge.output.node is StartNodeView sourceStartNodeView)
                {
                    string targetGUID = null;
                    if (edge.input.node is DialogueNodeView targetDialogueNodeView)
                    {
                        targetGUID = targetDialogueNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is TriggerNodeView targetTriggerNodeView)
                    {
                        targetGUID = targetTriggerNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is LogicNodeView targetLogicNodeView)
                    {
                        targetGUID = targetLogicNodeView.NodeData.GUID;
                    }

                    if (targetGUID != null)
                    {
                        var link = new NodeLink { TargetNodeGUID = targetGUID };
                        sourceStartNodeView.NodeData.ExitPorts.Add(link);
                    }
                }
                // Handle DialogueNode -> any node
                else if (edge.output.node is DialogueNodeView sourceDialogueNodeView)
                {
                    string targetGUID = null;
                    if (edge.input.node is DialogueNodeView targetDialogueNodeView)
                    {
                        targetGUID = targetDialogueNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is TriggerNodeView targetTriggerNodeView)
                    {
                        targetGUID = targetTriggerNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is LogicNodeView targetLogicNodeView)
                    {
                        targetGUID = targetLogicNodeView.NodeData.GUID;
                    }

                    if (targetGUID != null)
                    {
                        var link = new NodeLink { TargetNodeGUID = targetGUID };
                        sourceDialogueNodeView.NodeData.ExitPorts.Add(link);
                    }
                }
                // Handle TriggerNode -> any node
                else if (edge.output.node is TriggerNodeView sourceTriggerNodeView)
                {
                    string targetGUID = null;
                    if (edge.input.node is DialogueNodeView targetDialogueNodeView)
                    {
                        targetGUID = targetDialogueNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is TriggerNodeView targetTriggerNodeView)
                    {
                        targetGUID = targetTriggerNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is LogicNodeView targetLogicNodeView)
                    {
                        targetGUID = targetLogicNodeView.NodeData.GUID;
                    }

                    if (targetGUID != null)
                    {
                        var link = new NodeLink { TargetNodeGUID = targetGUID };
                        sourceTriggerNodeView.NodeData.ExitPorts.Add(link);
                    }
                }
                // Handle LogicNode -> any node (True and False outputs)
                else if (edge.output.node is LogicNodeView sourceLogicNodeView)
                {
                    string targetGUID = null;
                    if (edge.input.node is DialogueNodeView targetDialogueNodeView)
                    {
                        targetGUID = targetDialogueNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is TriggerNodeView targetTriggerNodeView)
                    {
                        targetGUID = targetTriggerNodeView.NodeData.GUID;
                    }
                    else if (edge.input.node is LogicNodeView targetLogicNodeView)
                    {
                        targetGUID = targetLogicNodeView.NodeData.GUID;
                    }

                    if (targetGUID != null)
                    {
                        var link = new NodeLink { TargetNodeGUID = targetGUID };
                        // Determine which output port (True or False)
                        if (edge.output == sourceLogicNodeView.OutputPortTrue)
                        {
                            sourceLogicNodeView.NodeData.ExitPortsTrue.Add(link);
                        }
                        else if (edge.output == sourceLogicNodeView.OutputPortFalse)
                        {
                            sourceLogicNodeView.NodeData.ExitPortsFalse.Add(link);
                        }
                    }
                }
                // Handle TriggerDataNode -> TriggerNode (data connections)
                else if (edge.output.node is TriggerDataNodeView sourceTriggerDataNodeView)
                {
                    if (edge.input.node is TriggerNodeView targetTriggerNodeView)
                    {
                        // This is a data connection from TriggerDataNode to TriggerNode argument
                        var connection = new ArgumentDataConnection
                        {
                            SourceDataNodeGUID = sourceTriggerDataNodeView.NodeData.GUID,
                            SourcePortName = edge.output.portName,
                            TargetFieldName = edge.input.portName // Port name should match field name
                        };
                        targetTriggerNodeView.NodeData.ArgumentDataConnections.Add(connection);
                    }
                    else if (edge.input.node is LogicNodeView targetLogicNodeView && 
                             edge.input == targetLogicNodeView.ConditionInputPort)
                    {
                        // This is a data connection from TriggerDataNode to LogicNode Condition input
                        var connection = new ArgumentDataConnection
                        {
                            SourceDataNodeGUID = sourceTriggerDataNodeView.NodeData.GUID,
                            SourcePortName = edge.output.portName,
                            TargetFieldName = "Condition" // Condition field name
                        };
                        targetLogicNodeView.NodeData.ConditionDataConnection = connection;
                    }
                }
            }

            // Mark the asset as dirty to ensure it's saved to disk
            EditorUtility.SetDirty(_currentGraph);

            // Save all pending changes to the asset
            AssetDatabase.SaveAssets();

            _isDirty = false;
            UpdateTitle();

            Debug.Log("Dialogue Graph saved successfully.");
        }
        private void OnDisable()
        {
            if (_isDirty)
            {
                // Wy�wietl okno dialogowe z ostrze�eniem
                bool saveChanges = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "The Dialogue Graph has unsaved changes. Do you want to save them before closing?",
                    "Save and Close",  
                    "Discard Changes"
                );

                if (saveChanges)
                {
                    SaveGraph();
                }
            }
        }

        public void AddNodeToGraph(DialogueNode nodeData)
        {
            if (_currentGraph == null)
            {
                Debug.LogError("Cannot add node: No DialogueSequence asset is currently selected.");
                return;
            }
            // Add the new data to the list in the ScriptableObject
            _currentGraph.Nodes.Add(nodeData);

            // If it's the first node (and no trigger nodes exist), set it as the start node
            if (_currentGraph.Nodes.Count == 1 && _currentGraph.TriggerNodes.Count == 0 && string.IsNullOrEmpty(_currentGraph.StartNodeGUID))
            {
                _currentGraph.StartNodeGUID = nodeData.GUID;
            }

            if (_serializedObject != null)
            {
                _serializedObject.Update();
            }

            SetDirty();
        }

        public void AddTriggerNodeToGraph(TriggerNode nodeData)
        {
            if (_currentGraph == null)
            {
                Debug.LogError("Cannot add trigger node: No DialogueSequence asset is currently selected.");
                return;
            }
            // Add the new data to the list in the ScriptableObject
            _currentGraph.TriggerNodes.Add(nodeData);

            // If it's the first node (and no dialogue nodes exist), set it as the start node
            if (_currentGraph.TriggerNodes.Count == 1 && _currentGraph.Nodes.Count == 0 && string.IsNullOrEmpty(_currentGraph.StartNodeGUID))
            {
                _currentGraph.StartNodeGUID = nodeData.GUID;
            }

            if (_serializedObject != null)
            {
                _serializedObject.Update();
            }

            SetDirty();
        }

        public void AddTriggerDataNodeToGraph(TriggerDataNode nodeData)
        {
            if (_currentGraph == null)
            {
                Debug.LogError("Cannot add trigger data node: No DialogueSequence asset is currently selected.");
                return;
            }
            // Add the new data to the list in the ScriptableObject
            _currentGraph.TriggerDataNodes.Add(nodeData);

            if (_serializedObject != null)
            {
                _serializedObject.Update();
            }

            SetDirty();
        }

        public void AddLogicNodeToGraph(LogicNode nodeData)
        {
            if (_currentGraph == null)
            {
                Debug.LogError("Cannot add logic node: No DialogueSequence asset is currently selected.");
                return;
            }
            // Add the new data to the list in the ScriptableObject
            _currentGraph.LogicNodes.Add(nodeData);

            if (_serializedObject != null)
            {
                _serializedObject.Update();
            }

            SetDirty();
        }

        public DialogueGraphView GetGraphView()
        {
            return _graphView;
        }
    }
}