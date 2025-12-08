using Interface;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Manages the in-game pause menu that appears during gameplay.
/// Handles menu visibility, input detection, and game state management.
/// </summary>
public class RabbitVsMoleInGameMenu : MonoBehaviour
{
    #region Fields
    
    private MainMenuManager _menuManager;
    
    [Header("Localization Settings")]
    [SerializeField] private string _localizationTableName = "Interface";
    
    private GUIPanel _mainMenu;
    private GUIPanel _mainMenuRestartQuestion;
    private GUIPanel _mainMenuBackToMenuQuestion;
    
    private bool _isMenuVisible = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        InitializeComponents();
        SetupMenu();
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeComponents()
    {
        _menuManager = GetComponent<MainMenuManager>();
        if (_menuManager == null)
        {
            Debug.LogError("RabbitVsMoleInGameMenu: MainMenuManager not found!");
        }
    }
    
    private void SetupMenu()
    {
        _mainMenuRestartQuestion = _menuManager.CreatePanel("menu_restart_question")
            .AddLabel(GetLocalizedString("text_restart_question"))
            .AddButton(GetLocalizedString("button_yes"), OnRestartConfirmed)
            .AddButton(GetLocalizedString("button_no"), _menuManager.GoBack)
            .Build();

        _mainMenuBackToMenuQuestion = _menuManager.CreatePanel("menu_back_to_main_menu_question")
            .AddLabel(GetLocalizedString("text_back_to_main_menu_question"))
            .AddButton(GetLocalizedString("button_yes"), OnBackToMainMenuConfirmed)
            .AddButton(GetLocalizedString("button_no"), _menuManager.GoBack)
            .Build();

        _mainMenu = _menuManager.CreatePanel("menu_in_game")
            .AddButton(GetLocalizedString("button_resume"), HideMenu)
            .AddButton(GetLocalizedString("button_restart"), _mainMenuRestartQuestion)
            .AddButton(GetLocalizedString("button_back_to_main_menu"), _mainMenuBackToMenuQuestion)
            .Build();
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleInput()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
        
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }
    
    #endregion
    
    #region Menu Control
    
    private void ToggleMenu()
    {
        if (_isMenuVisible)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }
    
    private void ShowMenu()
    {
        if (_mainMenu == null || _menuManager == null) return;
        if (_isMenuVisible && _mainMenu.IsVisible) return;
        
        _isMenuVisible = true;
        GameManager.Pause();
        
        if (!_mainMenu.gameObject.activeInHierarchy)
        {
            _mainMenu.gameObject.SetActive(true);
        }
        
        _menuManager.ChangePanel(_mainMenu);
    }
    
    private void HideMenu()
    {
        if (!_isMenuVisible) return;
        
        _isMenuVisible = false;
        GameManager.Unpause();
        
        if (_mainMenu != null)
        {
            _menuManager.ClosePanel(_mainMenu);
        }
    }
    
    #endregion
    
    #region Menu Actions
    
    private void OnRestartConfirmed()
    {
        GameManager.Unpause();
        GameManager.RestartGame();
    }
    
    private void OnBackToMainMenuConfirmed()
    {
        GameManager.Unpause();
        GameSceneManager.ChangeScene(GameSceneManager.SceneType.MainMenu);
    }
    
    #endregion
    
    #region Helpers
    
    private LocalizedString GetLocalizedString(string key)
    {
        var localizedString = new LocalizedString();
        localizedString.TableReference = _localizationTableName;
        localizedString.TableEntryReference = key;
        return localizedString;
    }
    
    #endregion
}