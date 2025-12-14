using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using UnityEditor.Search;

public partial class GUICustomElement_GameModeSelector : Interface.Element.LocalizedElementBase
{

    [Header("Game Mode Selector References")]
    [SerializeField] private ScrollRect _gameModeScrollView;
    [SerializeField] private RectTransform _scrollContent;
    [SerializeField] private Image _gameModeBackgroundImage;
    [SerializeField] private TextMeshProUGUI _gameModeDescriptionText;
    [SerializeField] private TextMeshProUGUI _gameModeConfigurationText;

    [Header("Button Template")]
    [SerializeField] private GameObject _gameModeButtonTemplate;

    [Header("Sound")]
    [SerializeField] private AudioClip _showSound;
    [SerializeField] private AudioClip _hideSound;

    [Header("Layout Settings")]
    [SerializeField] private float _buttonSpacing = 10f;
    [SerializeField] private bool _allowAnchorModificationOverride = false;

    private List<GameModeData> _gameModes = new List<GameModeData>();
    private List<Button> _createdButtons = new List<Button>();
    private int _selectedIndex = 0;

    public override bool AllowAnchorModification => _allowAnchorModificationOverride;

    public int SelectedIndex => _selectedIndex;

    public override void InitializeWithArgument(object argument)
    {
        if (argument is List<GameModeData> gameModes)
        {
            _gameModes = gameModes;
        }
        else if (argument is GameModeData[] gameModesArray)
        {
            _gameModes = new List<GameModeData>(gameModesArray);
        }
        else
        {
            DebugHelper.LogWarning(this, $"GameModeSelector: Invalid argument type. Expected List<GameModeData> or GameModeData[], got {argument?.GetType()}");
            return;
        }

        // Initialize buttons if setup was already called
        if (_gameModeScrollView != null && _gameModeButtonTemplate != null)
        {
            CreateButtons();
            
            // Select first mode by default if available
            if (_gameModes.Count > 0)
            {
                SelectMode(0);
            }
        }
    }

    public GameModeData GetSelectedGameMode()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _gameModes.Count)
        {
            return _gameModes[_selectedIndex];
        }
        return null;
    }

    protected override void Setup()
    {
        base.Setup();

        if (_gameModeScrollView == null)
        {
            Debug.LogError("GameModeSelector: gameModeScrollView is not assigned!");
            return;
        }

        if (_scrollContent == null)
        {
            // Try to find Content child in ScrollView
            Transform contentTransform = _gameModeScrollView.transform.Find("Viewport/Content");
            if (contentTransform != null)
            {
                _scrollContent = contentTransform.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogError("GameModeSelector: scrollContent is not assigned and cannot be found!");
                return;
            }
        }

        if (_gameModeButtonTemplate == null)
        {
            Debug.LogError("GameModeSelector: gameModeButtonTemplate is not assigned!");
            return;
        }

        // Only create buttons if game modes are already initialized
        // Otherwise, they will be created in InitializeWithArgument
        if (_gameModes != null && _gameModes.Count > 0)
        {
            CreateButtons();
            
            // Select first mode by default if available
            SelectMode(0);
        }

        FixCustomElementLayout();

        AudioManager.PreloadClips(_showSound, _hideSound);
    }

    public void FixCustomElementLayout()
    {
        RectTransform rectTransform = GetRectTransform();
        if (rectTransform != null)
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }
        }
    }

    void HideTemplateButton()
    {
        if (_gameModeButtonTemplate != null)
        {
            _gameModeButtonTemplate.SetActive(false);
            if (_gameModeButtonTemplate.transform.parent == _scrollContent)
            {
                _gameModeButtonTemplate.transform.SetParent(transform, false);
            }
            // hide template 
            _gameModeButtonTemplate.transform.position = new Vector3(10000, 10000, 0);
        }
    }

    void ClearOldButtons()
    {
        foreach (var button in _createdButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        _createdButtons.Clear();
    }

    void SetupLayout()
    {
        VerticalLayoutGroup layoutGroup = _scrollContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = _scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layoutGroup.spacing = _buttonSpacing;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        // Ensure ContentSizeFitter is present for proper scrolling
        ContentSizeFitter sizeFitter = _scrollContent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = _scrollContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void AddButtons()
    {
        for (int i = 0; i < _gameModes.Count; i++)
        {
            int modeIndex = i; // Capture for closure
            GameObject buttonObj = Instantiate(_gameModeButtonTemplate, _scrollContent);
            buttonObj.SetActive(true);

            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.GetComponentInChildren<Button>();
            }

            if (button == null)
            {
                DebugHelper.LogWarning(this, $"GameModeSelector: Button component not found in template for mode {i}");
                return;
            }


            button.onClick.AddListener(() => OnModeButtonClicked(modeIndex));
            _createdButtons.Add(button);

            // Try to set button text if there's a TextMeshProUGUI component
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && !_gameModes[modeIndex].modeName.IsEmpty)
            {
                // Clear any default text from template
                buttonText.text = "";
                
                // Use LocalizeStringEvent for automatic localization updates
                LocalizeStringEvent localizeStringEvent = buttonText.GetComponent<LocalizeStringEvent>();
                if (localizeStringEvent == null)
                {
                    localizeStringEvent = buttonText.gameObject.AddComponent<LocalizeStringEvent>();
                }
                
                // Set the localized string reference - this will automatically update on language change
                localizeStringEvent.StringReference = _gameModes[modeIndex].modeName;
                
                // Subscribe to update event to ensure text is set when language changes
                localizeStringEvent.OnUpdateString.RemoveAllListeners();
                localizeStringEvent.OnUpdateString.AddListener((localizedString) =>
                {
                    if (buttonText != null)
                    {
                        buttonText.text = localizedString;
                    }
                });
                
                // Refresh to load initial text
                localizeStringEvent.RefreshString();
            }
        }
    }

    public override void Show()
    {
        base.Show();
        if (_isReady)
            AudioManager.PlaySoundUI(_showSound);
    }

    public override void Hide()
    {
        base.Hide();
        if (_isReady)
            AudioManager.PlaySoundUI(_hideSound);
    }

    private void CreateButtons()
    {
        if (_gameModes == null || _gameModes.Count == 0)
        {
            Debug.LogError("GameModeSelector: No game modes provided!");
            return;
        }

        // Ensure template is disabled and not in scrollContent hierarchy
        HideTemplateButton();

        // Clear existing buttons
        ClearOldButtons();

        // Setup VerticalLayoutGroup on scroll content if not present
        SetupLayout();

        // Create buttons for each game mode
        AddButtons();

    }

    private void OnModeButtonClicked(int index)
    {
        if (index >= 0 && index < _gameModes.Count)
        {
            SelectMode(index);
        }
    }

    private void SelectMode(int index)
    {
        if (index < 0 || index >= _gameModes.Count)
        {
            DebugHelper.LogWarning(this, $"GameModeSelector: Invalid mode index {index}");
            return;
        }

        _selectedIndex = index;
        GameModeData selectedMode = _gameModes[index];

        // Update background image
        if (_gameModeBackgroundImage != null && selectedMode.modeImage != null)
        {
            _gameModeBackgroundImage.sprite = selectedMode.modeImage;
        }

        // Update description text
        if (_gameModeDescriptionText != null)
        {
            if (!selectedMode.modeDescription.IsEmpty)
            {
                selectedMode.modeDescription.GetLocalizedStringAsync().Completed += (op) =>
                {
                    if (op.IsDone && op.Result != null && _gameModeDescriptionText != null)
                    {
                        _gameModeDescriptionText.text = op.Result;
                    }
                };
            }
            else
            {
                _gameModeDescriptionText.text = "";
            }
        }
        // Update configuration text
        if (_gameModeConfigurationText != null)
        {
            if (!selectedMode.modeConfiguration.IsEmpty)
            {

                var gameModeDescriptionOp = new object[] {
                    selectedMode.carrotGoal,
                    TimeSpan.FromSeconds(selectedMode.timeLimitInMinutes * 60)
                    .ToString("m\\:ss") 
                };

                selectedMode.modeConfiguration.GetLocalizedStringAsync(gameModeDescriptionOp).Completed += (op) =>
                {
                    if (op.IsDone && op.Result != null && _gameModeConfigurationText != null)
                    {
                        _gameModeConfigurationText.text = op.Result;
                    }
                };
            }
            else
            {
                _gameModeConfigurationText.text = "";
            }
        }
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
        // Clean up button listeners
        foreach (var button in _createdButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}

