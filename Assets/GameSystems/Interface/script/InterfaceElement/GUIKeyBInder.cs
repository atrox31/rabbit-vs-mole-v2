using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Interface.Element
{
    public class GUIKeyBinder : InterfaceElement
    {
        [Header("Key Binder References")]
        [SerializeField] private TMP_Text _keyText;
        [SerializeField] private Button _keyButton;
        [SerializeField] private TMP_Text _bindingText;
        [SerializeField] private LocalizeStringEvent _keyLocalizeEvent;

        [Header("Localized string settings")]
        [SerializeField] private bool _useLocalizedString = true;

        private LocalizedString _localizedLabel;
        private InputAction _action;
        private int _bindingIndex = -1;
        private string _bindingGroup;
        private Action<InputAction, int, string> _onKeyChanged;
        private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
        private string _fallbackLabel;
        private bool _isListening;

        protected override void Setup()
        {
            base.Setup();

            if (_keyButton == null)
            {
                _keyButton = GetComponentInChildren<Button>(true);
            }

            if (_bindingText == null && _keyButton != null)
            {
                _bindingText = _keyButton.GetComponentInChildren<TMP_Text>();
            }

            SetupLocalization();
            AttachButtonHandler();
            UpdateBindingDisplay();
        }

        private void SetupLocalization()
        {
            if (_keyText == null)
                return;

            if (_useLocalizedString)
            {
                // Always find or create LocalizeStringEvent to ensure each instance has its own
                if (_keyLocalizeEvent == null)
                {
                    _keyLocalizeEvent = _keyText.GetComponent<LocalizeStringEvent>();
                    if (_keyLocalizeEvent == null)
                    {
                        _keyLocalizeEvent = _keyText.gameObject.AddComponent<LocalizeStringEvent>();
                    }
                }

                // Always set up listener if not already set
                if (_keyLocalizeEvent != null)
                {
                    _keyLocalizeEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
                    _keyLocalizeEvent.OnUpdateString.AddListener(OnLocalizedStringUpdate);
                    
                    // Always update StringReference if localizedLabel is set
                    if (_localizedLabel != null && !_localizedLabel.IsEmpty)
                    {
                       _keyLocalizeEvent.StringReference = _localizedLabel;
                        _keyLocalizeEvent.RefreshString();
                    }
                }
            }
            else if (!string.IsNullOrEmpty(_fallbackLabel))
            {
                // Clear localization if using fallback
                if (_keyLocalizeEvent != null)
                {
                    _keyLocalizeEvent.StringReference = null;
                }
                _keyText.text = _fallbackLabel;
            }
        }

        private void AttachButtonHandler()
        {
            if (_keyButton == null)
                return;

            _keyButton.onClick.RemoveListener(OnKeyButtonPressed);
            _keyButton.onClick.AddListener(OnKeyButtonPressed);
        }

        private void OnLocalizedStringUpdate(string localizedString)
        {
            if (_keyText != null)
            {
                _keyText.text = localizedString;
            }
        }

        public void Initialize(LocalizedString key, InputAction action, int bindingIndex, string bindingGroup = null, Action<InputAction, int, string> onKeyChanged = null)
        {
            _localizedLabel = key;
            _action = action;
            _bindingIndex = bindingIndex;
            _bindingGroup = bindingGroup;
            _onKeyChanged = onKeyChanged ?? InputBindingManager.HandleBindingChanged;
            
            // Ensure localization is set up after label is assigned
            Setup();
            SetupLocalization(); // Force update after Initialize
        }

        public void Initialize(string key, InputAction action, int bindingIndex, string bindingGroup = null, Action<InputAction, int, string> onKeyChanged = null)
        {
            _useLocalizedString = false;
            _fallbackLabel = key;
            _action = action;
            _bindingIndex = bindingIndex;
            _bindingGroup = bindingGroup;
            _onKeyChanged = onKeyChanged ?? InputBindingManager.HandleBindingChanged;
            
            // Ensure localization is set up after label is assigned
            Setup();
            SetupLocalization(); // Force update after Initialize
        }

        private void OnKeyButtonPressed()
        {
            if (_isListening || _action == null || _bindingIndex < 0)
                return;

            BeginRebind();
        }

        private void BeginRebind()
        {
            _isListening = true;

            if (_bindingText != null)
            {
                _bindingText.text = "...";
            }

            if (_keyButton != null)
            {
                _keyButton.interactable = false;
            }

            _rebindingOperation = InputBindingManager.BeginRebind(_action, _bindingIndex, OnRebindFinished, OnRebindCanceled, 0.1f, _bindingGroup);
            if (_rebindingOperation == null)
            {
                EndListeningState();
            }
        }

        private void OnRebindFinished()
        {
            EndListeningState();
            UpdateBindingDisplay();

            string spriteName = InputBindingManager.GetBindingSpriteName(_action, _bindingIndex);
            _onKeyChanged?.Invoke(_action, _bindingIndex, spriteName);

            var eventSystem = EventSystem.current;
            eventSystem?.SetSelectedGameObject(_keyButton.gameObject);
        }

        private void OnRebindCanceled()
        {
            EndListeningState();
            UpdateBindingDisplay();
        }

        private void EndListeningState()
        {
            _isListening = false;

            if (_keyButton != null)
            {
                _keyButton.interactable = true;
            }
        }

        private void UpdateBindingDisplay()
        {
            if (_bindingText == null || _action == null || _bindingIndex < 0)
                return;

            _bindingText.text = InputBindingManager.GetBindingSpriteTag(_action, _bindingIndex);
        }

        private void OnDisable()
        {
            CancelRebindOperation();
        }

        private void OnDestroy()
        {
            CancelRebindOperation();

            if (_keyLocalizeEvent != null)
            {
                _keyLocalizeEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }

            if (_keyButton != null)
            {
                _keyButton.onClick.RemoveListener(OnKeyButtonPressed);
            }
        }

        private void CancelRebindOperation()
        {
            if (_rebindingOperation != null)
            {
                _rebindingOperation.Cancel();
                _rebindingOperation.Dispose();
                _rebindingOperation = null;
            }
        }
    }
}