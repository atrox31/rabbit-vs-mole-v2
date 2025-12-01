using DialogueSystem.Nodes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Create Dialogue Graph")]
    public class DialogueSequence : ScriptableObject
    {
        // This list is serialized by Unity (ScriptableObject) and will be filled by GraphView.
        [HideInInspector]
        public List<DialogueNode> Nodes = new List<DialogueNode>();

        // Trigger nodes list
        [HideInInspector]
        public List<TriggerNode> TriggerNodes = new List<TriggerNode>();

        // Trigger data nodes list
        [HideInInspector]
        public List<TriggerDataNode> TriggerDataNodes = new List<TriggerDataNode>();

        // Logic nodes list
        [HideInInspector]
        public List<LogicNode> LogicNodes = new List<LogicNode>();

        // Start node - mandatory entry point
        [HideInInspector]
        public StartNode StartNode = new StartNode();

        // GameObject references for TD_GameObject nodes
        // These can be assigned in the inspector and referenced by name in TD_GameObject nodes
        [System.Serializable]
        public class GameObjectReference
        {
            public string Name;
            public GameObject GameObject;
        }

        [HideInInspector]
        public List<GameObjectReference> GameObjectReferences = new List<GameObjectReference>();

        // GUID of the first node where the conversation starts.
        // This should always point to StartNode.GUID, but kept for compatibility
        public string StartNodeGUID
        {
            get => StartNode?.GUID ?? "START_NODE";
            set { } // Ignore setter - StartNodeGUID is always from StartNode
        }

        // Runtime Dictionary for fast lookup (populated in Awake/OnEnable)
        private Dictionary<string, DialogueNode> _nodeMap;
        private Dictionary<string, TriggerNode> _triggerNodeMap;
        private Dictionary<string, TriggerDataNode> _triggerDataNodeMap;
        private Dictionary<string, LogicNode> _logicNodeMap;

        public Dictionary<string, DialogueNode> NodeMap
        {
            get
            {
                if (_nodeMap == null || _nodeMap.Count != Nodes.Count)
                {
                    _nodeMap = Nodes.ToDictionary(node => node.GUID);
                }
                return _nodeMap;
            }
        }

        public Dictionary<string, TriggerNode> TriggerNodeMap
        {
            get
            {
                if (_triggerNodeMap == null || _triggerNodeMap.Count != TriggerNodes.Count)
                {
                    _triggerNodeMap = TriggerNodes.ToDictionary(node => node.GUID);
                }
                return _triggerNodeMap;
            }
        }

        public Dictionary<string, TriggerDataNode> TriggerDataNodeMap
        {
            get
            {
                if (_triggerDataNodeMap == null || _triggerDataNodeMap.Count != TriggerDataNodes.Count)
                {
                    _triggerDataNodeMap = TriggerDataNodes.ToDictionary(node => node.GUID);
                }
                return _triggerDataNodeMap;
            }
        }

        public Dictionary<string, LogicNode> LogicNodeMap
        {
            get
            {
                if (_logicNodeMap == null || _logicNodeMap.Count != LogicNodes.Count)
                {
                    _logicNodeMap = LogicNodes.ToDictionary(node => node.GUID);
                }
                return _logicNodeMap;
            }
        }

        /// <summary>
        /// Gets a node by GUID, checking StartNode, DialogueNodes, TriggerNodes, TriggerDataNodes, and LogicNodes.
        /// </summary>
        public object GetNodeByGUID(string guid)
        {
            // Check StartNode first
            if (StartNode != null && StartNode.GUID == guid)
                return StartNode;
            
            if (NodeMap.ContainsKey(guid))
                return NodeMap[guid];
            if (TriggerNodeMap.ContainsKey(guid))
                return TriggerNodeMap[guid];
            if (TriggerDataNodeMap.ContainsKey(guid))
                return TriggerDataNodeMap[guid];
            if (LogicNodeMap.ContainsKey(guid))
                return LogicNodeMap[guid];
            return null;
        }

        /// <summary>
        /// Gets a TriggerDataNode by GUID.
        /// </summary>
        public TriggerDataNode GetTriggerDataNodeByGUID(string guid)
        {
            return TriggerDataNodeMap.ContainsKey(guid) ? TriggerDataNodeMap[guid] : null;
        }

        /// <summary>
        /// Gets a GameObject by name from the GameObjectReferences list.
        /// </summary>
        public GameObject GetGameObjectByName(string name)
        {
            if (string.IsNullOrEmpty(name) || GameObjectReferences == null)
                return null;

            foreach (var reference in GameObjectReferences)
            {
                if (reference != null && reference.Name == name)
                {
                    return reference.GameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a GUID belongs to a DialogueNode.
        /// </summary>
        public bool IsDialogueNode(string guid)
        {
            return NodeMap.ContainsKey(guid);
        }

        /// <summary>
        /// Checks if a GUID belongs to a TriggerNode.
        /// </summary>
        public bool IsTriggerNode(string guid)
        {
            return TriggerNodeMap.ContainsKey(guid);
        }

        /// <summary>
        /// Checks if a GUID belongs to a LogicNode.
        /// </summary>
        public bool IsLogicNode(string guid)
        {
            return LogicNodeMap.ContainsKey(guid);
        }
    }
}