using System.Collections.Generic;
using UnityEngine;
using Interface;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using Extensions;

public class RabbitVsMoleMenuSetup : MonoBehaviour
{
    private MainMenuManager _menuManager;
    private GUIPanel _mainMenu;

    private GUIPanel _panelOptions;
        private GUIPanel _panelOptionsGeneral;
        private GUIPanel _panelOptionsGraphic;
        private GUIPanel _panelOptionsAudio;
        private GUIPanel _panelOptionsControls;
            private GUIPanel _panelOptionsControlsKeyboardPrimary;
            private GUIPanel _panelOptionsControlsKeyboardSecondary;
            private GUIPanel _panelOptionsControlsGamepad;

    private GUIPanel _playPanel;
        private GUIPanel _playPanelStory;
            private GUIPanel _playPanelStoryRabbit;
            private GUIPanel _playPanelStoryMole;
        private GUIPanel _playPanelChalleange;
        private GUIPanel _playPanelDuel;
            private GUIPanel _playPanelDuelLocalSolo;
            private GUIPanel _playPanelDuelLocalSplit;
            private GUIPanel _playPanelDuelOnline;

    private GUIPanel _creditsPanel;
    private List<Interface.Element.GUILabel> _creditLabels = new List<Interface.Element.GUILabel>();

    [SerializeField] private TMPro.TextMeshProUGUI _versionLabel;

    [Header("Localization Settings")]
    [SerializeField] private string _localizationTableName = "Interface";

    [Header("Key Bindings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerActionMap = "Player";
    [SerializeField] private string playerPrimaryBindingGroup = "KeyboardP1";
    [SerializeField] private string playerSecondaryBindingGroup = "KeyboardP2";
    [SerializeField] private string playerGamepadBindingGroup = "Gamepad";

    [Header("Custom elements")]
    [SerializeField] private GameObject daySelectorPrefab;
    [SerializeField] private GameObject widePanelPrefab;
    [SerializeField] private GameObject gameModeSelectorPrefab;

    [Header("GameModes")]
    [SerializeField] private List<GameModeData> rabbitStoryGameModes;
    [SerializeField] private List<GameModeData> moleStoryGameModes;
    [SerializeField] private List<GameModeData> challengeGameModes;
    [SerializeField] private List<GameModeData> duelGameModes;

    private bool IsStoryComplite => GameManager.GetStoryProgress(System.DayOfWeek.Sunday, PlayerType.Rabbit);
    void Awake()
    {
        _menuManager = GetComponent<MainMenuManager>();
        if (_menuManager == null)
        {
            Debug.LogError("MainMenuManager not found!");
            return;
        }

        if (inputActions != null)
        {
            InputBindingManager.LoadInputBindings(inputActions);
        }
        else
        {
            Debug.LogWarning("Input Action Asset is not assigned on RabbitVsMoleMenuSetup.");
        }

        if (_versionLabel != null)
        {
            _versionLabel.text = "V " + Application.version;
        }

        SetupMenus();
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    public void ShowMenu()
    {
        _menuManager.ChangePanel(_mainMenu);
    }

    public void WebPageRedirect()
    {
        Application.OpenURL("https://gamejolt.com/@atrox_studio");
    }

    /// <summary>
    /// Helper method to create a LocalizedString from a table and key
    /// </summary>
    private LocalizedString GetLocalizedString(string key)
    {
        var localizedString = new LocalizedString();
        localizedString.TableReference = _localizationTableName;
        localizedString.TableEntryReference = key;
        return localizedString;
    }

    private int GetMaxStoryProgress(PlayerType playerType)
    {

        if (GameManager.GetStoryProgress(System.DayOfWeek.Sunday, playerType))
        {
            return 7;
        }
        else
        {
            for (var i = 6; i >= 0; i--)
            {
                if (GameManager.GetStoryProgress((System.DayOfWeek)i, playerType))
                {
                    return i;
                }
            }
            return 1;
        }
    }

    DayOfWeek GetDayFromSelectedIndex(int index)
    {
        if(index < 0 || index > 6)
        {
            Debug.LogWarning("GetDayFromSelectedIndex: Index out of range, returning Monday as default.");
            return DayOfWeek.Monday;
        }

        int dayValue = (index + 1) % 7;
        return (DayOfWeek)dayValue;
    }

    void PlayStoryRabbit()
    {
        var gamemode = (_playPanelStoryRabbit.GetElementByID("GameModeStoryRabbit") as GUICustomElement_GameModeSelector).GetSelectedGameMode();
        var dayOfWeek = GetDayFromSelectedIndex((_playPanelStoryRabbit.GetElementByID("GameModeStoryRabbit") as GUICustomElement_GameModeSelector).SelectedIndex);

        GameManager.PlayGame(
            gameMode: gamemode,
            map: GameSceneManager.SceneType.Gameplay_RabbitSolo,
            day: dayOfWeek,
            playerTypeForStory: PlayerType.Rabbit,
            rabbitControlAgent: PlayerControlAgent.Human,
            moleControlAgent: PlayerControlAgent.None);
    }
    void PlayStoryMole()
    {
        var gamemode = (_playPanelStoryMole.GetElementByID("GameModeStoryMole") as GUICustomElement_GameModeSelector).GetSelectedGameMode();
        var dayOfWeek = GetDayFromSelectedIndex((_playPanelStoryRabbit.GetElementByID("GameModeStoryMole") as GUICustomElement_GameModeSelector).SelectedIndex);

        GameManager.PlayGame(
            gameMode: gamemode,
            map: GameSceneManager.SceneType.GamePlay_MoleSolo,
            day: dayOfWeek,
            playerTypeForStory: PlayerType.Mole,
            rabbitControlAgent: PlayerControlAgent.None,
            moleControlAgent: PlayerControlAgent.Human);
    }

    void PlayDuelSplitScreen()
    {
        var gamemode = (_playPanelDuelLocalSplit.GetElementByID("GameModelDuelLocalSplit") as GUICustomElement_GameModeSelector).GetSelectedGameMode();
        GameManager.PlayGame(
            gameMode: gamemode, 
            map: GameSceneManager.SceneType.GamePlay_Duel, 
            day: System.DayOfWeek.Monday.SelectRandom(), 
            playerTypeForStory: PlayerType.Rabbit, 
            rabbitControlAgent: PlayerControlAgent.Human,
            moleControlAgent: PlayerControlAgent.Human);
    }

    void PlayDuelSolo(PlayerType playerType)
    {
        var gamemode = (_playPanelDuelLocalSolo.GetElementByID("GameModeDuelLocalSolo") as GUICustomElement_GameModeSelector).GetSelectedGameMode();
        GameManager.PlayGame(
            gameMode: gamemode,
            map: GameSceneManager.SceneType.GamePlay_Duel,
            day: System.DayOfWeek.Monday.SelectRandom(),
            playerTypeForStory: playerType,
            rabbitControlAgent: playerType == PlayerType.Rabbit ? PlayerControlAgent.Human : PlayerControlAgent.AI,
            moleControlAgent: playerType == PlayerType.Mole ? PlayerControlAgent.Human : PlayerControlAgent.AI);
    }

    private void SetupMenus()
    {
        _playPanelStoryMole = _menuManager.CreatePanel(GetLocalizedString("menu_play_story_mole"))
            .AddCustomElement(gameModeSelectorPrefab, rabbitStoryGameModes.GetRange(0, GetMaxStoryProgress(PlayerType.Mole)))
                .SetId("PlayPanelStoryMole")
            .AddButton(GetLocalizedString("button_play_story"), () => { }, true)
            .AddBackButton()
            .Build();

        _playPanelStoryRabbit = _menuManager.CreatePanel(GetLocalizedString("menu_play_story_rabbit"), widePanelPrefab)
            .AddCustomElement(gameModeSelectorPrefab, rabbitStoryGameModes.GetRange(0, GetMaxStoryProgress(PlayerType.Rabbit)))
                .SetId("GameModeStoryRabbit")
            .AddButton(GetLocalizedString("button_play_story"), PlayStoryRabbit, true)
            .AddBackButton()
            .Build();

        _playPanelStory = _menuManager.CreatePanel(GetLocalizedString("menu_play_story"), widePanelPrefab)
            .AddButton(GetLocalizedString("button_play_story_rabbit"), _playPanelStoryRabbit)
            .AddButton(GetLocalizedString("button_play_story_mole"), _playPanelStoryMole)
            .AddBackButton()
            .Build();

        _playPanelDuelLocalSolo = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel_local_solo"), widePanelPrefab)
           .AddCustomElement(gameModeSelectorPrefab, duelGameModes)
                .SetId("GameModeDuelLocalSolo")
           .AddButton(GetLocalizedString("button_play_duel_as_rabbit"),()=>PlayDuelSolo(PlayerType.Rabbit), true)
           .AddButton(GetLocalizedString("button_play_duel_as_mole"), () => PlayDuelSolo(PlayerType.Mole), true)
           .AddBackButton()
           .Build();

        _playPanelDuelLocalSplit = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel_local_split"), widePanelPrefab)
            .AddCustomElement(gameModeSelectorPrefab, duelGameModes)
                .SetId("GameModelDuelLocalSplit")
            .AddButton(GetLocalizedString("button_play_duel"), PlayDuelSplitScreen, true)
            .AddBackButton()
            .Build();

        _playPanelDuelOnline = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel_online"), widePanelPrefab)
            //TODO: 
            .AddBackButton()
            .Build();

        _playPanelDuel = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel"))
            .AddButton(GetLocalizedString("button_play_duel_local_solo"), _playPanelDuelLocalSolo)
            .AddButton(GetLocalizedString("button_play_duel_local_split"), _playPanelDuelLocalSplit)
            .AddButton(GetLocalizedString("button_play_duel_online"), _playPanelDuelOnline)
            .AddBackButton()
            .Build();

        _playPanelChalleange = _menuManager.CreatePanel(GetLocalizedString("menu_play_challenge"))
            //TODO: Add challenge mode levels selection here
            .AddBackButton()
            .Build();

        _playPanel = _menuManager.CreatePanel(GetLocalizedString("menu_play"))
            .AddButton(GetLocalizedString("button_play_story"), moleStoryGameModes.Count == 0 ? _playPanelStoryRabbit : _playPanelStory)
            .AddButton(GetLocalizedString("button_play_challenge"), _playPanelChalleange, !IsStoryComplite, 
                !IsStoryComplite 
                ? GetLocalizedString("tooltip_challenge_disabled") 
                : new LocalizedString())
            .AddButton(GetLocalizedString("button_play_duel"), _playPanelDuel)
            .AddBackButton()
            .Build();

        _creditsPanel = _menuManager.CreatePanel(GetLocalizedString("menu_credits"))
            .AddBackButton()
            .AddAutoScroll(.1f, 2f)
            .Build();
        UpdateCredits();

        _panelOptionsGraphic = _menuManager.CreatePanel(GetLocalizedString("menu_options_graphics"))
            .AddDropDown(GetLocalizedString("option_resolution"), MainMenuDefaultLogic.HandleResolutionChange, MainMenuDefaultLogic.GetAvailableResolutions(), MainMenuDefaultLogic.GetCurrentResolutionIndex)
            .AddToggle(GetLocalizedString("option_fullscreen"), MainMenuDefaultLogic.HandleFullScreen, MainMenuDefaultLogic.GetFullScreenCurrentMode)
            .AddToggle(GetLocalizedString("option_vsync"), MainMenuDefaultLogic.HandleVSync, MainMenuDefaultLogic.GetVSync)
            .AddDropDown(GetLocalizedString("option_quality"), MainMenuDefaultLogic.HandleQualityChange, MainMenuDefaultLogic.GetAvailableQualitySettings(), MainMenuDefaultLogic.GetCurrentQualityIndex)
            .AddBackButton()
            .Build();

        _panelOptionsAudio = _menuManager.CreatePanel(GetLocalizedString("menu_options_audio"))
            .AddSlider(GetLocalizedString("option_master_volume"), MainMenuDefaultLogic.HandleMasterVolumeChange, MainMenuDefaultLogic.GetMasterVolume)
            .AddSlider(GetLocalizedString("option_sfx_volume"), MainMenuDefaultLogic.HandleSFXVolume, MainMenuDefaultLogic.GetSFXVolume)
            .AddSlider(GetLocalizedString("option_music_volume"), MainMenuDefaultLogic.HandleMusicVolumeChange, MainMenuDefaultLogic.GetMusicVolume)
            .AddSlider(GetLocalizedString("option_dialogue_volume"), MainMenuDefaultLogic.HandleDialogueVolumeChange, MainMenuDefaultLogic.GetDialogueVolume)
            .AddSlider(GetLocalizedString("option_ambient_volume"), MainMenuDefaultLogic.HandleAmbientVolumeChange, MainMenuDefaultLogic.GetAmbientVolume)
            .AddBackButton()
            .Build();

        _panelOptionsControlsKeyboardPrimary = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_keyboard_primary"))
            .AddKeyBindControls(inputActions, playerActionMap, playerPrimaryBindingGroup)
            .AddBackButton()
            .Build();

        _panelOptionsControlsKeyboardSecondary = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_keyboard_secondary"))
            .AddKeyBindControls(inputActions, playerActionMap, playerSecondaryBindingGroup)
            .AddBackButton()
            .Build();

        _panelOptionsControlsGamepad = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_gamepad"))
            .AddKeyBindControls(inputActions, playerActionMap, playerGamepadBindingGroup)
            .AddBackButton()
            .Build();

        _panelOptionsControls = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls"))
            .AddButton(GetLocalizedString("button_controls_keyboard_primary"), _panelOptionsControlsKeyboardPrimary)
            .AddButton(GetLocalizedString("button_controls_keyboard_secondary"), _panelOptionsControlsKeyboardSecondary)
            .AddButton(GetLocalizedString("button_controls_gamepad"), _panelOptionsControlsGamepad)
            .AddBackButton()
            .Build();

        _panelOptionsGeneral = _menuManager.CreatePanel(GetLocalizedString("menu_general_options"))
            .AddDropDown(GetLocalizedString("option_language"), MainMenuDefaultLogic.HandleLanguageChange, MainMenuDefaultLogic.GetAvailableLanguages(), MainMenuDefaultLogic.GetCurrentLanguageIndex)
            .AddBackButton()
            .Build();
        
        _panelOptions = _menuManager.CreatePanel(GetLocalizedString("menu_options"))
            .AddButton(GetLocalizedString("button_general"), _panelOptionsGeneral)
            .AddButton(GetLocalizedString("button_graphics"), _panelOptionsGraphic)
            .AddButton(GetLocalizedString("button_audio"), _panelOptionsAudio)
            .AddButton(GetLocalizedString("button_controls"), _panelOptionsControls)
            .AddBackButton()
            .Build();

        _mainMenu = _menuManager.CreatePanel(GetLocalizedString("menu_main"))
            .AddButton(GetLocalizedString("button_play"), _playPanel)
            .AddButton(GetLocalizedString("button_options"), _panelOptions)
            .AddButton(GetLocalizedString("button_credits"), _creditsPanel)
            .AddExitButton()
            .Build();

    }

    private void UpdateCredits()
    {
        // Remove old labels
        foreach (var label in _creditLabels)
        {
            if (label != null)
            {
                _creditsPanel.RemoveElement(label);
                Destroy(label.gameObject);
            }
        }
        _creditLabels.Clear();
        
        // Add new labels
        foreach (var line in PrepareCredits())
        {
            var label = _menuManager.CreateLabel(line);
            _creditsPanel.AddElement(label);
            _creditLabels.Add(label as Interface.Element.GUILabel);
        }
    }
    
    private void OnLanguageChanged(Locale locale)
    {
        UpdateCredits();
    }
    
    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private List<string> PrepareCredits()
    {
        var localizedString = GetLocalizedString("credits_content");
        var op = localizedString.GetLocalizedStringAsync();
        
        if (op.IsDone && op.Result != null)
        {
            return new List<string>(op.Result.Split('\n'));
        }
        else
        {
            // If not done, wait for completion
            string result = op.WaitForCompletion();
            
            if (result != null)
            {
                return new List<string>(result.Split('\n'));
            }
            
            // Fallback - return empty list and update when ready
            op.Completed += (operation) =>
            {
                if (operation.IsDone && operation.Result != null && _creditsPanel != null)
                {
                    UpdateCredits();
                }
            };
            
            return new List<string>();
        }
    }
}