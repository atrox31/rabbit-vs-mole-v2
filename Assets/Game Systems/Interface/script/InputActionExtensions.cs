using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interface
{
    /// <summary>
    /// Extension methods for InputAction to support rebinding operations
    /// </summary>
    public static class InputActionExtensions
    {
        /// <summary>
        /// Begins an interactive rebinding operation with device restrictions based on action map name
        /// </summary>
        public static InputActionRebindingExtensions.RebindingOperation BeginRebindWithDeviceRestrictions(
            this InputAction action,
            int bindingIndex,
            Action onComplete,
            Action onCancel = null,
            float waitForAnother = 0.1f)
        {
            return InputBindingManager.BeginRebind(action, bindingIndex, onComplete, onCancel, waitForAnother);
        }

        /// <summary>
        /// Gets the sprite tag for displaying the current binding
        /// </summary>
        public static string GetBindingSpriteTag(this InputAction action, int bindingIndex)
        {
            return InputBindingManager.GetBindingSpriteTag(action, bindingIndex);
        }

        /// <summary>
        /// Gets the sprite name from the binding path
        /// </summary>
        public static string GetBindingSpriteName(this InputAction action, int bindingIndex)
        {
            return InputBindingManager.GetBindingSpriteName(action, bindingIndex);
        }

        /// <summary>
        /// Gets the binding index for this action, optionally filtered by binding group
        /// </summary>
        public static int GetBindingIndex(this InputAction action, string bindingGroup = null)
        {
            return InputBindingManager.GetBindingIndex(action, bindingGroup);
        }
    }
}

