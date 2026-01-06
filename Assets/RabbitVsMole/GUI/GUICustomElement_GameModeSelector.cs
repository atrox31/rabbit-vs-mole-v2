using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using Interface.Element;
using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole
{
    public partial class GUICustomElement_GameModeSelector : Interface.Element.LocalizedElementBase
    {
        private const int DEFAULT_AI_INTELLIGENCE = 90;
        private int _aiIntelligence = DEFAULT_AI_INTELLIGENCE; // 0..100

        public readonly struct InitArgs
        {
            public readonly List<GameModeData> GameModes;
            public readonly bool ShowAiSlider;

            public InitArgs(List<GameModeData> gameModes, bool showAiSlider)
            {
                GameModes = gameModes ?? new List<GameModeData>();
                ShowAiSlider = showAiSlider;
            }

            public InitArgs(GameModeData[] gameModes, bool showAiSlider)
            {
                GameModes = gameModes != null ? new List<GameModeData>(gameModes) : new List<GameModeData>();
                ShowAiSlider = showAiSlider;
            }
        }

        [Header("Game Mode Selector References")]
        [SerializeField] private ScrollRect _gameModeScrollView;
        [SerializeField] private RectTransform _scrollContent;
        [SerializeField] private Image _gameModeBackgroundImage;
        [SerializeField] private TextMeshProUGUI _gameModeDescriptionText;
        [SerializeField] private TextMeshProUGUI _gameModeConfigurationText;
        [SerializeField] private GUISlider _gameModeAiLevelSlider;
        [SerializeField] private GameObject _selectedMutatorIconTemplate;
        [SerializeField] private RectTransform _selectedIconsContainer;

        [Header("Button Template")]
        [SerializeField] private GameObject _gameModeButtonTemplate;

        [Header("Sound")]
        [SerializeField] private AudioClip _showSound;
        [SerializeField] private AudioClip _hideSound;

        [Header("Mutator selector")]
        [SerializeField] private Button _mutatorConfigButton;

        [Header("Layout Settings")]
        [SerializeField] private float _buttonSpacing = 10f;
        [SerializeField] private bool _allowAnchorModificationOverride = false;

        private List<GameModeData> _gameModes = new List<GameModeData>();
        private List<Button> _createdButtons = new List<Button>();
        private readonly List<Button> _selectedMutatorIconButtons = new();
        private int _selectedIndex = 0;
        private bool _showAiSliderContext;
        
        private LocalizeStringEvent _descriptionLocalizeEvent;
        private LocalizeStringEvent _configurationLocalizeEvent;

        public override bool AllowAnchorModification => _allowAnchorModificationOverride;

        public int SelectedIndex => _selectedIndex;

        public int GetSelectedAiIntelligence() => Mathf.Clamp(_aiIntelligence, 0, 100);

        public event Action<GUICustomElement_GameModeSelector, GameModeData, List<MutatorSO>> MutatorConfigRequested;

        private void OnEnable()
        {
            // Safety: if someone toggles the slider active in prefab/scene, keep it consistent with context.
            UpdateAiSliderVisibility();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // In editor there is no runtime context (InitArgs), so default to hidden unless explicitly enabled at runtime.
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _showAiSliderContext = false;
                UpdateAiSliderVisibility();
            }
        }
#endif

        public override void InitializeWithArgument(object argument)
        {
            if (argument is InitArgs initArgs)
            {
                _gameModes = initArgs.GameModes ?? new List<GameModeData>();
                _showAiSliderContext = initArgs.ShowAiSlider;
            }
            else if (argument is List<GameModeData> gameModes)
            {
                _gameModes = gameModes;
                _showAiSliderContext = false;
            }
            else if (argument is GameModeData[] gameModesArray)
            {
                _gameModes = new List<GameModeData>(gameModesArray);
                _showAiSliderContext = false;
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

            UpdateAiSliderVisibility();
            SetupAiSliderIfNeeded();
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

            UpdateAiSliderVisibility();
            SetupAiSliderIfNeeded();

            FixCustomElementLayout();
            HideSelectedMutatorTemplate();
            SetupSelectedIconsLayout();
            WireMutatorConfigButton();
            RefreshSelectedMutatorIcons(GetSelectedGameMode());

            AudioManager.PreloadClips(_showSound, _hideSound);
        }

        private void UpdateAiSliderVisibility()
        {
            if (_gameModeAiLevelSlider == null) return;

            // Slider ma być widoczny tylko w miejscach, które jawnie na to pozwalają (np. Duel Local Solo).
            _gameModeAiLevelSlider.gameObject.SetActive(_showAiSliderContext);
        }

        private void SetupAiSliderIfNeeded()
        {
            if (_gameModeAiLevelSlider == null) return;
            if (!_showAiSliderContext) return;

            // Localization key: label_ai_level (table: Interface)
            var localizedLabel = new LocalizedString("Interface", "label_ai_level");
            _gameModeAiLevelSlider.Initialize(
                localizedLabel: localizedLabel,
                onValueChanged: OnAiSliderChanged,
                getCurrentValue: () => Mathf.Clamp01(_aiIntelligence / 100f),
                valueFormatter: v => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f).ToString());
        }

        private void OnAiSliderChanged(float slider01)
        {
            _aiIntelligence = Mathf.RoundToInt(Mathf.Clamp01(slider01) * 100f);
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

            // Update description text with LocalizeStringEvent for automatic language change updates
            if (_gameModeDescriptionText != null)
            {
                if (!selectedMode.modeDescription.IsEmpty)
                {
                    if (_descriptionLocalizeEvent == null)
                    {
                        _descriptionLocalizeEvent = _gameModeDescriptionText.GetComponent<LocalizeStringEvent>();
                        if (_descriptionLocalizeEvent == null)
                        {
                            _descriptionLocalizeEvent = _gameModeDescriptionText.gameObject.AddComponent<LocalizeStringEvent>();
                        }
                        _descriptionLocalizeEvent.OnUpdateString.AddListener((localizedString) =>
                        {
                            if (_gameModeDescriptionText != null)
                            {
                                _gameModeDescriptionText.text = localizedString;
                            }
                        });
                    }
                    _descriptionLocalizeEvent.StringReference = selectedMode.modeDescription;
                    _descriptionLocalizeEvent.RefreshString();
                }
                else
                {
                    _gameModeDescriptionText.text = "";
                }
            }
            // Update configuration text with LocalizeStringEvent for automatic language change updates
            if (_gameModeConfigurationText != null)
            {
                if (!selectedMode.modeConfiguration.IsEmpty)
                {
                    if (_configurationLocalizeEvent == null)
                    {
                        _configurationLocalizeEvent = _gameModeConfigurationText.GetComponent<LocalizeStringEvent>();
                        if (_configurationLocalizeEvent == null)
                        {
                            _configurationLocalizeEvent = _gameModeConfigurationText.gameObject.AddComponent<LocalizeStringEvent>();
                        }
                        _configurationLocalizeEvent.OnUpdateString.AddListener((localizedString) =>
                        {
                            if (_gameModeConfigurationText != null)
                            {
                                _gameModeConfigurationText.text = localizedString;
                            }
                        });
                    }

                    string timeLimitText = selectedMode.timeLimitInMinutes > 0f
                        ? TimeSpan.FromSeconds(selectedMode.timeLimitInMinutes * 60f).ToString("m\\:ss")
                        : "-";

                    var configArgs = new object[] { selectedMode.carrotGoal, timeLimitText };

                    // IMPORTANT: assigning StringReference triggers an immediate refresh internally.
                    // Build a LocalizedString that already has Arguments set, otherwise entries that
                    // contain "{1}" will throw before we get a chance to assign Arguments.
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
                    _gameModeConfigurationText.text = "";
                }
            }

            RefreshSelectedMutatorIcons(selectedMode);
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
            
            // Clean up localization event listeners
            if (_descriptionLocalizeEvent != null)
            {
                _descriptionLocalizeEvent.OnUpdateString.RemoveAllListeners();
            }
            if (_configurationLocalizeEvent != null)
            {
                _configurationLocalizeEvent.OnUpdateString.RemoveAllListeners();
            }

            if (_mutatorConfigButton != null)
            {
                _mutatorConfigButton.onClick.RemoveListener(OnMutatorConfigClicked);
            }

            foreach (var btn in _selectedMutatorIconButtons)
            {
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }
            }
        }

        private void WireMutatorConfigButton()
        {
            if (_mutatorConfigButton == null) return;
            _mutatorConfigButton.onClick.RemoveListener(OnMutatorConfigClicked);
            _mutatorConfigButton.onClick.AddListener(OnMutatorConfigClicked);
        }

        private void OnMutatorConfigClicked()
        {
            var currentMode = GetSelectedGameMode();
            if (currentMode == null)
            {
                DebugHelper.LogWarning(this, "GameModeSelector: No selected game mode for mutator config.");
                return;
            }

            var currentSelection = currentMode.mutators != null
                ? currentMode.mutators.Where(m => m != null).ToList()
                : new List<MutatorSO>();

            MutatorConfigRequested?.Invoke(this, currentMode, currentSelection);
        }

        private void HideSelectedMutatorTemplate()
        {
            if (_selectedMutatorIconTemplate == null) return;
            _selectedMutatorIconTemplate.SetActive(false);
            _selectedMutatorIconTemplate.transform.position = new Vector3(10000, 10000, 0);
        }

        private void SetupSelectedIconsLayout()
        {
            if (_selectedIconsContainer == null && _selectedMutatorIconTemplate != null)
            {
                _selectedIconsContainer = _selectedMutatorIconTemplate.transform.parent as RectTransform;
            }

            if (_selectedIconsContainer == null)
            {
                DebugHelper.LogWarning(this, "GameModeSelector: Selected icons container is not assigned.");
                return;
            }

            HorizontalLayoutGroup layoutGroup = _selectedIconsContainer.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = _selectedIconsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            layoutGroup.spacing = _buttonSpacing;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter sizeFitter = _selectedIconsContainer.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = _selectedIconsContainer.gameObject.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void ClearSelectedMutatorIcons()
        {
            foreach (var btn in _selectedMutatorIconButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            _selectedMutatorIconButtons.Clear();
        }

        private void RefreshSelectedMutatorIcons(GameModeData selectedMode)
        {
            ClearSelectedMutatorIcons();

            if (_selectedIconsContainer == null || _selectedMutatorIconTemplate == null)
            {
                DebugHelper.LogWarning(this, "GameModeSelector: Missing selected mutator template or container, cannot render icons.");
                return;
            }

            var mutators = selectedMode?.mutators;
            if (mutators == null || mutators.Count == 0)
                return;

            foreach (var mutator in mutators)
            {
                if (mutator == null)
                    continue;

                GameObject iconObj = Instantiate(_selectedMutatorIconTemplate, _selectedIconsContainer);
                iconObj.SetActive(true);

                Button button = iconObj.GetComponent<Button>() ?? iconObj.GetComponentInChildren<Button>();
                if (button != null)
                {
                    MutatorSO captured = mutator;
                    button.onClick.AddListener(() => MutatorConfigRequested?.Invoke(this, selectedMode, selectedMode.mutators?.Where(m => m != null).ToList() ?? new List<MutatorSO>()));
                    _selectedMutatorIconButtons.Add(button);
                }

                Image image = iconObj.GetComponent<Image>() ?? iconObj.GetComponentInChildren<Image>();
                if (image != null)
                {
                    image.sprite = mutator.image;
                    image.enabled = mutator.image != null;
                }

                TextMeshProUGUI label = iconObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    LocalizeStringEvent le = label.GetComponent<LocalizeStringEvent>();
                    if (le != null)
                    {
                        le.StringReference = null;
                        le.OnUpdateString.RemoveAllListeners();
                    }
                    label.text = mutator.GetLocalizedName() ?? mutator.name;
                }
            }
        }

        public void ApplyMutatorsToSelectedMode(IEnumerable<MutatorSO> mutators)
        {
            var selectedMode = GetSelectedGameMode();
            if (selectedMode == null)
            {
                DebugHelper.LogWarning(this, "GameModeSelector: Cannot apply mutators, no mode selected.");
                return;
            }

            var safeList = mutators != null
                ? mutators.Where(m => m != null).ToList()
                : new List<MutatorSO>();

            selectedMode.mutators = safeList;
            RefreshSelectedMutatorIcons(selectedMode);
        }
    }

}