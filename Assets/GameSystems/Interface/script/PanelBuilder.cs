using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.InputSystem;
using Interface.Element;
using UnityEngine;

namespace Interface
{
    public class PanelBuilder
    {
        private MainMenuManager _manager;
        private GUIPanel _panel;
        private InterfaceElement _lastAddedInterfaceElement;

        public PanelBuilder(MainMenuManager manager, GUIPanel panel)
        {
            _manager = manager;
            _panel = panel;
        }

        public PanelBuilder AddButton(string text, Action onClick, bool isBottomButton = false)
        {
            var button = _manager.CreateButton(text, onClick);
            _lastAddedInterfaceElement = _panel.AddElement(button, isBottomButton);
            return this;
        }

        public PanelBuilder AddButton(LocalizedString localizedText, Action onClick, bool isBottomButton = false)
        {
            var button = _manager.CreateButton(localizedText, onClick);
            _lastAddedInterfaceElement = _panel.AddElement(button, isBottomButton);
            return this;
        }

        public PanelBuilder AddButton(string text, GUIPanel targetPanel, bool isBottomButton = false)
        {
            return AddButton(text, () => _manager.ChangePanel(targetPanel), isBottomButton);
        }

        public PanelBuilder AddButton(LocalizedString localizedText, GUIPanel targetPanel, bool isBottomButton = false)
        {
            return AddButton(localizedText, () => _manager.ChangePanel(targetPanel), isBottomButton);
        }

        public PanelBuilder AddButton(LocalizedString localizedText, GUIPanel targetPanel, bool disabled, LocalizedString tooltipText, bool isBottomButton = false)
        {
            var button = _manager.CreateButton(localizedText, () => _manager.ChangePanel(targetPanel));
            if (button is Interface.Element.GUIButton guiButton)
            {
                guiButton.SetDisabled(disabled, tooltipText);
            }
            _lastAddedInterfaceElement = _panel.AddElement(button, isBottomButton);
            return this;
        }


        public PanelBuilder AddSlider(string label, Action<float> onValueChanged, Func<float> getCurrentValue = null, Func<float, string> valueFormatter = null)
        {
            var slider = _manager.CreateSlider(label, onValueChanged, getCurrentValue, valueFormatter);
            _lastAddedInterfaceElement = _panel.AddElement(slider);
            return this;
        }

        public PanelBuilder AddSlider(LocalizedString localizedLabel, Action<float> onValueChanged, Func<float> getCurrentValue = null, Func<float, string> valueFormatter = null)
        {
            var slider = _manager.CreateSlider(localizedLabel, onValueChanged, getCurrentValue, valueFormatter);
            _lastAddedInterfaceElement = _panel.AddElement(slider);
            return this;
        }

        public PanelBuilder AddToggle(string label, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            var toggle = _manager.CreateToggle(label, onValueChanged, getCurrentValue);
            _lastAddedInterfaceElement = _panel.AddElement(toggle);
            return this;
        }

        public PanelBuilder AddToggle(LocalizedString localizedLabel, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            var toggle = _manager.CreateToggle(localizedLabel, onValueChanged, getCurrentValue);
            _lastAddedInterfaceElement = _panel.AddElement(toggle);
            return this;
        }

        public PanelBuilder AddDropDown(string label, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            var dropdown = _manager.CreateDropdown(label, onValueChanged, options, getCurrentValue);
            _lastAddedInterfaceElement = _panel.AddElement(dropdown);
            return this;
        }

        public PanelBuilder AddDropDown(LocalizedString localizedLabel, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            var dropdown = _manager.CreateDropdown(localizedLabel, onValueChanged, options, getCurrentValue);
            _lastAddedInterfaceElement = _panel.AddElement(dropdown);
            return this;
        }

        public PanelBuilder AddDropDown(LocalizedString localizedLabel, Action<int> onValueChanged, List<LocalizedString> localizedOptions, Func<int> getCurrentValue = null)
        {
            var dropdown = _manager.CreateDropdown(localizedLabel, onValueChanged, localizedOptions, getCurrentValue);
            _lastAddedInterfaceElement = _panel.AddElement(dropdown);
            return this;
        }

        public PanelBuilder AddLabel(string text)
        {
            var label = _manager.CreateLabel(text);
            _lastAddedInterfaceElement = _panel.AddElement(label);
            return this;
        }

        public PanelBuilder AddLabel(LocalizedString localizedText)
        {
            var label = _manager.CreateLabel(localizedText);
            _lastAddedInterfaceElement = _panel.AddElement(label);
            return this;
        }

        public PanelBuilder AddSpacer(float height = 20f)
        {
            var spacer = _manager.CreateSpacer(height);
            _lastAddedInterfaceElement = _panel.AddElement(spacer);
            return this;
        }

        public PanelBuilder AddKeyBindControls(InputActionAsset inputActions, string actionMapName, string bindingGroup = null, string localizationPrefix = "key_bind_")
        {
            if (inputActions == null)
                return this;

            var actionMap = InputBindingManager.GetActionMap(inputActions, actionMapName);
            if (actionMap == null)
                return this;

            foreach (var action in actionMap.actions)
            {
                int bindingIndex = InputBindingManager.GetBindingIndex(action, bindingGroup);
                if (bindingIndex < 0)
                    continue;

                string localizationKey = InputBindingManager.BuildLocalizedKey(action.name, localizationPrefix);
                LocalizedString localizedLabel = string.IsNullOrEmpty(localizationKey)
                    ? new LocalizedString()
                    : _manager.GetLocalizedString(localizationKey);
                var keyBinder = _manager.CreateKeyBinder(localizedLabel, action, bindingIndex, bindingGroup);
                if (keyBinder != null)
                {
                    _lastAddedInterfaceElement = _panel.AddElement(keyBinder);
                }
            }

            return this;
        }

        public PanelBuilder AddBackButton()
        {
            // Try to use localized version from Inspector first
            var localizedBack = _manager.GetBackButtonLocalized();
            if (localizedBack != null && localizedBack.IsEmpty == false)
            {
                return AddButton(localizedBack, () => _manager.GoBack(), true);
            }
            
            // Fallback to creating LocalizedString from key
            try
            {
                var localizedBackFromKey = _manager.GetLocalizedString("button_back");
                return AddButton(localizedBackFromKey, () => _manager.GoBack(), true);
            }
            catch
            {
                // Final fallback to plain text
                return AddButton(_manager.GetBackButtonText(), () => _manager.GoBack(), true);
            }
        }

        public PanelBuilder AddExitButton()
        {
            // Try to use localized version from Inspector first
            var localizedExit = _manager.GetExitButtonLocalized();
            if (localizedExit != null && localizedExit.IsEmpty == false)
            {
                return AddButton(localizedExit, () => _manager.ExitGame(), true);
            }
            
            // Fallback to creating LocalizedString from key
            try
            {
                var localizedExitFromKey = _manager.GetLocalizedString("button_exit");
                return AddButton(localizedExitFromKey, () => _manager.ExitGame(), true);
            }
            catch
            {
                // Final fallback to plain text
                return AddButton(_manager.GetExitButtonText(), () => _manager.ExitGame(), true);
            }
        }

        public PanelBuilder AddAutoScroll(float speed, float delay)
        {
            if (_panel != null)
            {
                _panel.SetAutoScroll(speed, delay);
            }
            return this;
        }

        /// <summary>
        /// Adds a custom element from a prefab to the panel
        /// </summary>
        /// <param name="prefab">Prefab GameObject containing the custom element component</param>
        /// <param name="isBottomElement">Whether this element should be placed at the bottom of the panel</param>
        /// <returns>PanelBuilder instance for method chaining</returns>
        public PanelBuilder AddCustomElement(GameObject prefab, bool isBottomElement = false)
        {
            return AddCustomElement(prefab, null, isBottomElement);
        }

        /// <summary>
        /// Adds a custom element from a prefab to the panel with an initialization argument
        /// </summary>
        /// <param name="prefab">Prefab GameObject containing the custom element component</param>
        /// <param name="argument">Argument to pass to the custom element's InitializeWithArgument method</param>
        /// <param name="isBottomElement">Whether this element should be placed at the bottom of the panel</param>
        /// <returns>PanelBuilder instance for method chaining</returns>
        public PanelBuilder AddCustomElement(GameObject prefab, object argument, bool isBottomElement = false)
        {
            var customElement = _manager.CreateCustomElement(prefab, argument);
            if (customElement != null)
            {
                _lastAddedInterfaceElement = _panel.AddElement(customElement, isBottomElement);
            }
            return this;
        }

        public GUIPanel Build()
        {
            return _panel;
        }

        public PanelBuilder SetId(string id)
        {
            if (_panel != null && _lastAddedInterfaceElement != null)
            {
                _lastAddedInterfaceElement.ElementID = id;
            }
            return this;
        }
    }
}

