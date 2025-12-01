using System;
using System.Collections;
using System.Collections.Generic;
using Interface.Element;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem;

namespace Interface
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _panelPrefab;
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private GameObject _sliderPrefab;
        [SerializeField] private GameObject _togglePrefab;
        [SerializeField] private GameObject _dropdownPrefab;
        [SerializeField] private GameObject _keyBinderPrefab;
        [SerializeField] private GameObject _toolTipPrefab;

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

        private void Awake()
        {
            if (_panelsParent == null)
            {
                _panelsParent = transform;
            }
        }

        public PanelBuilder CreatePanel(string panelName)
        {
            if (_panels.ContainsKey(panelName))
            {
                Debug.LogWarning($"Panel '{panelName}' already exists!");
                return new PanelBuilder(this, _panels[panelName]);
            }

            GameObject panelObj = Instantiate(_panelPrefab, _panelsParent);
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

        public PanelBuilder CreatePanel(LocalizedString localizedPanelName)
        {
            string panelKey = localizedPanelName.TableEntryReference.Key;
            if (_panels.ContainsKey(panelKey))
            {
                Debug.LogWarning($"Panel '{panelKey}' already exists!");
                return new PanelBuilder(this, _panels[panelKey]);
            }

            GameObject panelObj = Instantiate(_panelPrefab, _panelsParent);
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
            if (panel == _currentPanel) return; 
            
            if (_currentPanel != null && _currentPanel.IsAnimating)
            {
                return; 
            }

            StartCoroutine(ChangePanelCoroutine(panel));
        }

        private IEnumerator ChangePanelCoroutine(GUIPanel newPanel)
        {
            if (_currentPanel != null)
            {
                _panelHistory.Push(_currentPanel);
                _currentPanel.HidePanel();
                
                yield return new WaitUntil(() => !_currentPanel.IsAnimating);
            }

            _currentPanel = newPanel;
            newPanel.ShowPanel();
        }

        public void GoBack()
        {
            if (_panelHistory.Count == 0)
            {
                Debug.LogWarning("No previous panel to go back to!");
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
        }

        internal InterfaceElement CreateButton(string text, Action onClick)
        {
            if (_buttonPrefab == null)
            {
                Debug.LogError("Button prefab is not assigned!");
                return null;
            }

            GameObject buttonObj = Instantiate(_buttonPrefab);
            GUIButton button = buttonObj.GetComponent<GUIButton>();
            
            if (button == null)
            {
                button = buttonObj.AddComponent<GUIButton>();
            }

            button.Initialize(text, onClick);
            return button;
        }

        internal InterfaceElement CreateButton(LocalizedString localizedText, Action onClick)
        {
            if (_buttonPrefab == null)
            {
                Debug.LogError("Button prefab is not assigned!");
                return null;
            }

            GameObject buttonObj = Instantiate(_buttonPrefab);
            GUIButton button = buttonObj.GetComponent<GUIButton>();
            
            if (button == null)
            {
                button = buttonObj.AddComponent<GUIButton>();
            }

            button.Initialize(localizedText, onClick);
            return button;
        }

        internal InterfaceElement CreateSlider(string label, Action<float> onValueChanged, Func<float> getCurrentValue = null)
        {
            if (_sliderPrefab == null)
            {
                Debug.LogError("Slider prefab is not assigned!");
                return null;
            }

            GameObject sliderObj = Instantiate(_sliderPrefab);
            GUISlider slider = sliderObj.GetComponent<GUISlider>();
            
            if (slider == null)
            {
                slider = sliderObj.AddComponent<GUISlider>();
            }

            slider.Initialize(label, onValueChanged, getCurrentValue);
            return slider;
        }

        internal InterfaceElement CreateSlider(LocalizedString localizedLabel, Action<float> onValueChanged, Func<float> getCurrentValue = null)
        {
            if (_sliderPrefab == null)
            {
                Debug.LogError("Slider prefab is not assigned!");
                return null;
            }

            GameObject sliderObj = Instantiate(_sliderPrefab);
            GUISlider slider = sliderObj.GetComponent<GUISlider>();
            
            if (slider == null)
            {
                slider = sliderObj.AddComponent<GUISlider>();
            }

            slider.Initialize(localizedLabel, onValueChanged, getCurrentValue);
            return slider;
        }

        internal InterfaceElement CreateToggle(string label, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            if (_togglePrefab == null)
            {
                Debug.LogError("Toggle prefab is not assigned!");
                return null;
            }

            GameObject toggleObj = Instantiate(_togglePrefab);
            GUIToggle toggle = toggleObj.GetComponent<GUIToggle>();
            
            if (toggle == null)
            {
                toggle = toggleObj.AddComponent<GUIToggle>();
            }

            toggle.Initialize(label, onValueChanged, getCurrentValue);
            return toggle;
        }

        internal InterfaceElement CreateToggle(LocalizedString localizedLabel, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            if (_togglePrefab == null)
            {
                Debug.LogError("Toggle prefab is not assigned!");
                return null;
            }

            GameObject toggleObj = Instantiate(_togglePrefab);
            GUIToggle toggle = toggleObj.GetComponent<GUIToggle>();
            
            if (toggle == null)
            {
                toggle = toggleObj.AddComponent<GUIToggle>();
            }

            toggle.Initialize(localizedLabel, onValueChanged, getCurrentValue);
            return toggle;
        }

        internal InterfaceElement CreateDropdown(string label, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            if (_dropdownPrefab == null)
            {
                Debug.LogError("Dropdown prefab is not assigned!");
                return null;
            }

            GameObject dropdownObj = Instantiate(_dropdownPrefab);
            GUIDropdown dropdown = dropdownObj.GetComponent<GUIDropdown>();
            
            if (dropdown == null)
            {
                dropdown = dropdownObj.AddComponent<GUIDropdown>();
            }

            dropdown.Initialize(label, onValueChanged, options, getCurrentValue);
            return dropdown;
        }

        internal InterfaceElement CreateDropdown(LocalizedString localizedLabel, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            if (_dropdownPrefab == null)
            {
                Debug.LogError("Dropdown prefab is not assigned!");
                return null;
            }

            GameObject dropdownObj = Instantiate(_dropdownPrefab);
            GUIDropdown dropdown = dropdownObj.GetComponent<GUIDropdown>();
            
            if (dropdown == null)
            {
                dropdown = dropdownObj.AddComponent<GUIDropdown>();
            }

            dropdown.Initialize(localizedLabel, onValueChanged, options, getCurrentValue);
            return dropdown;
        }

        internal InterfaceElement CreateDropdown(LocalizedString localizedLabel, Action<int> onValueChanged, List<LocalizedString> localizedOptions, Func<int> getCurrentValue = null)
        {
            if (_dropdownPrefab == null)
            {
                Debug.LogError("Dropdown prefab is not assigned!");
                return null;
            }

            GameObject dropdownObj = Instantiate(_dropdownPrefab);
            GUIDropdown dropdown = dropdownObj.GetComponent<GUIDropdown>();
            
            if (dropdown == null)
            {
                dropdown = dropdownObj.AddComponent<GUIDropdown>();
            }

            dropdown.Initialize(localizedLabel, onValueChanged, localizedOptions, getCurrentValue);
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
            GUIToolTip toolTip = toolTipObj.GetComponent<GUIToolTip>();

            if (toolTip == null)
            {
                toolTip = toolTipObj.AddComponent<GUIToolTip>();
            }

            toolTip.Initialize(localizedString);
            return toolTip;
        }

        internal GUIKeyBinder CreateKeyBinder(LocalizedString localizedLabel, InputAction action, int bindingIndex, string bindingGroup = null)
        {
            if (_keyBinderPrefab == null)
            {
                Debug.LogError("Key binder prefab is not assigned!");
                return null;
            }

            GameObject binderObj = Instantiate(_keyBinderPrefab);
            GUIKeyBinder binder = binderObj.GetComponent<GUIKeyBinder>();

            if (binder == null)
            {
                binder = binderObj.AddComponent<GUIKeyBinder>();
            }

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
        /// Zwraca tekst przycisku Back (konfigurowalny z inspektora)
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
        /// Zwraca tekst przycisku Exit (konfigurowalny z inspektora)
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
        /// Zwraca zlokalizowany tekst przycisku Back
        /// </summary>
        public LocalizedString GetBackButtonLocalized()
        {
            return _backButtonLocalized;
        }

        /// <summary>
        /// Zwraca zlokalizowany tekst przycisku Exit
        /// </summary>
        public LocalizedString GetExitButtonLocalized()
        {
            return _exitButtonLocalized;
        }

        /// <summary>
        /// Tworzy LocalizedString z klucza lokalizacji
        /// </summary>
        public LocalizedString GetLocalizedString(string key)
        {
            var localizedString = new LocalizedString();
            localizedString.TableReference = _localizationTableName;
            localizedString.TableEntryReference = key;
            return localizedString;
        }
    }
}

