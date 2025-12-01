using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Interface.Element
{
    public class GUISlider : InterfaceElement
    {
        [Header("Slider References")]
        [SerializeField] private UnityEngine.UI.Slider _slider;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private LocalizeStringEvent _localizeLabelEvent;

        private Action<float> _onValueChanged;
        private Func<float> _getCurrentValue;
        private LocalizedString _localizedLabel;

        protected override void Setup()
        {
            base.Setup();

            if (_slider == null)
                _slider = GetComponentInChildren<UnityEngine.UI.Slider>();

            if (_slider != null)
            {
                _slider.onValueChanged.RemoveAllListeners();
                _slider.onValueChanged.AddListener(OnSliderValueChanged);
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

        public void Initialize(string label, Action<float> onValueChanged, Func<float> getCurrentValue = null)
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

            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = 1f;
                _slider.wholeNumbers = false;

                if (_getCurrentValue != null)
                {
                    _slider.value = _getCurrentValue();
                }
            }

            UpdateValueText();
            Setup();
        }

        public void Initialize(LocalizedString localizedLabel, Action<float> onValueChanged, Func<float> getCurrentValue = null)
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

            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = 1f;
                _slider.wholeNumbers = false;

                if (_getCurrentValue != null)
                {
                    _slider.value = _getCurrentValue();
                }
            }

            UpdateValueText();
        }

        /// <summary>
        /// Naprawia ustawienia RectTransform slidera po dodaniu do panelu
        /// </summary>
        public void FixSliderLayout()
        {
            if (_slider == null) return;

            RectTransform sliderRect = _slider.GetComponent<RectTransform>();
            if (sliderRect != null)
            {
                sliderRect.anchorMin = new Vector2(0f, 0.5f);
                sliderRect.anchorMax = new Vector2(1f, 0.5f);
                sliderRect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private void OnSliderValueChanged(float value)
        {
            UpdateValueText();
            _onValueChanged?.Invoke(value);
        }

        private void UpdateValueText()
        {
            if (_valueText != null && _slider != null)
            {
                _valueText.text = Mathf.RoundToInt(_slider.value * 100f).ToString() + "%";
            }
        }

        public void SetValue(float value)
        {
            if (_slider != null)
            {
                _slider.value = value;
                UpdateValueText();
            }
        }

        public float GetValue()
        {
            return _slider != null ? _slider.value : 0f;
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

