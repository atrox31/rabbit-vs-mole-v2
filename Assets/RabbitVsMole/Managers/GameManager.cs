using InputManager;
using PlayerManagementSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using WalkingImmersionSystem;

namespace RabbitVsMole
{
    public partial class GameManager : SingletonMonoBehaviour<GameManager>
    {
        private PlayerType _currentPlayerForStory;
        public static PlayerType CurrentPlayerForStory { get { return Instance._currentPlayerForStory; } private set { Instance._currentPlayerForStory = value; } }

        // Last used settings for current gameplay session, used by RestartGame
        private PlayGameSettings _lastPlayGameSettings;

        private DayOfWeek _currentDayOfWeek = DayOfWeek.Monday;
        public static DayOfWeek CurrentDayOfWeek { get { return Instance._currentDayOfWeek; } private set { Instance._currentDayOfWeek = value; } }

        [Header("Game Inspector")]
        [SerializeField] private GameObject GameInspectorPrefab;

        [Header("Music")]
        [SerializeField] private MusicPlaylistSO MusicForGameplay;
        [SerializeField] private AudioClip MusicForMainMenu;
        [SerializeField] private AudioClip MusicForVictory;
        [SerializeField] private AudioClip MusicForDefeat;
        [SerializeField] private AudioClip MusicForLoading;
        public enum MusicType { Gameplay, Victory, Defeat, MainMenu };

        // player stats and settings
        private Dictionary<DayOfWeek, bool> _goldenCarrotPickStateRabbit = new Dictionary<DayOfWeek, bool>();
        private Dictionary<DayOfWeek, bool> _goldenCarrotPickStateMole = new Dictionary<DayOfWeek, bool>();
        private Dictionary<DayOfWeek, bool> _playerStoryProgressRabbit = new Dictionary<DayOfWeek, bool>();
        private Dictionary<DayOfWeek, bool> _playerStoryProgressMole = new Dictionary<DayOfWeek, bool>();

        [Header("Debug")]
        [SerializeField] bool _debugResetGoldenCarrotState = false;

        protected override void Ready()
        {
            DebugHelper.Log(this, "GameManager: Awake started.");
            DialogueSystem.TriggerData.TD_GameManagerGet.OnGetValue = Get;
            DebugHelper.Log(this, "GameManager: Awake completed.");
        }

        public override void OnGameStart()
        {
            GoToMainMenu();
        }

        void OnDestroy()
        {
            DialogueSystem.TriggerData.TD_GameManagerGet.OnGetValue = null;
        }

        private IEnumerator Start()
        {
            LoadPlayerPrefs();
            yield return null;

            SoundConfigLoader.InitializeLoader();
            yield return null;

            if (_debugResetGoldenCarrotState)
            {
                ResetGoldenCarrotProgress(PlayerType.Rabbit);
                ResetGoldenCarrotProgress(PlayerType.Mole);
            }

            SceneLoader.SetOnSceneUnload(() =>
            {
                AudioManager.ClearSoundCache();
            });
        }

        private void LoadPlayerPrefs()
        {
            _goldenCarrotPickStateRabbit = GameProgressDataManager.LoadStatus(PlayerPrefsConst.RABBIT_GOLDEN_CARROT_DATA);
            _goldenCarrotPickStateMole = GameProgressDataManager.LoadStatus(PlayerPrefsConst.MOLE_GOLDEN_CARROT_DATA);
            _playerStoryProgressRabbit = GameProgressDataManager.LoadStatus(PlayerPrefsConst.RABBIT_STORY_PROGRESS);
            _playerStoryProgressMole = GameProgressDataManager.LoadStatus(PlayerPrefsConst.MOLE_STORY_PROGRESS);

            AudioManager.SetAmbientVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.AMBIENT_VOLUME, 1.0f));
            AudioManager.SetSFXVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.SFX_VOLUME, 1.0f));
            AudioManager.SetMusicVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.MUSIC_VOLUME, 0.35f));
            AudioManager.SetDialoguesVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.DIALOGUES_VOLUME, 1.0f));
            AudioManager.SetMasterVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.MASTER_VOLUME, 1.0f));
        }

        private void SavePlayerPrefs()
        {
            GameProgressDataManager.SaveStatus(_playerStoryProgressRabbit, PlayerPrefsConst.RABBIT_STORY_PROGRESS);
            GameProgressDataManager.SaveStatus(_playerStoryProgressMole, PlayerPrefsConst.MOLE_STORY_PROGRESS);
        }

        public static void PlayMusic(MusicType type)
        {
            switch (type)
            {
                case MusicType.Gameplay:
                    AudioManager.PlayMusicPlaylist(Instance.MusicForGameplay);
                    break;
                case MusicType.Victory:
                    AudioManager.PlayMusic(Instance.MusicForVictory);
                    break;
                case MusicType.Defeat:
                    AudioManager.PlayMusic(Instance.MusicForDefeat);
                    break;
                case MusicType.MainMenu:
                    AudioManager.PlayMusic(Instance.MusicForMainMenu);
                    break;
                default:
                    break;
            }
        }

        // --- GAMEPLAY --- //
        public static void GoldenCarrotPick()
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.GoldenCarrotPick: Instance is null.");
                return;
            }

            if (!GameInspector.IsActive)
            {
                DebugHelper.LogWarning(null, "GameManager.GoldenCarrotPick: GameInspector is not active.");
                return;
            }

            switch (GameInspector.CurrentPlayerOnStory)
            {
                case PlayerType.Rabbit:
                    Instance._goldenCarrotPickStateRabbit[Instance._currentDayOfWeek] = true;
                    break;
                case PlayerType.Mole:
                    Instance._goldenCarrotPickStateMole[Instance._currentDayOfWeek] = true;
                    break;
                default:
                    DebugHelper.LogWarning(null, "GameManager.GoldenCarrotPick: Unknown player type.");
                    break;
            }
        }
        internal static bool IsGoldenCarrotCollected(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.IsGoldenCarrotCollected: Instance is null.");
                return false;
            }

            switch (playerType)
            {
                case PlayerType.Rabbit:
                    return Instance._goldenCarrotPickStateRabbit[dayOfWeek];
                case PlayerType.Mole:
                    return Instance._goldenCarrotPickStateMole[dayOfWeek];
                default:
                    DebugHelper.LogWarning(null, "GameManager.IsGoldenCarrotCollected: Unknown player type.");
                    return false;
            }
        }

        /// <summary>
        /// Sets a specific index in the rabbit story progress array.
        /// </summary>
        public static void SetStoryProgress(DayOfWeek dayOfWeek, bool value)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.SetStoryProgress: Instance is null.");
                return;
            }
            if (!GameInspector.IsActive)
            {
                DebugHelper.LogWarning(null, "GameManager.SetStoryProgress: GameInspector is not active.");
                return;
            }

            switch (GameInspector.CurrentPlayerOnStory)
            {
                case PlayerType.Rabbit:
                    Instance._playerStoryProgressRabbit[dayOfWeek] = value;
                    break;
                case PlayerType.Mole:
                    Instance._playerStoryProgressMole[dayOfWeek] = value;
                    break;
                default:
                    DebugHelper.LogWarning(null, "GameManager.SetStoryProgress: Unknown player type.");
                    return;
            }

            Instance.SavePlayerPrefs();
        }

        /// <summary>
        /// Gets a specific index in the rabbit story progress array.
        /// </summary>
        public static bool GetStoryProgress(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.GetStoryProgress: Instance is null.");
                return false;
            }

            switch (playerType)
            {
                case PlayerType.Rabbit:
                    return Instance._playerStoryProgressRabbit[dayOfWeek];
                case PlayerType.Mole:
                    return Instance._playerStoryProgressMole[dayOfWeek];
                default:
                    DebugHelper.LogWarning(null, "GameManager.GetStoryProgress: Unknown player type.");
                    return false;
            }
        }

        public static void ResetStoryProgress(PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.ResetStoryProgress: Instance is null.");
                return;
            }
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        Instance._playerStoryProgressRabbit[(DayOfWeek)day] = false;
                    }
                    break;
                case PlayerType.Mole:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        Instance._playerStoryProgressMole[(DayOfWeek)day] = false;
                    }
                    break;
                default:
                    DebugHelper.LogWarning(null, "GameManager.ResetStoryProgress: Unknown player type.");
                    return;
            }
            Instance.SavePlayerPrefs();
        }

        public static void ResetGoldenCarrotProgress(PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.ResetGoldenCarrotProgress: Instance is null.");
                return;
            }
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        Instance._goldenCarrotPickStateRabbit[(DayOfWeek)day] = false;
                    }
                    break;
                case PlayerType.Mole:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        Instance._goldenCarrotPickStateMole[(DayOfWeek)day] = false;
                    }
                    break;
                default:
                    DebugHelper.LogWarning(null, "GameManager.ResetGoldenCarrotProgress: Unknown player type.");
                    return;
            }
            Instance.SavePlayerPrefs();
        }

        public static void CompliteCurrentDay()
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.CompliteCurrentDay: Instance is null.");
                return;
            }
            if (!GameInspector.IsActive)
            {
                DebugHelper.LogWarning(null, "GameManager.CompliteCurrentDay: GameInspector is not active.");
                return;
            }

            switch (GameInspector.CurrentPlayerOnStory)
            {
                case PlayerType.Rabbit:
                    SetStoryProgress(Instance._currentDayOfWeek, true);
                    break;
                case PlayerType.Mole:
                    SetStoryProgress(Instance._currentDayOfWeek, true);
                    break;
                default:
                    DebugHelper.LogWarning(null, "GameManager.CompliteCurrentDay: Unknown player type.");
                    return;
            }
        }

        /// <summary>
        /// Dialogue system questions
        /// </summary>
        public static object Get(string field)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.Get: Instance is null.");
                return null;
            }

            switch (field)
            {
                case "GoldenCarrotPicked":
                    return false;
                default:
                    DebugHelper.LogWarning(null, $"GameManager.Get: Unknown field '{field}'");
                    return null;
            }
        }

        private static string GetSceneTypeDescription(GameSceneManager.SceneType sceneType)
        {
            var field = sceneType.GetType().GetField(sceneType.ToString());
            if (field != null)
            {
                var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes.Length > 0)
                {
                    return ((DescriptionAttribute)attributes[0]).Description;
                }
            }
            return sceneType.ToString();
        }

        /// <summary>
        /// Starts the game with the specified game mode, map, day, player type, and control agents.
        /// </summary>
        /// <remarks>This method initializes the game by setting the current day and player type for the story, 
        /// loading the specified map, and configuring the game environment. It also sets up the game  inspector and spawns
        /// the players using the provided control agents.</remarks>
        /// <param name="gameMode">The game mode data that defines the rules and settings for the game.</param>
        /// <param name="map">The map to load for the game, represented as a scene type.</param>
        /// <param name="day">The day of the week to associate with the game session.</param>
        /// <param name="playerTypeForStory">The player type to use for story-related gameplay elements.</param>
        /// <param name="rabbitControlAgent">The control agent responsible for managing the rabbit player's actions.</param>
        /// <param name="moleControlAgent">The control agent responsible for managing the mole player's actions.</param>
        public static void PlayGame(GameModeData gameMode,
                GameSceneManager.SceneType map,
                DayOfWeek day,
                PlayerType playerTypeForStory,
                PlayerControlAgent rabbitControlAgent,
                PlayerControlAgent moleControlAgent)
        {
            var playGameSettings = new PlayGameSettings(
                gameMode: gameMode,
                map: map,
                day: day,
                playerTypeForStory: playerTypeForStory,
                rabbitControlAgent: rabbitControlAgent,
                moleControlAgent: moleControlAgent);

            bool isSplitScreen = playGameSettings.IsAllHumanAgents;

            if (isSplitScreen)
            {
                switch (InputDeviceManager.GamepadCount)
                {
                    case 0: PlayGameInternal(playGameSettings); return;
                    case 1: GetGamepadPlayerAndPlayGame(playGameSettings); return;
                    case 2:
                    default: PlayGameInternal(playGameSettings.SetGamepadForBoth()); return;
                }
            }
            else
            {
                PlayGameInternal(playGameSettings.SetGamepadForPlayer(playerTypeForStory));
            }
        }

        public static void CreateAgentController(PlayGameSettings playGameSettings, PlayerType playerType)
        {
            switch (playGameSettings.GetPlayerControlAgent(playerType))
            {
                case PlayerControlAgent.None:
                    break;
                case PlayerControlAgent.Human:
                    HumanAgentController.CreateInstance(playGameSettings, playerType);
                    break;
                case PlayerControlAgent.Bot:
                    BotAgentController.CreateInstance(playGameSettings, playerType);
                    break;
                case PlayerControlAgent.Online:
                    //OnlineAgentController.CreateInstance(playerType);
                    break;
                default:
                    break;
            }
        }

        private static void PlayGameInternal(PlayGameSettings playGameSettings)
        {
            // Remember settings used to start this gameplay session (for RestartGame)
            Instance._lastPlayGameSettings = playGameSettings;

            Instance._currentDayOfWeek = playGameSettings.day;
            Instance._currentPlayerForStory = playGameSettings.playerTypeForStory;
            GameSceneManager.ChangeScene(
                scene: playGameSettings.map,
                OnSceneLoad: (scene) =>
                {
                    DebugHelper.Log(Instance, "Play game -> On Scene Load");
                    Instantiate(Instance.GameInspectorPrefab, scene);
                    GameInspector.InicializeGameStats(null);
                    GameInspector.CurrentGameMode = playGameSettings.gameMode;
                    GameInspector.CurrentPlayerOnStory = playGameSettings.playerTypeForStory;
                    GameInspector.RabbitControlAgent = playGameSettings.GetPlayerControlAgent(PlayerType.Rabbit);
                    GameInspector.MoleControlAgent = playGameSettings.GetPlayerControlAgent(PlayerType.Mole);
                },
                OnSceneStart: () =>
                {
                    PlayMusic(MusicType.Gameplay);
                    CreateAgentController(playGameSettings, PlayerType.Rabbit);
                    CreateAgentController(playGameSettings, PlayerType.Mole);
                },
                OnSceneShow: () =>
                {
                    GameInspector.StartGame();
                });

            DebugHelper.Log(Instance, $"GameManager: Starting game for {playGameSettings.day}, Map: {GetSceneTypeDescription(playGameSettings.map)}[{playGameSettings.map}], Rabbit: {playGameSettings.GetPlayerControlAgent(PlayerType.Rabbit)}, Mole: {playGameSettings.GetPlayerControlAgent(PlayerType.Mole)}");
            DebugHelper.Log(Instance, playGameSettings.ToStringDebug());
        }

        private static void GetGamepadPlayerAndPlayGame(PlayGameSettings playGameSettings)
        {
            var menuInGame = FindFirstObjectByType<MainMenuSetup>();
            menuInGame?.ShowInputPrompt(PlayGameInternal, playGameSettings);
        }

        public static void GoToMainMenu()
        {
            GameSceneManager.ChangeScene(
                scene: GameSceneManager.SceneType.MainMenu,
                OnSceneLoad: null,
                OnSceneStart: () =>
                {
                    PlayMusic(MusicType.MainMenu);
                },
                OnSceneShow: () =>
                {
                    var rabbitVsMoleMenuSetup = FindFirstObjectByType<MainMenuSetup>();
                    rabbitVsMoleMenuSetup?.ShowMenu();

                });
        }

        public static void RestartGame()
        {
            if (Instance == null)
            {
                Debug.LogError("GameManager.RestartGame: Instance is null! Cannot restart game.");
                return;
            }

            if (GameInspector.CurrentGameMode == null)
            {
                Debug.LogError("GameManager.RestartGame: CurrentGameMode is null! Cannot restart game.");
                return;
            }

            // MainMenu cannot be restarted - it's not a gameplay scene
            GameSceneManager.SceneType sceneToRestart = GameSceneManager.CurrentScene;
            if (sceneToRestart == GameSceneManager.SceneType.MainMenu)
            {
                Debug.LogError("GameManager.RestartGame: Cannot restart MainMenu scene. CurrentScene is not set correctly or you are trying to restart from MainMenu.");
                return;
            }

            // Restart with the same settings that were used to start this gameplay session.
            // This preserves which player was using the gamepad without needing to ask again.
            PlayGameInternal(Instance._lastPlayGameSettings);
        }

        internal static bool GetMoleProgress(DayOfWeek dayOfWeek)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.GetMoleProgress: Instance is null.");
                return false;
            }
            return Instance._playerStoryProgressMole[dayOfWeek];
        }

        internal static void GamePlayTimeEnd()
        {
            throw new NotImplementedException();
        }

        internal static void GamePlayCarrotGoal()
        {
            throw new NotImplementedException();
        }

        private bool _isPaused;
        internal static void Pause()
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.Pause: Instance is null.");
                return;
            }
            Instance._isPaused = true;
            Time.timeScale = 0f;
        }

        internal static void Unpause()
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.Unpause: Instance is null.");
                return;
            }
            Instance._isPaused = false;
            Time.timeScale = 1f;
        }
        internal static bool IsPaused => Instance != null && Instance._isPaused;
    }
}