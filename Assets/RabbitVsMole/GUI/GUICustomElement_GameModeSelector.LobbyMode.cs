using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using System;

namespace RabbitVsMole
{
    public partial class GUICustomElement_GameModeSelector
    {
        private List<string> _lobbyEntries = new();

        /// <summary>
        /// Switches the selector to "lobby mode" where the list shows plain strings (player nick + role),
        /// instead of GameModeData.
        /// </summary>
        public void SetLobbyMode(List<string> entries)
        {
            _lobbyEntries = entries ?? new List<string>();
            CreateLobbyButtons();
        }

        /// <summary>
        /// Sets center texts directly (disables localization events for these two fields).
        /// </summary>
        public void SetCenterOverride(string description, string configuration)
        {
            if (_gameModeDescriptionText != null)
            {
                var le = _gameModeDescriptionText.GetComponent<LocalizeStringEvent>();
                if (le != null)
                {
                    le.StringReference = null;
                    le.OnUpdateString.RemoveAllListeners();
                }
                _gameModeDescriptionText.text = description ?? string.Empty;
            }

            if (_gameModeConfigurationText != null)
            {
                var le = _gameModeConfigurationText.GetComponent<LocalizeStringEvent>();
                if (le != null)
                {
                    le.StringReference = null;
                    le.OnUpdateString.RemoveAllListeners();
                }
                _gameModeConfigurationText.text = configuration ?? string.Empty;
            }
        }

        public void SetCenterOverrideLocalized(LocalizedString description, LocalizedString configuration)
        {
            if (_gameModeDescriptionText != null)
            {
                var le = _gameModeDescriptionText.GetComponent<LocalizeStringEvent>();
                if (le == null)
                {
                    le = _gameModeDescriptionText.gameObject.AddComponent<LocalizeStringEvent>();
                }
                le.OnUpdateString.RemoveAllListeners();
                le.OnUpdateString.AddListener(s =>
                {
                    if (_gameModeDescriptionText != null) _gameModeDescriptionText.text = s ?? string.Empty;
                });
                le.StringReference = description;
                le.RefreshString();
            }

            if (_gameModeConfigurationText != null)
            {
                var le = _gameModeConfigurationText.GetComponent<LocalizeStringEvent>();
                if (le == null)
                {
                    le = _gameModeConfigurationText.gameObject.AddComponent<LocalizeStringEvent>();
                }
                le.OnUpdateString.RemoveAllListeners();
                le.OnUpdateString.AddListener(s =>
                {
                    if (_gameModeConfigurationText != null) _gameModeConfigurationText.text = s ?? string.Empty;
                });
                le.StringReference = configuration;
                le.RefreshString();
            }
        }

        /// <summary>
        /// In lobby mode we still want to show correct visuals (image, description, configuration)
        /// for the currently selected GameModeData.
        /// </summary>
        public void SetModeVisuals(GameModeData selectedMode)
        {
            // Background image / icon
            if (_gameModeBackgroundImage != null)
            {
                if (selectedMode != null && selectedMode.modeImage != null)
                {
                    _gameModeBackgroundImage.sprite = selectedMode.modeImage;
                    _gameModeBackgroundImage.enabled = true;
                }
                else
                {
                    _gameModeBackgroundImage.sprite = null;
                    _gameModeBackgroundImage.enabled = false;
                }
            }

            // Description (localized)
            if (_gameModeDescriptionText != null)
            {
                if (selectedMode != null && !selectedMode.modeDescription.IsEmpty)
                {
                    if (_descriptionLocalizeEvent == null)
                    {
                        _descriptionLocalizeEvent = _gameModeDescriptionText.GetComponent<LocalizeStringEvent>()
                            ?? _gameModeDescriptionText.gameObject.AddComponent<LocalizeStringEvent>();
                        _descriptionLocalizeEvent.OnUpdateString.RemoveAllListeners();
                        _descriptionLocalizeEvent.OnUpdateString.AddListener(s =>
                        {
                            if (_gameModeDescriptionText != null) _gameModeDescriptionText.text = s ?? string.Empty;
                        });
                    }
                    _descriptionLocalizeEvent.StringReference = selectedMode.modeDescription;
                    _descriptionLocalizeEvent.RefreshString();
                }
                else
                {
                    _gameModeDescriptionText.text = string.Empty;
                }
            }

            // Configuration (localized + args)
            if (_gameModeConfigurationText != null)
            {
                if (selectedMode != null && !selectedMode.modeConfiguration.IsEmpty)
                {
                    if (_configurationLocalizeEvent == null)
                    {
                        _configurationLocalizeEvent = _gameModeConfigurationText.GetComponent<LocalizeStringEvent>()
                            ?? _gameModeConfigurationText.gameObject.AddComponent<LocalizeStringEvent>();
                        _configurationLocalizeEvent.OnUpdateString.RemoveAllListeners();
                        _configurationLocalizeEvent.OnUpdateString.AddListener(s =>
                        {
                            if (_gameModeConfigurationText != null) _gameModeConfigurationText.text = s ?? string.Empty;
                        });
                    }

                    string timeLimitText = selectedMode.timeLimitInMinutes > 0f
                        ? TimeSpan.FromSeconds(selectedMode.timeLimitInMinutes * 60f).ToString("m\\:ss")
                        : "-";

                    var configArgs = new object[] { selectedMode.carrotGoal, timeLimitText };
                    var configLocalized = new LocalizedString
                    {
                        TableReference = selectedMode.modeConfiguration.TableReference,
                        TableEntryReference = selectedMode.modeConfiguration.TableEntryReference,
                        Arguments = configArgs
                    };

                    _configurationLocalizeEvent.StringReference = configLocalized;
                    _configurationLocalizeEvent.RefreshString();
                }
                else
                {
                    _gameModeConfigurationText.text = string.Empty;
                }
            }
        }

        private void CreateLobbyButtons()
        {
            if (_gameModeScrollView == null || _scrollContent == null || _gameModeButtonTemplate == null)
                return;

            HideTemplateButton();
            ClearOldButtons();
            SetupLayout();

            for (int i = 0; i < _lobbyEntries.Count; i++)
            {
                int idx = i;
                GameObject buttonObj = Instantiate(_gameModeButtonTemplate, _scrollContent);
                buttonObj.SetActive(true);

                Button button = buttonObj.GetComponent<Button>() ?? buttonObj.GetComponentInChildren<Button>();
                if (button == null)
                    continue;

                // In lobby mode, click does nothing by default (selection is not used).
                button.onClick.AddListener(() => { _selectedIndex = idx; });
                _createdButtons.Add(button);

                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    // Ensure no localization overwrites it.
                    LocalizeStringEvent localizeStringEvent = buttonText.GetComponent<LocalizeStringEvent>();
                    if (localizeStringEvent != null)
                    {
                        localizeStringEvent.StringReference = null;
                        localizeStringEvent.OnUpdateString.RemoveAllListeners();
                    }
                    buttonText.text = _lobbyEntries[idx] ?? string.Empty;
                }
            }
        }
    }
}


