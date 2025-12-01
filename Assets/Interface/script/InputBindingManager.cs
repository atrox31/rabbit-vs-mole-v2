using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interface
{
    /// <summary>
    /// Manages input binding operations including loading, saving, and rebinding
    /// </summary>
    public static class InputBindingManager
    {
        private const string DefaultBindingLocalizationPrefix = "key_bind_";

        /// <summary>
        /// Loads input bindings from PlayerPrefs
        /// </summary>
        public static void LoadInputBindings(InputActionAsset asset)
        {
            if (asset == null)
                return;

            if (!PlayerPrefs.HasKey(PlayerPrefsConst.INPUT_BINDINGS))
                return;

            string json = PlayerPrefs.GetString(PlayerPrefsConst.INPUT_BINDINGS, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                asset.LoadBindingOverridesFromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load input bindings: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves input bindings to PlayerPrefs
        /// </summary>
        public static void SaveInputBindings(InputActionAsset asset)
        {
            if (asset == null)
                return;

            try
            {
                string json = asset.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString(PlayerPrefsConst.INPUT_BINDINGS, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save input bindings: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all input binding overrides
        /// </summary>
        public static void ClearInputBindings(InputActionAsset asset)
        {
            if (asset == null)
                return;

            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PlayerPrefsConst.INPUT_BINDINGS);
        }

        /// <summary>
        /// Gets an action map from an input action asset
        /// </summary>
        public static InputActionMap GetActionMap(InputActionAsset asset, string actionMapName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(actionMapName))
                return null;

            try
            {
                return asset.FindActionMap(actionMapName, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Cannot find action map '{actionMapName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the binding index for an action, optionally filtered by binding group
        /// </summary>
        public static int GetBindingIndex(InputAction action, string bindingGroup = null)
        {
            if (action == null)
                return -1;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite)
                    continue;

                if (string.IsNullOrWhiteSpace(bindingGroup))
                    return i;

                if (BindingContainsGroup(binding, bindingGroup))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Gets the sprite tag for displaying a binding (e.g., &lt;sprite name="key"&gt;)
        /// </summary>
        public static string GetBindingSpriteTag(InputAction action, int bindingIndex)
        {
            string spriteName = GetBindingSpriteName(action, bindingIndex);
            if (string.IsNullOrEmpty(spriteName))
            {
                return "-";
            }

            return $"<sprite name=\"{spriteName}\">";
        }

        /// <summary>
        /// Gets the sprite name from a binding path
        /// </summary>
        public static string GetBindingSpriteName(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return string.Empty;

            var binding = action.bindings[bindingIndex];
            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path))
                path = binding.path;

            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int separatorIndex = path.LastIndexOf('/');
            string name = separatorIndex >= 0 ? path.Substring(separatorIndex + 1) : path;
            name = name.Replace("<", string.Empty).Replace(">", string.Empty);

            return string.IsNullOrEmpty(name) ? string.Empty : name;
        }

        /// <summary>
        /// Begins an interactive rebinding operation for an action
        /// </summary>
        public static InputActionRebindingExtensions.RebindingOperation BeginRebind(InputAction action, int bindingIndex, Action onComplete, Action onCancel = null, float waitForAnother = 0.1f)
        {
            if (action == null)
                return null;

            try
            {
                // Disable action before rebinding
                bool wasEnabled = action.enabled;
                if (wasEnabled)
                {
                    action.Disable();
                }

                var operation = action.PerformInteractiveRebinding(bindingIndex)
                    .WithCancelingThrough("<Keyboard>/escape")
                    .OnMatchWaitForAnother(waitForAnother)
                    .OnComplete(op =>
                    {
                        op.Dispose();
                        
                        // Get the binding path after rebinding and normalize it
                        string currentPath = action.bindings[bindingIndex].effectivePath;
                        if (string.IsNullOrEmpty(currentPath))
                        {
                            currentPath = action.bindings[bindingIndex].path;
                        }
                        
                        // Normalize the binding path to remove key variants (e.g., leftShift -> shift, numpad0 -> 0)
                        if (!string.IsNullOrEmpty(currentPath))
                        {
                            string normalizedPath = NormalizeBindingPath(currentPath);
                            if (!string.IsNullOrEmpty(normalizedPath) && normalizedPath != currentPath)
                            {
                                action.ApplyBindingOverride(bindingIndex, normalizedPath);
                            }
                        }
                        
                        // Re-enable action after rebinding
                        if (wasEnabled)
                        {
                            action.Enable();
                        }
                        onComplete?.Invoke();
                        SaveInputBindings(action.actionMap?.asset);
                    })
                    .OnCancel(op =>
                    {
                        op.Dispose();
                        // Re-enable action after cancel
                        if (wasEnabled)
                        {
                            action.Enable();
                        }
                        onCancel?.Invoke();
                    });

                // Restrict device types based on action map name
                string actionMapName = action.actionMap?.name ?? string.Empty;
                if (actionMapName.Contains("Keyboard", StringComparison.OrdinalIgnoreCase))
                {
                    // Only allow keyboard devices
                    operation.WithControlsExcluding("<Gamepad>");
                    operation.WithControlsExcluding("<Mouse>");
                }
                else if (actionMapName.Contains("Gamepad", StringComparison.OrdinalIgnoreCase))
                {
                    // Only allow gamepad devices
                    operation.WithControlsExcluding("<Keyboard>");
                    operation.WithControlsExcluding("<Mouse>");
                }

                operation.Start();
                return operation;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to start rebinding for action '{action?.name}': {ex.Message}");
                // Re-enable action if it was enabled before
                if (action.enabled == false && action.actionMap != null)
                {
                    try
                    {
                        action.Enable();
                    }
                    catch { }
                }
                return null;
            }
        }

        /// <summary>
        /// Handles when a binding has been changed
        /// </summary>
        public static void HandleBindingChanged(InputAction action, int bindingIndex, string spriteName)
        {
            if (action?.actionMap?.asset == null)
                return;

            SaveInputBindings(action.actionMap.asset);
            Debug.Log($"Binding updated for '{action.name}' ({bindingIndex}): {spriteName}");
        }

        /// <summary>
        /// Builds a localized key string from an action name
        /// </summary>
        public static string BuildLocalizedKey(string actionName, string prefix = null)
        {
            if (string.IsNullOrEmpty(actionName))
                return string.Empty;

            string sanitizedName = actionName.Trim().ToLowerInvariant().Replace(" ", "_");
            string effectivePrefix = string.IsNullOrEmpty(prefix) ? DefaultBindingLocalizationPrefix : prefix;
            return $"{effectivePrefix}{sanitizedName}";
        }

        /// <summary>
        /// Normalizes a binding path to remove key variants (e.g., leftShift -> shift, numpad0 -> 0)
        /// </summary>
        private static string NormalizeBindingPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Extract device and key parts
            int separatorIndex = path.LastIndexOf('/');
            if (separatorIndex < 0)
                return path;

            string device = path.Substring(0, separatorIndex + 1);
            string key = path.Substring(separatorIndex + 1);
            
            // Remove angle brackets if present (Unity format: <Keyboard>/<leftShift> or <Keyboard>/leftShift)
            key = key.Replace("<", string.Empty).Replace(">", string.Empty);

            // Normalize keyboard key variants
            if (device.Contains("<Keyboard>"))
            {
                // Modifier keys - normalize left/right variants
                if (key.Equals("leftShift", StringComparison.OrdinalIgnoreCase) || 
                    key.Equals("rightShift", StringComparison.OrdinalIgnoreCase))
                {
                    key = "shift";
                }
                else if (key.Equals("leftCtrl", StringComparison.OrdinalIgnoreCase) || 
                         key.Equals("rightCtrl", StringComparison.OrdinalIgnoreCase))
                {
                    key = "ctrl";
                }
                else if (key.Equals("leftAlt", StringComparison.OrdinalIgnoreCase) || 
                         key.Equals("rightAlt", StringComparison.OrdinalIgnoreCase))
                {
                    key = "alt";
                }
                // Numpad keys - normalize to main keyboard equivalents
                else if (key.StartsWith("numpad", StringComparison.OrdinalIgnoreCase))
                {
                    string numpadKey = key.Substring(6); // Remove "numpad" prefix
                    
                    // Map numpad numbers to regular numbers
                    if (numpadKey.Length == 1 && char.IsDigit(numpadKey[0]))
                    {
                        key = numpadKey; // numpad0 -> 0, numpad1 -> 1, etc.
                    }
                    // Map numpad special keys
                    else if (numpadKey.Equals("enter", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "enter";
                    }
                    else if (numpadKey.Equals("plus", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "equals"; // numpadPlus -> equals (closest equivalent)
                    }
                    else if (numpadKey.Equals("minus", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "minus";
                    }
                    else if (numpadKey.Equals("multiply", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "8"; // numpadMultiply -> 8 (asterisk on 8)
                    }
                    else if (numpadKey.Equals("divide", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "slash"; // numpadDivide -> slash
                    }
                    else if (numpadKey.Equals("period", StringComparison.OrdinalIgnoreCase) || 
                             numpadKey.Equals("dot", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "period";
                    }
                    else
                    {
                        // Keep original if we don't have a mapping
                        return path;
                    }
                }
            }

            // Reconstruct path (Unity format: <Device>/keyName, no angle brackets around key)
            return $"{device}{key}";
        }

        /// <summary>
        /// Checks if a binding contains a specific group
        /// </summary>
        private static bool BindingContainsGroup(InputBinding binding, string bindingGroup)
        {
            if (string.IsNullOrWhiteSpace(binding.groups))
                return false;

            var groups = binding.groups.Split(';');
            return groups.Any(group => string.Equals(group.Trim(), bindingGroup, StringComparison.OrdinalIgnoreCase));
        }
    }
}

