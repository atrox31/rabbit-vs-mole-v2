using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GUICustomElement_GameModeSelector : Interface.Element.InterfaceElement
{

    [Header("Game Mode Selector References")]
    [SerializeField] private ScrollRect _gameModeScrollView;
    [SerializeField] private RectTransform _scrollContent;
    [SerializeField] private Image _gameModeBackgroundImage;
    [SerializeField] private TextMeshProUGUI _gameModeDescriptionText;
    [SerializeField] private TextMeshProUGUI _gameModeConfigurationText;

    [Header("Button Template")]
    [SerializeField] private GameObject _gameModeButtonTemplate;

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
            Debug.LogWarning($"GameModeSelector: Invalid argument type. Expected List<GameModeData> or GameModeData[], got {argument?.GetType()}");
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

        // Disable template so it's not included in layout calculations
        _gameModeButtonTemplate.SetActive(false);

        // Only create buttons if game modes are already initialized
        // Otherwise, they will be created in InitializeWithArgument
        if (_gameModes != null && _gameModes.Count > 0)
        {
            CreateButtons();
            
            // Select first mode by default if available
            SelectMode(0);
        }

        FixCustomElementLayout();
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

    private void CreateButtons()
    {
        // Clear existing buttons
        foreach (var button in _createdButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        _createdButtons.Clear();

        if (_gameModes == null || _gameModes.Count == 0)
        {
            Debug.LogWarning("GameModeSelector: No game modes provided!");
            return;
        }

        // Setup VerticalLayoutGroup on scroll content if not present
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

        // Create buttons for each game mode
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

            if (button != null)
            {
                button.onClick.AddListener(() => OnModeButtonClicked(modeIndex));
                _createdButtons.Add(button);

                // Try to set button text if there's a TextMeshProUGUI component
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && _gameModes[modeIndex].modeName.IsEmpty == false)
                {
                    _gameModes[modeIndex].modeName.GetLocalizedStringAsync().Completed += (op) =>
                    {
                        if (op.IsDone && op.Result != null && buttonText != null)
                        {
                            buttonText.text = op.Result;
                        }
                    };
                }
            }
            else
            {
                Debug.LogWarning($"GameModeSelector: Button component not found in template for mode {i}");
            }
        }
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
            Debug.LogWarning($"GameModeSelector: Invalid mode index {index}");
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
                    TimeSpan.FromSeconds(selectedMode.timeLimitInMinutes)
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

    private void OnDestroy()
    {
        // Clean up button listeners
        foreach (var button in _createdButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }

    internal PlayerType GetSelectedPlayer()
    {
        throw new NotImplementedException();
    }
}

