using GameSystems.Steam;
using Interface.Element;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

namespace Interface
{
    public class MainMenuManager : MonoBehaviour, ICancelHandler
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _panelPrefab;
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private GameObject _sliderPrefab;
        [SerializeField] private GameObject _togglePrefab;
        [SerializeField] private GameObject _dropdownPrefab;
        [SerializeField] private GameObject _keyBinderPrefab;
        [SerializeField] private GameObject _toolTipPrefab;
        [SerializeField] private GameObject _labelPrefab;

        [Header("Steam")]
        [SerializeField] private SteamAvatar _avatarPrefab;
        public SteamAvatar SteamAvatar { get { return _avatarPrefab; } private set { _avatarPrefab = value; }}

        [Header("Settings")]
        [SerializeField] private Transform _panelsParent;
        [SerializeField] private float _animationDuration = 0.5f;
        
        [Header("Panel Animation Settings")]
        [SerializeField] private PanelAnimationType _panelAnimationType = PanelAnimationType.Fade;
        [SerializeField] private SlideDirection _slideDirection = SlideDirection.Left;
        [SerializeField] private float _slideDistance = 500f;
        [SerializeField] private float _bounceAmount = 50f;
        
        [Header("Button Labels")]
        [SerializeField] private string _backButtonText = "Back";
        [SerializeField] private string _exitButtonText = "Exit";
        [SerializeField] private LocalizedString _backButtonLocalized;
        [SerializeField] private LocalizedString _exitButtonLocalized;
        
        [Header("Localization Settings")]
        [SerializeField] private string _localizationTableName = "Interface";

        private Dictionary<string, GUIPanel> _panels = new Dictionary<string, GUIPanel>();
        private Stack<GUIPanel> _panelHistory = new Stack<GUIPanel>();
        private GUIPanel _currentPanel = null;
        private EventSystem _eventSystem;
        private InputAction _cancelAction;

        private void Awake()
        {
            if (_panelsParent == null)
            {
                DebugHelper.LogError(this, "PanelsParent is not assigned, create default Canvas");
                _panelsParent = gameObject.AddComponent<Canvas>().transform;
            }
            _eventSystem = GetComponentInChildren<EventSystem>();
            if(_eventSystem == null)
            {
                DebugHelper.LogError(this, "Event system is not found");
            }

            // Setup Cancel action from Input System
            SetupCancelAction();
        }

        private void SetupCancelAction()
        {
            // Try to get Cancel action from InputSystemUIInputModule first
            var inputModule = _eventSystem?.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
            if (inputModule != null && inputModule.cancel != null)
            {
                _cancelAction = inputModule.cancel.action;
                if (_cancelAction != null)
                {
                    _cancelAction.performed += OnCancelPerformed;
                    _cancelAction.Enable();
                    return;
                }
            }
            
            // Fallback: Try to get Cancel action from Input System directly
            var inputActionsAsset = UnityEngine.InputSystem.InputSystem.actions;
            if (inputActionsAsset != null)
            {
                var uiMap = inputActionsAsset.FindActionMap("UI");
                if (uiMap != null)
                {
                    _cancelAction = uiMap.FindAction("Cancel");
                    if (_cancelAction != null)
                    {
                        _cancelAction.performed += OnCancelPerformed;
                        _cancelAction.Enable();
                        return;
                    }
                }
            }
            
            // Last resort: Create a new InputAction for Cancel
            _cancelAction = new InputAction("Cancel", InputActionType.Button);
            _cancelAction.AddBinding("<Keyboard>/escape");
            _cancelAction.AddBinding("<Gamepad>/buttonEast");
            _cancelAction.performed += OnCancelPerformed;
            _cancelAction.Enable();
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                GoBack();
            }
        }

        private void OnDestroy()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancelPerformed;
                _cancelAction.Disable();
            }
        }

        public PanelBuilder CreatePanel(string panelName, GameObject customPanel = null)
        {
            if (_panels.ContainsKey(panelName))
            {
                DebugHelper.LogWarning(this, $"Panel '{panelName}' already exists!");
                return new PanelBuilder(this, _panels[panelName]);
            }

            GameObject panelObj = Instantiate(customPanel is null ? _panelPrefab : customPanel, _panelsParent);
            GUIPanel panel = panelObj.GetComponent<GUIPanel>();
            
            if (panel == null)
            {
                panel = panelObj.AddComponent<GUIPanel>();
            }

            panel.SetPanelName(panelName);
            panel.gameObject.SetActive(false);
            _panels[panelName] = panel;
            
            panel.SetAnimationDuration(_animationDuration);
            panel.SetAnimationSettings(_panelAnimationType, _slideDirection, _slideDistance, _bounceAmount);

            return new PanelBuilder(this, panel);
        }

        public PanelBuilder CreatePanel(LocalizedString localizedPanelName, GameObject customPanel = null)
        {
            string panelKey = localizedPanelName.TableEntryReference.Key;
            if (_panels.ContainsKey(panelKey))
            {
                DebugHelper.LogWarning(this, $"Panel '{panelKey}' already exists!");
                return new PanelBuilder(this, _panels[panelKey]);
            }

            GameObject panelObj = Instantiate(customPanel is null ? _panelPrefab : customPanel, _panelsParent);
            GUIPanel panel = panelObj.GetComponent<GUIPanel>();
            
            if (panel == null)
            {
                panel = panelObj.AddComponent<GUIPanel>();
            }

            panel.SetPanelName(localizedPanelName);
            panel.gameObject.SetActive(false);
            _panels[panelKey] = panel;
            
            panel.SetAnimationDuration(_animationDuration);
            panel.SetAnimationSettings(_panelAnimationType, _slideDirection, _slideDistance, _bounceAmount);

            return new PanelBuilder(this, panel);
        }

        public void ChangePanel(string panelName)
        {
            if (!_panels.ContainsKey(panelName))
            {
                Debug.LogError($"Panel '{panelName}' does not exist!");
                return;
            }

            ChangePanel(_panels[panelName]);
        }

        public void ChangePanel(GUIPanel panel)
        {
            if (panel == null) return;
            
            // If panel is already current and visible, don't change
            if (panel == _currentPanel && panel.IsVisible && !panel.IsAnimating)
            {
                return; 
            }
            
            if (_currentPanel != null && _currentPanel.IsAnimating)
            {
                return; 
            }

            StartCoroutine(ChangePanelCoroutine(panel));
        }

        public void ClosePanel(GUIPanel panel)
        {
            if (panel == null) return;
            
            // If this is the current panel, clear it
            if (panel == _currentPanel)
            {
                _currentPanel = null;
            }
            
            panel.HidePanel();
        }

        private IEnumerator ChangePanelCoroutine(GUIPanel newPanel)
        {
            if (_currentPanel != null && _currentPanel != newPanel)
            {
                GUIPanel panelToHide = _currentPanel;
                _panelHistory.Push(panelToHide);
                panelToHide.HidePanel();
                
                // Wait for animation to complete using unscaled time
                float timeout = 5f; // Safety timeout
                float elapsed = 0f;
                while (panelToHide.IsAnimating && elapsed < timeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            _currentPanel = newPanel;
            newPanel?.ShowPanel();
            yield return null;
            _eventSystem.SetSelectedGameObject(newPanel?.GetFirstButton());
        }

        public void OnCancel(BaseEventData eventData)
        {
            GoBack();
        }

        public void GoBack()
        {
            if (_panelHistory.Count == 0)
            {
                DebugHelper.LogWarning(this, "No previous panel to go back to!");
                return;
            }
            
            if (_currentPanel != null && _currentPanel.IsAnimating)
            {
                return;
            }

            StartCoroutine(GoBackCoroutine());
        }

        private IEnumerator GoBackCoroutine()
        {
            if (_currentPanel != null)
            {
                _currentPanel.HidePanel();
                yield return new WaitUntil(() => !_currentPanel.IsAnimating);
            }

            _currentPanel = _panelHistory.Pop();
            _currentPanel.ShowPanel();
            yield return null;
            _eventSystem.SetSelectedGameObject(_currentPanel?.GetFirstButton());
        }

        internal InterfaceElement CreateButton(string text, Action onClick)
        {
            return CreateButtonInternal(text, onClick);
        }

        internal InterfaceElement CreateButton(LocalizedString localizedText, Action onClick)
        {
            return CreateButtonInternal(localizedText, onClick);
        }

        private InterfaceElement CreateButtonInternal(object textOrLocalized, Action onClick)
        {
            if (_buttonPrefab == null)
            {
                Debug.LogError("Button prefab is not assigned!");
                return null;
            }

            GameObject buttonObj = Instantiate(_buttonPrefab);
            GUIButton button = buttonObj.GetComponent<GUIButton>() ?? buttonObj.AddComponent<GUIButton>();

            if (textOrLocalized is LocalizedString localized)
            {
                button.Initialize(localized, onClick);
            }
            else
            {
                button.Initialize(textOrLocalized as string, onClick);
            }
            
            return button;
        }

        internal InterfaceElement CreateSlider(string label, Action<float> onValueChanged, Func<float> getCurrentValue = null, Func<float, string> valueFormatter = null)
        {
            return CreateSliderInternal(label, onValueChanged, getCurrentValue, valueFormatter);
        }

        internal InterfaceElement CreateSlider(LocalizedString localizedLabel, Action<float> onValueChanged, Func<float> getCurrentValue = null, Func<float, string> valueFormatter = null)
        {
            return CreateSliderInternal(localizedLabel, onValueChanged, getCurrentValue, valueFormatter);
        }

        private InterfaceElement CreateSliderInternal(object labelOrLocalized, Action<float> onValueChanged, Func<float> getCurrentValue, Func<float, string> valueFormatter)
        {
            if (_sliderPrefab == null)
            {
                Debug.LogError("Slider prefab is not assigned!");
                return null;
            }

            GameObject sliderObj = Instantiate(_sliderPrefab);
            GUISlider slider = sliderObj.GetComponent<GUISlider>() ?? sliderObj.AddComponent<GUISlider>();

            if (labelOrLocalized is LocalizedString localized)
            {
                slider.Initialize(localized, onValueChanged, getCurrentValue, valueFormatter);
            }
            else
            {
                slider.Initialize(labelOrLocalized as string, onValueChanged, getCurrentValue, valueFormatter);
            }
            
            return slider;
        }

        internal InterfaceElement CreateToggle(string label, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            return CreateToggleInternal(label, onValueChanged, getCurrentValue);
        }

        internal InterfaceElement CreateToggle(LocalizedString localizedLabel, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            return CreateToggleInternal(localizedLabel, onValueChanged, getCurrentValue);
        }

        private InterfaceElement CreateToggleInternal(object labelOrLocalized, Action<bool> onValueChanged, Func<bool> getCurrentValue)
        {
            if (_togglePrefab == null)
            {
                Debug.LogError("Toggle prefab is not assigned!");
                return null;
            }

            GameObject toggleObj = Instantiate(_togglePrefab);
            GUIToggle toggle = toggleObj.GetComponent<GUIToggle>() ?? toggleObj.AddComponent<GUIToggle>();

            if (labelOrLocalized is LocalizedString localized)
            {
                toggle.Initialize(localized, onValueChanged, getCurrentValue);
            }
            else
            {
                toggle.Initialize(labelOrLocalized as string, onValueChanged, getCurrentValue);
            }
            
            return toggle;
        }

        internal InterfaceElement CreateDropdown(string label, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            return CreateDropdownInternal(label, onValueChanged, options, getCurrentValue);
        }

        internal InterfaceElement CreateDropdown(LocalizedString localizedLabel, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            return CreateDropdownInternal(localizedLabel, onValueChanged, options, getCurrentValue);
        }

        internal InterfaceElement CreateDropdown(LocalizedString localizedLabel, Action<int> onValueChanged, List<LocalizedString> localizedOptions, Func<int> getCurrentValue = null)
        {
            if (_dropdownPrefab == null)
            {
                Debug.LogError("Dropdown prefab is not assigned!");
                return null;
            }

            GameObject dropdownObj = Instantiate(_dropdownPrefab);
            GUIDropdown dropdown = dropdownObj.GetComponent<GUIDropdown>() ?? dropdownObj.AddComponent<GUIDropdown>();
            dropdown.Initialize(localizedLabel, onValueChanged, localizedOptions, getCurrentValue);
            return dropdown;
        }

        private InterfaceElement CreateDropdownInternal(object labelOrLocalized, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue)
        {
            if (_dropdownPrefab == null)
            {
                Debug.LogError("Dropdown prefab is not assigned!");
                return null;
            }

            GameObject dropdownObj = Instantiate(_dropdownPrefab);
            GUIDropdown dropdown = dropdownObj.GetComponent<GUIDropdown>() ?? dropdownObj.AddComponent<GUIDropdown>();

            if (labelOrLocalized is LocalizedString localized)
            {
                dropdown.Initialize(localized, onValueChanged, options, getCurrentValue);
            }
            else
            {
                dropdown.Initialize(labelOrLocalized as string, onValueChanged, options, getCurrentValue);
            }
            
            return dropdown;
        }

        internal InterfaceElement CreateToolTip(LocalizedString localizedString)
        {
            if (_toolTipPrefab == null)
            {
                Debug.LogError("Tooltip prefab is not assigned!");
                return null;
            }

            GameObject toolTipObj = Instantiate(_toolTipPrefab);
            GUIToolTip toolTip = toolTipObj.GetComponent<GUIToolTip>() ?? toolTipObj.AddComponent<GUIToolTip>();
            toolTip.Initialize(localizedString);
            return toolTip;
        }

        internal InterfaceElement CreateLabel(string text)
        {
            return CreateLabelInternal(text);
        }

        internal InterfaceElement CreateLabel(LocalizedString localizedText)
        {
            return CreateLabelInternal(localizedText);
        }

        internal InterfaceElement CreateSpacer(float height = 20f)
        {
            GameObject spacerObj = new GameObject("GUISpacer");
            GUISpacer spacer = spacerObj.AddComponent<GUISpacer>();
            spacer.Initialize(height);
            return spacer;
        }

        private InterfaceElement CreateLabelInternal(object textOrLocalized)
        {
            if (_labelPrefab == null)
            {
                Debug.LogError("Label prefab is not assigned!");
                return null;
            }

            GameObject labelObj = Instantiate(_labelPrefab);
            GUILabel label = labelObj.GetComponent<GUILabel>() ?? labelObj.AddComponent<GUILabel>();

            if (textOrLocalized is LocalizedString localized)
            {
                label.Initialize(localized);
            }
            else
            {
                label.Initialize(textOrLocalized as string);
            }
            
            return label;
        }

        internal GUIKeyBinder CreateKeyBinder(LocalizedString localizedLabel, InputAction action, int bindingIndex, string bindingGroup = null)
        {
            if (_keyBinderPrefab == null)
            {
                Debug.LogError("Key binder prefab is not assigned!");
                return null;
            }

            GameObject binderObj = Instantiate(_keyBinderPrefab);
            GUIKeyBinder binder = binderObj.GetComponent<GUIKeyBinder>() ?? binderObj.AddComponent<GUIKeyBinder>();

            if (localizedLabel.IsEmpty)
            {
                binder.Initialize(action?.name ?? "Action", action, bindingIndex, bindingGroup);
            }
            else
            {
                binder.Initialize(localizedLabel, action, bindingIndex, bindingGroup);
            }
            return binder;
        }

        public void ExitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        /// <summary>
        /// Helper method that returns an Action for changing to a panel.
        /// Can be used like: .AddButton("Play", GetChangePanelAction(playPanel))
        /// </summary>
        public Action GetChangePanelAction(GUIPanel panel)
        {
            return () => ChangePanel(panel);
        }

        /// <summary>
        /// Returns the Back button text (configurable from inspector)
        /// </summary>
        public string GetBackButtonText()
        {
            if (_backButtonLocalized != null && _backButtonLocalized.IsEmpty == false)
            {
                var op = _backButtonLocalized.GetLocalizedStringAsync();
                if (op.IsDone && op.Result != null)
                {
                    return op.Result;
                }
            }
            return string.IsNullOrEmpty(_backButtonText) ? "Back" : _backButtonText;
        }

        /// <summary>
        /// Returns the Exit button text (configurable from inspector)
        /// </summary>
        public string GetExitButtonText()
        {
            if (_exitButtonLocalized != null && _exitButtonLocalized.IsEmpty == false)
            {
                var op = _exitButtonLocalized.GetLocalizedStringAsync();
                if (op.IsDone && op.Result != null)
                {
                    return op.Result;
                }
            }
            return string.IsNullOrEmpty(_exitButtonText) ? "Exit" : _exitButtonText;
        }

        /// <summary>
        /// Returns the localized Back button text
        /// </summary>
        public LocalizedString GetBackButtonLocalized()
        {
            return _backButtonLocalized;
        }

        /// <summary>
        /// Returns the localized Exit button text
        /// </summary>
        public LocalizedString GetExitButtonLocalized()
        {
            return _exitButtonLocalized;
        }

        /// <summary>
        /// Creates a LocalizedString from a localization key
        /// </summary>
        public LocalizedString GetLocalizedString(string key)
        {
            var localizedString = new LocalizedString();
            localizedString.TableReference = _localizationTableName;
            localizedString.TableEntryReference = key;
            return localizedString;
        }

        /// <summary>
        /// Creates a custom element from a prefab and initializes it with an optional argument
        /// </summary>
        /// <param name="prefab">Prefab GameObject containing the custom element component</param>
        /// <param name="argument">Optional argument to pass to the custom element's InitializeWithArgument method</param>
        /// <returns>The created InterfaceElement, or null if prefab is invalid</returns>
        internal InterfaceElement CreateCustomElement(GameObject prefab, object argument = null)
        {
            if (prefab == null)
            {
                Debug.LogError("Custom element prefab is not assigned!");
                return null;
            }

            GameObject elementObj = Instantiate(prefab);
            InterfaceElement element = elementObj.GetComponent<InterfaceElement>();
            
            if (element == null)
            {
                Debug.LogError($"Custom element prefab '{prefab.name}' does not contain an InterfaceElement component!");
                Destroy(elementObj);
                return null;
            }

            // Initialize with argument if provided
            if (argument != null)
            {
                element.InitializeWithArgument(argument);
            }

            return element;
        }
    }
}

