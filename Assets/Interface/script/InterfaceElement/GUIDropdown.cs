using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Interface.Element
{
    public class GUIDropdown : InterfaceElement
    {
        [Header("Dropdown References")]
        [SerializeField] private TMPro.TMP_Dropdown _dropdown;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private LocalizeStringEvent _localizeLabelEvent;

        private Action<int> _onValueChanged;
        private Func<int> _getCurrentValue;
        private LocalizedString _localizedLabel;
        private List<LocalizedString> _localizedOptions;

        protected override void Setup()
        {
            base.Setup();

            if (_dropdown == null)
                _dropdown = GetComponentInChildren<TMPro.TMP_Dropdown>();

            if (_dropdown != null)
            {
                _dropdown.onValueChanged.RemoveAllListeners();
                _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
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

        public void Initialize(string label, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
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

            Setup();
        }

        public void Initialize(LocalizedString localizedLabel, Action<int> onValueChanged, List<string> options, Func<int> getCurrentValue = null)
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

        public void Initialize(LocalizedString localizedLabel, Action<int> onValueChanged, List<LocalizedString> localizedOptions, Func<int> getCurrentValue = null)
        {
            _localizedLabel = localizedLabel;
            _localizedOptions = localizedOptions;
            _onValueChanged = onValueChanged;
            _getCurrentValue = getCurrentValue;

            // Setup first to ensure LocalizeStringEvent is available
            Setup();

            if (_labelText != null)
            {
                if (_localizeLabelEvent != null)
                {
                    _localizeLabelEvent.StringReference = localizedLabel;
                }
                else
                {
                    localizedLabel.GetLocalizedStringAsync().Completed += (op) =>
                    {
                        if (op.IsDone && op.Result != null && _labelText != null)
                        {
                            _labelText.text = op.Result;
                        }
                    };
                }
            }

            if (_dropdown != null)
            {
                _dropdown.ClearOptions();
                
                // Load localized options asynchronously
                if (localizedOptions != null && localizedOptions.Count > 0)
                {
                    LoadLocalizedOptions(localizedOptions, getCurrentValue);
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
            _onValueChanged?.Invoke(value);
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

        private void OnDestroy()
        {
            if (_localizeLabelEvent != null)
            {
                _localizeLabelEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }
        }
    }
}

