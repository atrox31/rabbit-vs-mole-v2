using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Nodes
{
    [Serializable]
    public class DialogueNode
    {
        // GUID used by GraphView/Editor to uniquely identify this node
        public string GUID;

        // Position used by GraphView/Editor to draw the node on screen
        public Vector2 EditorPosition;

        // --- CONTENT  ---
        public Actor _actor;
        public string _poseName = "(none)";
        public ActorSideOnScreen ScreenPosition;
        [TextArea(3, 10)] public string text;

        // --- CONNECTION LOGIC  ---
        // For a simple text node, it will have ONE connection to the next node.
        // For a Choice Node, this list would have multiple elements.
        public List<NodeLink> ExitPorts = new List<NodeLink>();

        // Constructor for editor utility
        public DialogueNode(Vector2 position)
        {
            GUID = Guid.NewGuid().ToString();
            EditorPosition = position;
        }

        public override string ToString()
        {
            // Implementation for debugging
            return $"[DialogueNode] Actor: {_actor?.actorName}, Text: {text.Truncate(12)}";
        }
    }
}