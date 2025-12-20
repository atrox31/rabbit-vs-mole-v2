using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace Interface.Element
{
    public class GUIDropdown : LocalizedElementBase
    {
        [Header("Dropdown References")]
        [SerializeField] private TMPro.TMP_Dropdown _dropdown;
        [SerializeField] private TextMeshProUGUI _labelTextOverride;

        [Header("Sound")]
        [SerializeField] private AudioClip _onClickSound;

        private Action<int> _onValueChanged;
        private Func<int> _getCurrentValue;
        private List<LocalizedString> _localizedOptions;

        protected override void Setup()
        {
            base.Setup();

            if (_dropdown == null)
                _dropdown = GetComponentInChildren<TMPro.TMP_Dropdown>();

            if (_labelTextOverride != null)
            {
                _labelText = _labelTextOverride;
            }

            if (_dropdown != null)
            {
                _dropdown.onValueChanged.RemoveAllListeners();
                _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
            AudioManager.PreloadClips(_onClickSound);
        }

        public void Initialize(string label, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;
            ApplyText(label);
            SetupDropdown(options);
            Setup();
        }

        public void Initialize(LocalizedString localizedLabel, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
        {
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;
            ApplyLocalizedText(localizedLabel);
            SetupDropdown(options);
            Setup();
        }

        public void Initialize(LocalizedString localizedLabel, Action<int> onValueChanged, List<LocalizedString> localizedOptions, Func<int> getCurrentValue = null)
        {
            _localizedOptions = localizedOptions;
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;
            ApplyLocalizedText(localizedLabel);
            Setup();
            
            if (_dropdown != null)
            {
                _dropdown.ClearOptions();
                if (localizedOptions != null && localizedOptions.Count > 0)
                {
                    LoadLocalizedOptions(localizedOptions, getCurrentValue);
                }
            }
        }

        private void SetupDropdown(List<string> options)
        {
            if (_dropdown != null)
            {
                _dropdown.ClearOptions();
                _dropdown.AddOptions(options);

                if (_getCurrentValue != null)
                {
                    int currentIndex = _getCurrentValue();
                    if (currentIndex >= 0 && currentIndex < options.Count)
                    {
                        _dropdown.value = currentIndex;
                    }
                }
            }
        }

        private void LoadLocalizedOptions(List<LocalizedString> localizedOptions, Func<int> getCurrentValue)
        {
            List<string> optionStrings = new List<string>();
            int loadedCount = 0;
            int totalCount = localizedOptions.Count;

            for (int i = 0; i < localizedOptions.Count; i++)
            {
                int index = i;
                optionStrings.Add(""); // Placeholder
                
                localizedOptions[i].GetLocalizedStringAsync().Completed += (op) =>
                {
                    if (op.IsDone && op.Result != null)
                    {
                        optionStrings[index] = op.Result;
                        loadedCount++;

                        if (loadedCount == totalCount)
                        {
                            _dropdown.AddOptions(optionStrings);
                            
                            if (getCurrentValue != null)
                            {
                                int currentIndex = getCurrentValue();
                                if (currentIndex >= 0 && currentIndex < optionStrings.Count)
                                {
                                    _dropdown.value = currentIndex;
                                }
                            }
                        }
                    }
                };
            }
        }

        private void OnDropdownValueChanged(int value)
        {
            if (!_isReady)
                return;

            _onValueChanged?.Invoke(value);
            AudioManager.PlaySoundUI(_onClickSound);
        }

        public void SetValue(int value)
        {
            if (_dropdown != null && value >= 0 && value < _dropdown.options.Count)
            {
                _dropdown.value = value;
            }
        }

        public int GetValue()
        {
            return _dropdown != null ? _dropdown.value : 0;
        }
    }
}

