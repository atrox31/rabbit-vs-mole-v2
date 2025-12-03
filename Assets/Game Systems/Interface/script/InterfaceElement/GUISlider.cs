using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

namespace Interface.Element
{
    public class GUISlider : LocalizedElementBase
    {
        [Header("Slider References")]
        [SerializeField] private UnityEngine.UI.Slider _slider;
        [SerializeField] private TextMeshProUGUI _labelTextOverride;
        [SerializeField] private TextMeshProUGUI _valueText;

        private Action<float> _onValueChanged;
        private Func<float> _getCurrentValue;

        protected override void Setup()
        {
            base.Setup();

            if (_slider == null)
                _slider = GetComponentInChildren<UnityEngine.UI.Slider>();

            if (_labelTextOverride != null)
            {
                _labelText = _labelTextOverride;
            }

            if (_slider != null)
            {
                _slider.onValueChanged.RemoveAllListeners();
                _slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        public void Initialize(string label, Action<float> onValueChanged, Func<float> getCurrentValue = null)
        {
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;
            ApplyText(label);
            SetupSlider();
            Setup();
        }

        public void Initialize(LocalizedString localizedLabel, Action<float> onValueChanged, Func<float> getCurrentValue = null)
        {
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;
            ApplyLocalizedText(localizedLabel);
            SetupSlider();
            Setup();
        }

        private void SetupSlider()
        {
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
        /// Fixes RectTransform settings after adding to panel
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
    }
}

