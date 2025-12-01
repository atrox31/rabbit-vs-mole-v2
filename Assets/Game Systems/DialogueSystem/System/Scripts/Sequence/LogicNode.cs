using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Nodes
{
    /// <summary>
    /// Logic node - evaluates a condition and routes to True or False output.
    /// </summary>
    [Serializable]
    public class LogicNode
    {
        // GUID used by GraphView/Editor to uniquely identify this node
        public string GUID;

        // Position used by GraphView/Editor to draw the node on screen
        public Vector2 EditorPosition;

        // --- CONNECTION LOGIC ---
        public List<NodeLink> ExitPortsTrue = new List<NodeLink>();
        public List<NodeLink> ExitPortsFalse = new List<NodeLink>();

        // Condition value (can be set manually or via input connection)
        public bool Condition = false;

        // Condition input connection from TriggerDataNode
        public ArgumentDataConnection ConditionDataConnection;

        // Constructor for editor utility
        public LogicNode(Vector2 position)
        {
            GUID = Guid.NewGuid().ToString();
            EditorPosition = position;
        }

        public override string ToString()
        {
            return $"[LogicNode] Condition: {Condition}";
        }
    }
}


