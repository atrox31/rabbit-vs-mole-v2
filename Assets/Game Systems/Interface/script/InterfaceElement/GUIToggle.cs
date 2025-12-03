using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

namespace Interface.Element
{
    public class GUIToggle : LocalizedElementBase
    {
        [Header("Toggle References")]
        [SerializeField] private UnityEngine.UI.Toggle _toggle;
        [SerializeField] private TextMeshProUGUI _labelTextOverride;

        private Action<bool> _onValueChanged;
        private Func<bool> _getCurrentValue;

        protected override void Setup()
        {
            base.Setup();

            if (_toggle == null)
                _toggle = GetComponentInChildren<UnityEngine.UI.Toggle>();

            if (_labelTextOverride != null)
            {
                _labelText = _labelTextOverride;
            }

            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveAllListeners();
                _toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        public void Initialize(string label, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;
            ApplyText(label);
            SetupToggle();
            Setup();
        }

        public void Initialize(LocalizedString localizedLabel, Action<bool> onValueChanged, Func<bool> getCurrentValue = null)
        {
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;
            ApplyLocalizedText(localizedLabel);
            SetupToggle();
            Setup();
        }

        private void SetupToggle()
        {
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
    }
}

