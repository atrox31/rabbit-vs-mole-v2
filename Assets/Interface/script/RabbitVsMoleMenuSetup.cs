using System.Collections.Generic;
using UnityEngine;
using Interface;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem;

public class RabbitVsMoleMenuSetup : MonoBehaviour
{
    private MainMenuManager _menuManager;
    private GUIPanel _mainMenu;

    private GUIPanel _panelOptions;
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
            private GUIPanel _playPanelDuelLocal;
            private GUIPanel _playPanelDuelOnline;

    private GUIPanel _creditsPanel;

    [SerializeField] private TMPro.TextMeshProUGUI _versionLabel;

    [Header("Localization Settings")]
    [SerializeField] private string _localizationTableName = "Interface";

    [Header("Key Bindings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerPrimaryActions = "PlayerKeyboard1";
    [SerializeField] private string playerSecondaryActions = "PlayerKeyboard2";
    [SerializeField] private string playerGamepadActions = "PlayerGamepad";

    private void Start()
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

        if(_versionLabel != null)
        {
            _versionLabel.text = "V " + Application.version;
        }

        SetupMenus();
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

    private void SetupMenus()
    {
        // Create panels with localized names
        _playPanel = _menuManager.CreatePanel(GetLocalizedString("menu_play"))
            .AddButton(GetLocalizedString("button_play_story"), _playPanelStory)
            .AddButton(GetLocalizedString("button_play_challenge"), _playPanelChalleange, true, GetLocalizedString("tooltip_challenge_disabled"))
            .AddButton(GetLocalizedString("button_play_duel"), _playPanelDuel)
            .AddBackButton()
            .Build();

        _playPanelStory = _menuManager.CreatePanel(GetLocalizedString("menu_play_story_rabbit"))
            //TODO: Add story mode levels selection here
            .AddBackButton()
            .Build();

        _playPanelChalleange = _menuManager.CreatePanel(GetLocalizedString("menu_play_challenge"))
            //TODO: Add challenge mode levels selection here
            .AddBackButton()
            .Build();

        _playPanelDuel = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel"))
            .AddButton(GetLocalizedString("button_play_duel_local"), _playPanelDuelLocal)
            .AddButton(GetLocalizedString("button_play_duel_online"), _playPanelDuelOnline)
            .AddBackButton()
            .Build();

        _creditsPanel = _menuManager.CreatePanel(GetLocalizedString("menu_credits"))
            .AddBackButton()
            .Build();

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
            .AddKeyBindControls(inputActions, playerPrimaryActions)
            .AddBackButton()
            .Build();

        _panelOptionsControlsKeyboardSecondary = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_keyboard_secondary"))
            .AddKeyBindControls(inputActions, playerSecondaryActions)
            .AddBackButton()
            .Build();

        _panelOptionsControlsGamepad = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_gamepad"))
            .AddKeyBindControls(inputActions, playerGamepadActions)
            .AddBackButton()
            .Build();

        _panelOptionsControls = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls"))
            .AddButton(GetLocalizedString("button_controls_keyboard_primary"), _panelOptionsControlsKeyboardPrimary)
            .AddButton(GetLocalizedString("button_controls_keyboard_secondary"), _panelOptionsControlsKeyboardSecondary)
            .AddButton(GetLocalizedString("button_controls_gamepad"), _panelOptionsControlsGamepad)
            .AddBackButton()
            .Build();
        
        _panelOptions = _menuManager.CreatePanel(GetLocalizedString("menu_options"))
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

        _menuManager.ChangePanel(_mainMenu);
    }
}