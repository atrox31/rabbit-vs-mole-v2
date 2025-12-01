using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace DialogueSystem.Editor
{
    public class DialogueGraphView : GraphView
    {
        private DialogueGraphEditor _editor;

        public DialogueGraphView(DialogueGraphEditor editor)
        {
            _editor = editor;

            // Setup panning, zooming
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            // Add grid background
            this.Insert(0, new GridBackground());

            // Add manipulation abilities (selection, dragging)
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());
        }

        // Defines which ports can be connected
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction && // Input to Output or vice versa
                endPort.node != startPort.node &&           // Not connecting to itself
                ArePortsCompatible(startPort, endPort)).ToList();
        }

        private bool ArePortsCompatible(Port startPort, Port endPort)
        {
            // Allow connections from TriggerDataNode output ports to TriggerNode argument input ports
            if (startPort.node is TriggerDataNodeView sourceDataNode && endPort.node is TriggerNodeView)
            {
                // For TD_GameManagerGet, require exact type match (no object fallback)
                if (sourceDataNode.NodeData != null && 
                    sourceDataNode.NodeData.DataTypeName != null &&
                    sourceDataNode.NodeData.DataTypeName.Contains("TD_GameManagerGet"))
                {
                    // Require exact type match for GameManagerGet
                    return startPort.portType == endPort.portType;
                }
                
                // For other TriggerDataNodes, check if types are compatible
                return startPort.portType == endPort.portType || 
                       startPort.portType.IsAssignableFrom(endPort.portType) ||
                       endPort.portType.IsAssignableFrom(startPort.portType);
            }

            // Allow connections from TriggerNode argument input ports to TriggerDataNode output ports
            if (startPort.node is TriggerNodeView && endPort.node is TriggerDataNodeView targetDataNode)
            {
                // For TD_GameManagerGet, require exact type match
                if (targetDataNode.NodeData != null && 
                    targetDataNode.NodeData.DataTypeName != null &&
                    targetDataNode.NodeData.DataTypeName.Contains("TD_GameManagerGet"))
                {
                    return startPort.portType == endPort.portType;
                }
                
                return startPort.portType == endPort.portType || 
                       startPort.portType.IsAssignableFrom(endPort.portType) ||
                       endPort.portType.IsAssignableFrom(startPort.portType);
            }

            // For other connections (DialogueNode, TriggerNode flow), allow if types match
            return startPort.portType == endPort.portType;
        }

        // Method to create a new dialogue node
        public void CreateNode(Vector2 position)
        {
            var nodeData = new DialogueSystem.Nodes.DialogueNode(position);
            _editor.AddNodeToGraph(nodeData);
            var nodeView = new DialogueNodeView(nodeData, _editor);

            AddElement(nodeView);
        }

        // Method to create a new trigger node
        public void CreateTriggerNode(Vector2 position)
        {
            var triggerNodeData = new DialogueSystem.Nodes.TriggerNode(position);
            _editor.AddTriggerNodeToGraph(triggerNodeData);
            var triggerNodeView = new TriggerNodeView(triggerNodeData, _editor);

            AddElement(triggerNodeView);
        }

        // Method to create a new trigger node with a specific trigger type
        public void CreateTriggerNodeWithType(Vector2 position, System.Type triggerType)
        {
            var triggerNodeData = new DialogueSystem.Nodes.TriggerNode(position);
            var args = TriggerTypeHelper.CreateArgumentsInstance(triggerType);
            if (args != null)
            {
                triggerNodeData.SetTrigger(TriggerTypeHelper.GetFullTypeName(triggerType), args);
            }
            _editor.AddTriggerNodeToGraph(triggerNodeData);
            var triggerNodeView = new TriggerNodeView(triggerNodeData, _editor);

            AddElement(triggerNodeView);
        }

        // Method to create a new trigger data node
        public void CreateTriggerDataNode(Vector2 position)
        {
            var triggerDataNodeData = new DialogueSystem.Nodes.TriggerDataNode(position);
            _editor.AddTriggerDataNodeToGraph(triggerDataNodeData);
            var triggerDataNodeView = new TriggerDataNodeView(triggerDataNodeData, _editor);

            AddElement(triggerDataNodeView);
        }

        // Method to create a new trigger data node with a specific data type
        public void CreateTriggerDataNodeWithType(Vector2 position, System.Type dataType)
        {
            var triggerDataNodeData = new DialogueSystem.Nodes.TriggerDataNode(position);
            var data = TriggerDataTypeHelper.CreateDataInstance(dataType);
            if (data != null)
            {
                triggerDataNodeData.SetData(TriggerDataTypeHelper.GetFullTypeName(dataType), data);
            }
            _editor.AddTriggerDataNodeToGraph(triggerDataNodeData);
            var triggerDataNodeView = new TriggerDataNodeView(triggerDataNodeData, _editor);

            AddElement(triggerDataNodeView);
        }

        // Method to create a new logic node
        public void CreateLogicNode(Vector2 position)
        {
            var logicNodeData = new DialogueSystem.Nodes.LogicNode(position);
            _editor.AddLogicNodeToGraph(logicNodeData);
            var logicNodeView = new LogicNodeView(logicNodeData, _editor);

            AddElement(logicNodeView);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Check if clicking on an edge
            if (evt.target is Edge edge)
            {
                evt.menu.AppendAction(
                    "Delete Connection",
                    (action) => DeleteEdge(edge),
                    DropdownMenuAction.AlwaysEnabled
                );
                return;
            }

            // Check if the click was on a node by walking up the hierarchy from the target
            VisualElement target = evt.target as VisualElement;
            if (target != null)
            {
                VisualElement current = target;
                while (current != null && current != this)
                {
                    if (current is DialogueNodeView || current is TriggerNodeView || current is TriggerDataNodeView || current is StartNodeView || current is LogicNodeView)
                    {
                        // Let the node handle its own menu - don't interfere
                        // The node's BuildContextualMenu will be called automatically
                        return;
                    }
                    current = current.parent;
                }
            }

            // If not on a node, show the graph view menu
            // Reset the default menu before adding our custom items
            evt.menu.MenuItems().Clear();

            // Get the position where the user right-clicked (in world space)
            Vector2 screenMousePosition = evt.localMousePosition;

            // Convert the screen position to the GraphView's local content position
            Vector2 worldMousePosition = contentViewContainer.WorldToLocal(screenMousePosition);

            evt.menu.AppendAction(
                "Create Dialogue Node",
                (action) => CreateNode(worldMousePosition),
                DropdownMenuAction.AlwaysEnabled
            );

            evt.menu.AppendAction(
                "Create Logic Node",
                (action) => CreateLogicNode(worldMousePosition),
                DropdownMenuAction.AlwaysEnabled
            );

            // Add trigger node creation with submenu for trigger types
            var triggerTypes = TriggerTypeHelper.GetTriggerTypes();
            if (triggerTypes.Count > 0)
            {
                var position = worldMousePosition; // Capture for closure
                evt.menu.AppendAction(
                    "Create Trigger Node/Empty",
                    (action) => CreateTriggerNode(position),
                    DropdownMenuAction.AlwaysEnabled
                );
                evt.menu.AppendSeparator("Create Trigger Node/");

                foreach (var triggerType in triggerTypes)
                {
                    var displayName = TriggerTypeHelper.GetTriggerDisplayName(triggerType);
                    var type = triggerType; // Capture for closure
                    evt.menu.AppendAction(
                        $"Create Trigger Node/{displayName}",
                        (action) => CreateTriggerNodeWithType(position, type),
                        DropdownMenuAction.AlwaysEnabled
                    );
                }
            }
            else
            {
                evt.menu.AppendAction(
                    "Create Trigger Node",
                    (action) => CreateTriggerNode(worldMousePosition),
                    DropdownMenuAction.AlwaysEnabled
                );
            }

            // Add trigger data node creation with submenu for data types
            var dataTypes = TriggerDataTypeHelper.GetDataTypes();
            if (dataTypes.Count > 0)
            {
                var position = worldMousePosition; // Capture for closure
                evt.menu.AppendAction(
                    "Create Trigger Data/Empty",
                    (action) => CreateTriggerDataNode(position),
                    DropdownMenuAction.AlwaysEnabled
                );
                evt.menu.AppendSeparator("Create Trigger Data/");

                foreach (var dataType in dataTypes)
                {
                    var displayName = TriggerDataTypeHelper.GetDataDisplayName(dataType);
                    var type = dataType; // Capture for closure
                    evt.menu.AppendAction(
                        $"Create Trigger Data/{displayName}",
                        (action) => CreateTriggerDataNodeWithType(position, type),
                        DropdownMenuAction.AlwaysEnabled
                    );
                }
            }
            else
            {
                evt.menu.AppendAction(
                    "Create Trigger Data",
                    (action) => CreateTriggerDataNode(worldMousePosition),
                    DropdownMenuAction.AlwaysEnabled
                );
            }

            evt.menu.AppendSeparator();
        }

        private void DeleteEdge(Edge edge)
        {
            RemoveElement(edge);
            _editor.SetDirty();
        }
        
        public void ClearGraph()
        {
            // Remove all nodes and edges
            DeleteElements(graphElements.Where(elem => elem is Node || elem is Edge));
        }
    }
}