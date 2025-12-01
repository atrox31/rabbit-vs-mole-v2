using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Interface.Element
{
    public class GUIToggle : InterfaceElement
    {
        [Header("Toggle References")]
        [SerializeField] private UnityEngine.UI.Toggle _toggle;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private LocalizeStringEvent _localizeLabelEvent;

        private Action<bool> _onValueChanged;
        private Func<bool> _getCurrentValue;
        private LocalizedString _localizedLabel;

        protected override void Setup()
        {
            base.Setup();

            if (_toggle == null)
                _toggle = GetComponentInChildren<UnityEngine.UI.Toggle>();

            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveAllListeners();
                _toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }

            // Setup LocalizeStringEvent if available
            if (_localizeLabelEvent == null && _labelText != null)
            {
                _localizeLabelEvent = _labelText.GetComponent<LocalizeStringEvent>();
                if (_localizeLabelEvent == null)
                {
                    _localizeLabelEvent = _labelText.gameObject.AddComponent<LocalizeStringEvent>();
                }
                
                // Subscribe to update event to ensure it works
                if (_localizeLabelEvent != null)
                {
                    _localizeLabelEvent.OnUpdateString.AddListener(OnLocalizedStringUpdate);
                }
            }
        }

        private void OnLocalizedStringUpdate(string localizedString)
        {
            if (_labelText != null)
            {
                _labelText.text = localizedString;
            }
        }

        public void Initialize(string label, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;

            if (_labelText != null)
            {
                // Try to use localization if available, otherwise use plain text
                if (_localizeLabelEvent != null && _localizedLabel != null)
                {
                    _localizeLabelEvent.StringReference = _localizedLabel;
                }
                else
                {
                    _labelText.text = label;
                }
            }

            if (_toggle != null && _getCurrentValue != null)
            {
                _toggle.isOn = _getCurrentValue();
            }

            Setup();
        }

        public void Initialize(LocalizedString localizedLabel, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            _localizedLabel = localizedLabel;
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;

            // Setup first to ensure LocalizeStringEvent is available
            Setup();

            if (_labelText != null)
            {
                if (_localizeLabelEvent != null)
                {
                    _localizeLabelEvent.StringReference = localizedLabel;
                    _localizeLabelEvent.RefreshString();
                }
                else
                {
                    // Fallback to async loading if LocalizeStringEvent not available
                    localizedLabel.GetLocalizedStringAsync().Completed += (op) =>
                    {
                        if (op.IsDone && op.Result != null && _labelText != null)
                        {
                            _labelText.text = op.Result;
                        }
                    };
                }
            }

            if (_toggle != null && _getCurrentValue != null)
            {
                _toggle.isOn = _getCurrentValue();
            }
        }

        private void OnToggleValueChanged(bool value)
        {
            _onValueChanged?.Invoke(value);
        }

        public void SetValue(bool value)
        {
            if (_toggle != null)
            {
                _toggle.isOn = value;
            }
        }

        public bool GetValue()
        {
            return _toggle != null ? _toggle.isOn : false;
        }

        private void OnDestroy()
        {
            if (_localizeLabelEvent != null)
            {
                _localizeLabelEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }
        }
    }
}

