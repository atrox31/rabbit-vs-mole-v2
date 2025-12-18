using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Nodes
{
    /// <summary>
    /// Start node - mandatory entry point for dialogue sequences.
    /// </summary>
    [Serializable]
    public class StartNode
    {
        // GUID used by GraphView/Editor to uniquely identify this node
        public string GUID = "START_NODE";

        // Position used by GraphView/Editor to draw the node on screen
        public Vector2 EditorPosition = new Vector2(50, 50);

        // --- CONNECTION LOGIC ---
        public List<NodeLink> ExitPorts = new List<NodeLink>();

        public StartNode()
        {
            // Start node always has the same GUID
            GUID = "START_NODE";
        }

        public override string ToString()
        {
            return "[StartNode]";
        }
    }
}

