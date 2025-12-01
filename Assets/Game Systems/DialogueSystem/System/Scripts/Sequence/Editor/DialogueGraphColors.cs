using UnityEngine;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Centralized color definitions for the Dialogue Graph Editor.
    /// </summary>
    public static class DialogueGraphColors
    {
        // Node colors
        public static readonly Color DialogueNodeColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        public static readonly Color TriggerNodeColor = new Color(0.3f, 0.6f, 0.3f, 0.8f);
        public static readonly Color TriggerDataNodeColor = new Color(0.6f, 0.3f, 0.6f, 0.8f);
        public static readonly Color StartNodeColor = new Color(0.8f, 0.4f, 0.2f, 0.8f);
        public static readonly Color LogicNodeColor = new Color(0.8f, 0.6f, 0.2f, 0.8f); // Orange/Yellow
    }
}

