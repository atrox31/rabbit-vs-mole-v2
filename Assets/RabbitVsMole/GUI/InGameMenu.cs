using Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

namespace RabbitVsMole
{
    /// <summary>
    /// Manages the in-game pause menu that appears during gameplay.
    /// Handles menu visibility, input detection, and game state management.
    /// </summary>
    public class InGameMenu : MonoBehaviour
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
        LocalizedString GetLocalizedString(string key) => new LocalizedString(_localizationTableName, key);
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
            _mainMenuRestartQuestion = _menuManager.CreatePanel(GetLocalizedString("menu_restart_question"))
                .AddLabel(GetLocalizedString("text_restart_question"))
                .AddButton(GetLocalizedString("button_yes"), OnRestartConfirmed)
                .AddButton(GetLocalizedString("button_no"), _menuManager.GoBack)
                .Build();

            _mainMenuBackToMenuQuestion = _menuManager.CreatePanel(GetLocalizedString("menu_back_to_main_menu_question"))
                .AddLabel(GetLocalizedString("text_back_to_main_menu_question"))
                .AddButton(GetLocalizedString("button_yes"), OnBackToMainMenuConfirmed)
                .AddButton(GetLocalizedString("button_no"), _menuManager.GoBack)
                .Build();

            _mainMenu = _menuManager.CreatePanel(GetLocalizedString("menu_in_game"))
                .AddButton(GetLocalizedString("button_resume"), () => HideMenu(_mainMenu))
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
                HideMenu(_mainMenu);
            }
            else
            {
                ShowMenu(_mainMenu);
            }
        }

        private void ShowMenu(GUIPanel menu)
        {
            if (GameManager.IsPaused) return;
            if (menu == null || _menuManager == null) return;
            if (_isMenuVisible && menu.IsVisible) return;

            _isMenuVisible = true;
            GameManager.Pause();

            if (!menu.gameObject.activeInHierarchy)
            {
                menu.gameObject.SetActive(true);
            }

            _menuManager.ChangePanel(menu);
        }

        private void HideMenu(GUIPanel menu)
        {
            if (!_isMenuVisible) return;

            _isMenuVisible = false;
            GameManager.Unpause();

            if (menu != null)
            {
                _menuManager.ClosePanel(menu);
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
            GameManager.GoToMainMenu();
        }

        #endregion

    }
}