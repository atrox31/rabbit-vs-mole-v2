using GameSystems.Steam;
using GameSystems.Steam.Scripts;
using AddressablesStaticDictionary;
using InputManager;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using PlayerManagementSystem.Backpack.Events;
using RabbitVsMole.Events;
using RabbitVsMole.GameData;
using RabbitVsMole.InteractableGameObject.Enums;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization;
using WalkingImmersionSystem;
using EventBus = GameSystems.EventBus;

namespace RabbitVsMole
{
    /// <summary>
    /// Core game manager singleton that orchestrates game flow.
    /// Manages scene transitions, game sessions, and coordinates between subsystems.
    /// Lives in DontDestroyOnLoad scene for the entire game lifecycle.
    /// </summary>
    public partial class GameManager : SingletonMonoBehaviour<GameManager>
    {
        private PlayerType _currentPlayerForStory;
        public static PlayerType CurrentPlayerForStory 
        { 
            get => Instance._currentPlayerForStory; 
            private set => Instance._currentPlayerForStory = value; 
        }

        private DayOfWeek _currentDayOfWeek = DayOfWeek.Monday;
        public static DayOfWeek CurrentDayOfWeek 
        { 
            get => Instance._currentDayOfWeek; 
            private set => Instance._currentDayOfWeek = value; 
        }

        // Last used settings for current gameplay session, used by RestartGame
        private PlayGameSettings _lastPlayGameSettings;
        private bool _endGame = false;
        private bool _isPaused;

        [Header("Game Inspector")]
        [SerializeField] private GameObject gameInspectorPrefab;
        private GameInspector _currentGameInspector;

        // Client-side: we need a visual proxy avatar for the remote host player.
        // This loads the same character prefabs AgentController uses, but without creating a controller.
        private static readonly AddressablesStaticDictionary<PlayerType> _onlineCharacterPrefabs =
            new("Assets/Prefabs/Agents/Characters/", ".prefab");

        /// <summary>
        /// Static accessor for the current game inspector instance.
        /// Returns null when not in a game session.
        /// </summary>
        public static GameInspector CurrentGameInspector => Instance?._currentGameInspector;

        /// <summary>
        /// Convenient shortcut to current game stats.
        /// Returns null when not in a game session.
        /// </summary>
        public static GameStats CurrentGameStats => CurrentGameInspector?.GameStats;

        /// <summary>
        /// Convenient shortcut to current game mode.
        /// Returns null when not in a game session.
        /// </summary>
        public static GameModeData CurrentGameMode => CurrentGameInspector?.CurrentGameMode;

        private GameProgressManager progressManager;
        private GameAudioManager gameAudioManager;

        [Header("Localization")]
        [SerializeField] private string localizationTableName = "Interface";

        private string GetLocalizedString(string key) => 
            new LocalizedString(localizationTableName, key).GetLocalizedString();

        protected override void Ready()
        {
            DebugHelper.Log(this, "GameManager: Ready started.");
            
            // Steam achievements/statistics watcher (singleton) + manual registration of achievements from code.
            AchievementsWatcher.Initialize(
                achievements: new IAchievement[]
                {
                // Carrot stored first time (diff -1 when putting into storage)
                new EventAchievement<InventoryChangedEvent>(
                    "ACHIEVEMENT_CARROT_1",
                    e =>
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        if (e.BackpackItemType == BackpackItemType.Carrot && e.Diff == -1)
                            DebugHelper.Log(this, $"[Achievements] Carrot put down -> InventoryChangedEvent diff={e.Diff} count={e.Count}/{e.Capacity}");
#endif
                        return e.BackpackItemType == BackpackItemType.Carrot && e.Diff == -1;
                    }),

                // Carrot 100 progress via Steam stat (0..100)
                new StatProgressAchievement<InventoryChangedEvent>(
                    "ACHIEVEMENT_CARROT_100",
                    statName: "CARROT100",
                    max: 100,
                    incrementPerTrigger: 1,
                    condition: e =>
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        if (e.BackpackItemType == BackpackItemType.Carrot && e.Diff == -1)
                            DebugHelper.Log(this, $"[Achievements] Carrot100 progress trigger -> diff={e.Diff} count={e.Count}/{e.Capacity}");
#endif
                        return e.BackpackItemType == BackpackItemType.Carrot && e.Diff == -1;
                    }),

                // Seed achievements (use/plant consumes seeds -> diff < 0)
                new EventAchievement<InventoryChangedEvent>(
                    "ACHIEVEMENT_SEED_1",
                    e => e.BackpackItemType == BackpackItemType.Seed && e.Diff < 0),
                new StatProgressAchievement<InventoryChangedEvent>(
                    "ACHIEVEMENT_SEED_100",
                    statName: "SEED100",
                    max: 100,
                    incrementPerTrigger: 1,
                    condition: e => e.BackpackItemType == BackpackItemType.Seed && e.Diff < 0),

                // Water achievements (watering consumes water -> diff < 0)
                new EventAchievement<InventoryChangedEvent>(
                    "ACHIEVEMENT_WATER_1",
                    e => e.BackpackItemType == BackpackItemType.Water && e.Diff < 0),
                new StatProgressAchievement<InventoryChangedEvent>(
                    "ACHIEVEMENT_WATER_100",
                    statName: "WATER100",
                    max: 100,
                    incrementPerTrigger: 1,
                    condition: e => e.BackpackItemType == BackpackItemType.Water && e.Diff < 0),

                // Fast carrot: pick a carrot within first 30 seconds of the match (carrot added to inventory)
                new EventAchievement<InventoryChangedEvent>(
                    "ACHIEVEMENT_FAST_CARROT",
                    e => e.BackpackItemType == BackpackItemType.Carrot
                         && e.Diff > 0
                         && CurrentGameInspector != null
                         && CurrentGameInspector.CurrentGameTime <= 30),

                // Golden carrots
                new EventAchievement<GoldenCarrotCollectedEvent>(
                    "ACHIEVEMENT_GOLDER_CARROT_1",
                    _ => true),
                // NOTE: Requires Steam stat configured on Steamworks side: GOLDEN_CARROT_COUNT (0..7)
                new StatProgressAchievement<GoldenCarrotCollectedEvent>(
                    "ACHIEVEMENT_GOLDER_CARROT_7",
                    statName: "GOLDEN_CARROT_COUNT",
                    max: 7,
                    incrementPerTrigger: 1,
                    condition: _ => true),

                // Wins per mode (only when local player wins; disabled in splitscreen by watcher)
                new EventAchievement<GameEndedEvent>(
                    "ACHIEVEMENT_WIN_RIVALRY",
                    e => e.WinCondition == GameModeWinCondition.Rivalry && DidLocalPlayerWin(e)),
                new EventAchievement<GameEndedEvent>(
                    "ACHIEVEMENT_WIN_TIME_ATTACK",
                    e => e.WinCondition == GameModeWinCondition.TimeLimit && DidLocalPlayerWin(e)),
                new EventAchievement<GameEndedEvent>(
                    "ACHIEVEMENT_WIN_COOPERATION",
                    e => e.WinCondition == GameModeWinCondition.Cooperation && DidLocalPlayerWin(e)),
                new EventAchievement<GameEndedEvent>(
                    "ACHIEVEMENT_WIN_CARROT_RACE",
                    e => e.WinCondition == GameModeWinCondition.CarrotCollection && DidLocalPlayerWin(e)),
                },
                isTrackingAllowed: AchievementsEnabled);

            // Initialize managers
            if (!gameObject.TryGetComponent(out progressManager))
            {
                DebugHelper.LogWarning(this, "GameManager: GameProgressManager not found, adding default but expect some errors.");
                progressManager = gameObject.AddComponent<GameProgressManager>();
            }
            
            if (!gameObject.TryGetComponent(out gameAudioManager))
            {
                DebugHelper.LogWarning(this, "GameManager: GameAudioManager not found, adding default but expect some errors.");
                gameAudioManager = gameObject.AddComponent<GameAudioManager>();
            }
            
            DebugHelper.Log(this, "GameManager: Ready completed.");
        }

        private static bool DidLocalPlayerWin(GameEndedEvent e)
        {
            return e.WinResult.winner switch
            {
                WinConditionEvaluator.Winner.Rabbit => e.RabbitIsLocal,
                WinConditionEvaluator.Winner.Mole => e.MoleIsLocal,
                WinConditionEvaluator.Winner.Both => e.RabbitIsLocal || e.MoleIsLocal,
                _ => false,
            };
        }

        /// <summary>
        /// Global switch for Steam achievements/stats tracking.
        /// Enabled only for Story / vs AI / Online; disabled in local splitscreen.
        /// </summary>
        public static bool AchievementsEnabled()
        {
            var inspector = CurrentGameInspector;
            if (inspector == null)
                return false;

            if (inspector.IsSplitScreen)
                return false;

            // Ignore bot-vs-bot / test sessions where nobody is actually playing.
            bool rabbitCounts = inspector.RabbitControlAgent == PlayerControlAgent.Human
                                || inspector.RabbitControlAgent == PlayerControlAgent.Online;
            bool moleCounts = inspector.MoleControlAgent == PlayerControlAgent.Human
                              || inspector.MoleControlAgent == PlayerControlAgent.Online;
            return rabbitCounts || moleCounts;
        }

        public override void OnGameStart()
        {
            GoToMainMenu();
        }

        private IEnumerator Start()
        {
            // Load progress and audio settings
            progressManager.LoadProgress();
            yield return null;

            SoundConfigLoader.InitializeLoader();
            yield return null;

            // Setup audio cache clearing on scene unload
            SceneLoader.SetOnSceneUnload(() =>
            {
                AudioManager.ClearSoundCache();
            });
        }

        #region Game Flow Management

        /// <summary>
        /// Starts a new game with the specified settings.
        /// </summary>
        public static void PlayGame(
            GameModeData gameMode,
            GameSceneManager.SceneType map,
            DayOfWeek day,
            PlayerType playerTypeForStory,
            PlayerControlAgent rabbitControlAgent,
            PlayerControlAgent moleControlAgent,
            int aiIntelligence = 90,
            PlayGameSettings.OnlineConfig onlineConfig = default)
        {
            var playGameSettings = new PlayGameSettings(
                gameMode: gameMode,
                map: map,
                day: day,
                playerTypeForStory: playerTypeForStory,
                rabbitControlAgent: rabbitControlAgent,
                moleControlAgent: moleControlAgent,
                aiIntelligence: aiIntelligence,
                onlineConfig: onlineConfig);

            bool isSplitScreen = playGameSettings.IsAllHumanAgents;

            if (isSplitScreen)
            {
                switch (InputDeviceManager.GamepadCount)
                {
                    case 0: 
                        PlayGameInternal(playGameSettings); 
                        return;
                    case 1: 
                        GetGamepadPlayerAndPlayGame(playGameSettings); 
                        return;
                    case 2:
                    default: 
                        PlayGameInternal(playGameSettings.SetGamepadForBoth()); 
                        return;
                }
            }
            else
            {
                PlayGameInternal(playGameSettings.SetGamepadForPlayer(playerTypeForStory));
            }
        }

        private static void PlayGameInternal(PlayGameSettings playGameSettings)
        {
            // Remember settings used to start this gameplay session (for RestartGame)
            Instance._lastPlayGameSettings = playGameSettings;
            Instance._endGame = false;

            Instance._currentDayOfWeek = playGameSettings.day;
            Instance._currentPlayerForStory = playGameSettings.playerTypeForStory;

            GameSceneManager.ChangeScene(
                scene: playGameSettings.map,
                OnSceneLoad: (scene) =>
                {
                    DebugHelper.Log(Instance, "Play game -> On Scene Load");
                    
                    // Instantiate GameInspector
                    var inspectorObj = Instantiate(Instance.gameInspectorPrefab, scene);
                    Instance._currentGameInspector = inspectorObj.GetComponent<GameInspector>();
                    
                    if (Instance._currentGameInspector != null)
                    {
                        // Initialize GameInspector
                        Instance._currentGameInspector.Initialize(playGameSettings);
                        
                        // Subscribe to game end event
                        Instance._currentGameInspector.OnGameEnded += Instance.HandleGameEnd;
                    }
                    else
                    {
                        Debug.LogError("GameManager: GameInspector component not found on prefab!");
                    }
                },
                OnSceneStart: () =>
                {
                    Instance.gameAudioManager.PlayMusic(GameAudioManager.MusicType.Gameplay);
                    CreateAgentController(playGameSettings, PlayerType.Rabbit);
                    CreateAgentController(playGameSettings, PlayerType.Mole);

                    EnsureRemoteAvatarProxyIfNeeded(playGameSettings);
                },
                OnSceneShow: () =>
                {
                    if (Instance._currentGameInspector == null)
                        return;

                    // Online: pause after load and wait for both peers to load, then start together.
                    if (playGameSettings.onlineConfig.IsOnline)
                    {
                        // Ensure endgame watcher exists during online gameplay.
                        _ = GameSystems.Steam.Scripts.SteamOnlineEndgameWatcher.Instance;
                        GameSystems.Steam.Scripts.SteamOnlineStartCoordinator.Instance.OnGameplaySceneShown();
                        return;
                    }

                    Instance._currentGameInspector.StartGame();
                });

            DebugHelper.Log(Instance, $"GameManager: Starting game for {playGameSettings.day}, " +
                $"Map: [{playGameSettings.map}], " +
                $"Rabbit: {playGameSettings.GetPlayerControlAgent(PlayerType.Rabbit)}, " +
                $"Mole: {playGameSettings.GetPlayerControlAgent(PlayerType.Mole)}");
        }

        private static void GetGamepadPlayerAndPlayGame(PlayGameSettings playGameSettings)
        {
            var menuInGame = FindFirstObjectByType<MainMenuSetup>();
            menuInGame?.ShowInputPrompt(PlayGameInternal, playGameSettings);
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
                    OnlineAgentController.CreateInstance(playGameSettings, playerType);
                    break;
                default:
                    break;
            }
        }

        private static void EnsureRemoteAvatarProxyIfNeeded(PlayGameSettings playGameSettings)
        {
            if (!playGameSettings.onlineConfig.IsOnline || playGameSettings.onlineConfig.IsHost)
                return;

            // Client: local player is playerTypeForStory, remote is the other one.
            var local = playGameSettings.playerTypeForStory;
            var remote = local == PlayerType.Rabbit ? PlayerType.Mole : PlayerType.Rabbit;

            if (remote == PlayerType.Rabbit && PlayerAvatar.RabbitStaticInstance != null)
                return;
            if (remote == PlayerType.Mole && PlayerAvatar.MoleStaticInstance != null)
                return;

            var prefab = _onlineCharacterPrefabs.GetPrefab(remote);
            if (prefab == null)
            {
                Debug.LogError($"Online: character prefab for remote player '{remote}' not found (Addressables)");
                return;
            }

            var spawn = PlayerSpawnPoint<PlayerType>.FindSpawnPoint(remote);
            var pos = spawn != null ? spawn.position : Vector3.zero;
            var rot = spawn != null ? spawn.rotation : Quaternion.identity;

            var go = Instantiate(prefab, pos, rot);
            go.name = $"RemoteAvatarProxy ({remote})";
        }

        public static void GoToMainMenu()
        {
            GameSceneManager.ChangeScene(
                scene: GameSceneManager.SceneType.MainMenu,
                OnSceneLoad: (scene) =>
                {
                    // Reset game inspector reference when leaving game scene
                    if (Instance._currentGameInspector != null)
                    {
                        Instance._currentGameInspector.OnGameEnded -= Instance.HandleGameEnd;
                        Instance._currentGameInspector = null;
                    }
                },
                OnSceneStart: () =>
                {
                    Instance.gameAudioManager.PlayMusic(GameAudioManager.MusicType.MainMenu);
                },
                OnSceneShow: () =>
                {
                    var rabbitVsMoleMenuSetup = FindFirstObjectByType<MainMenuSetup>();
                    rabbitVsMoleMenuSetup?.ShowMenu();
                });
        }

        public static void GoToOnlineDuelLobbyList()
        {
            GameSceneManager.ChangeScene(
                scene: GameSceneManager.SceneType.MainMenu,
                OnSceneLoad: (scene) =>
                {
                    // Reset game inspector reference when leaving game scene
                    if (Instance._currentGameInspector != null)
                    {
                        Instance._currentGameInspector.OnGameEnded -= Instance.HandleGameEnd;
                        Instance._currentGameInspector = null;
                    }
                },
                OnSceneStart: () =>
                {
                    Instance.gameAudioManager.PlayMusic(GameAudioManager.MusicType.MainMenu);
                },
                OnSceneShow: () =>
                {
                    var rabbitVsMoleMenuSetup = FindFirstObjectByType<MainMenuSetup>();
                    rabbitVsMoleMenuSetup?.ShowOnlineDuelList();
                });
        }

        public static void RestartGame()
        {
            if (Instance == null)
            {
                Debug.LogError("GameManager.RestartGame: Instance is null! Cannot restart game.");
                return;
            }

            if (Instance._currentGameInspector?.CurrentGameMode == null)
            {
                Debug.LogError("GameManager.RestartGame: CurrentGameMode is null! Cannot restart game.");
                return;
            }

            // MainMenu cannot be restarted - it's not a gameplay scene
            GameSceneManager.SceneType sceneToRestart = GameSceneManager.CurrentScene;
            if (sceneToRestart == GameSceneManager.SceneType.MainMenu)
            {
                Debug.LogError("GameManager.RestartGame: Cannot restart MainMenu scene.");
                return;
            }

            // Restart with the same settings that were used to start this gameplay session
            PlayGameInternal(Instance._lastPlayGameSettings);
        }

        #endregion

        #region Game End Handling

        /// <summary>
        /// Handles game end event from GameInspector.
        /// </summary>
        private void HandleGameEnd(WinConditionEvaluator.WinResult winResult) =>
            HandleGameEndInternal(winResult, fromNetwork: false);

        internal static bool IsGameEnded => Instance != null && Instance._endGame;

        internal static void ApplyRemoteGameEnd(WinConditionEvaluator.WinResult winResult)
        {
            if (Instance == null) return;
            Instance.HandleGameEndInternal(winResult, fromNetwork: true);
        }

        internal static void ForceWinForLocalPlayer()
        {
            if (Instance == null || Instance._currentGameInspector == null) return;
            var local = Instance._currentGameInspector.CurrentPlayerOnStory;
            Instance.HandleGameEndInternal(WinConditionEvaluator.GetWinner(local), fromNetwork: true);
        }

        private void HandleGameEndInternal(WinConditionEvaluator.WinResult winResult, bool fromNetwork)
        {
            // Online: only host decides. Client ignores local evaluation and waits for host.
            if (!fromNetwork && _currentGameInspector != null && _currentGameInspector.IsOnlineSession && !_currentGameInspector.IsOnlineHost)
                return;

            if (_endGame)
                return;

            _endGame = true;

            // Online: publish result from host to client via lobby data.
            if (!fromNetwork && _currentGameInspector != null && _currentGameInspector.IsOnlineSession && _currentGameInspector.IsOnlineHost)
            {
                try
                {
                    SteamLobbySession.Instance.HostPublishGameEnd(winResult.winner);
                }
                catch { }
            }

            // Publish a typed event for systems like Steam achievements.
            // AchievementsWatcher will ignore this in splitscreen sessions.
            if (_currentGameInspector?.CurrentGameMode != null)
            {
                bool rabbitLocal = _currentGameInspector.RabbitControlAgent == PlayerControlAgent.Human
                                   || _currentGameInspector.RabbitControlAgent == PlayerControlAgent.Online;
                bool moleLocal = _currentGameInspector.MoleControlAgent == PlayerControlAgent.Human
                                 || _currentGameInspector.MoleControlAgent == PlayerControlAgent.Online;

                EventBus.Publish(new GameEndedEvent
                {
                    WinCondition = _currentGameInspector.CurrentGameMode.winCondition,
                    WinResult = winResult,
                    RabbitIsLocal = rabbitLocal,
                    MoleIsLocal = moleLocal,
                });
            }

            var inGameMenu = FindAnyObjectByType<InGameMenu>();
            if (inGameMenu != null)
                inGameMenu.BlockMenu();

            if (_currentGameInspector != null)
                _currentGameInspector.StopTimer();

            bool DidPlayerWin(WinConditionEvaluator.Winner winner) => 
                winResult.winner == winner || winResult.winner == WinConditionEvaluator.Winner.Both;

            TriggerEndGameAnimation(PlayerAvatar.RabbitStaticInstance, DidPlayerWin(WinConditionEvaluator.Winner.Rabbit));
            TriggerEndGameAnimation(PlayerAvatar.MoleStaticInstance, DidPlayerWin(WinConditionEvaluator.Winner.Mole));

            // Play appropriate music
            if (winResult.winner != WinConditionEvaluator.Winner.None)
            {
                gameAudioManager.PlayMusic(GameAudioManager.MusicType.Victory);
            }
            else
            {
                gameAudioManager.PlayMusic(GameAudioManager.MusicType.Defeat);
            }

            // Show game over screen
            if (_currentGameInspector?.GameUI != null)
            {
                string winnerText = winResult.winner switch
                {
                    WinConditionEvaluator.Winner.None => GetLocalizedString("WinPlayer_None"),
                    WinConditionEvaluator.Winner.Rabbit => GetLocalizedString("WinPlayer_Rabbit"),
                    WinConditionEvaluator.Winner.Mole => GetLocalizedString("WinPlayer_Mole"),
                    WinConditionEvaluator.Winner.Both => GetLocalizedString("WinPlayer_Both"),
                    _ => "Error",
                };

                var title = GetLocalizedString("title_end_game");

                _currentGameInspector.GameUI.ShowGameOverScreen(winnerText, title);
            }
        }

        private void TriggerEndGameAnimation(PlayerAvatar player, bool isWinner)
        {
            if (player == null) return;

            ActionType finalAction = isWinner ? ActionType.Victory : ActionType.Defeat;
            player.PerformAction(finalAction, null, null, true);
        }

        #endregion

        #region Progress Management (Delegated to GameProgressManager)

        /// <summary>
        /// Marks golden carrot as collected for current player and day.
        /// </summary>
        public static void GoldenCarrotPick()
        {
            if (Instance == null || Instance._currentGameInspector == null)
            {
                DebugHelper.LogWarning(null, "GameManager.GoldenCarrotPick: Instance or GameInspector is null.");
                return;
            }

            var day = Instance._currentDayOfWeek;
            var player = Instance._currentGameInspector.CurrentPlayerOnStory;

            Instance.progressManager.SetGoldenCarrotCollected(
                day,
                player);

            EventBus.Publish(new GoldenCarrotCollectedEvent
            {
                DayOfWeek = day,
                PlayerType = player,
            });
        }

        /// <summary>
        /// Checks if golden carrot was collected for a specific day and player.
        /// </summary>
        public static bool IsGoldenCarrotCollected(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.IsGoldenCarrotCollected: Instance is null.");
                return false;
            }

            return Instance.progressManager.IsGoldenCarrotCollected(dayOfWeek, playerType);
        }

        /// <summary>
        /// Sets story progress for a specific day and player.
        /// </summary>
        public static void SetStoryProgress(DayOfWeek dayOfWeek, bool value)
        {
            if (Instance == null || Instance._currentGameInspector == null)
            {
                DebugHelper.LogWarning(null, "GameManager.SetStoryProgress: Instance or GameInspector is null.");
                return;
            }

            Instance.progressManager.SetStoryProgress(
                dayOfWeek, 
                Instance._currentGameInspector.CurrentPlayerOnStory, 
                value);
        }

        /// <summary>
        /// Gets story progress for a specific day and player.
        /// </summary>
        public static bool GetStoryProgress(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.GetStoryProgress: Instance is null.");
                return false;
            }

            return Instance.progressManager.GetStoryProgress(dayOfWeek, playerType);
        }

        /// <summary>
        /// Resets all story progress for a player.
        /// </summary>
        public static void ResetStoryProgress(PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.ResetStoryProgress: Instance is null.");
                return;
            }

            Instance.progressManager.ResetStoryProgress(playerType);
        }

        /// <summary>
        /// Resets all golden carrot progress for a player.
        /// </summary>
        public static void ResetGoldenCarrotProgress(PlayerType playerType)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.ResetGoldenCarrotProgress: Instance is null.");
                return;
            }

            Instance.progressManager.ResetGoldenCarrotProgress(playerType);
        }

        /// <summary>
        /// Marks the current day as completed.
        /// </summary>
        public static void CompleteCurrentDay()
        {
            if (Instance == null || Instance._currentGameInspector == null)
            {
                DebugHelper.LogWarning(null, "GameManager.CompleteCurrentDay: Instance or GameInspector is null.");
                return;
            }

            Instance.progressManager.CompleteCurrentDay(
                Instance._currentDayOfWeek, 
                Instance._currentGameInspector.CurrentPlayerOnStory);
        }

        /// <summary>
        /// Gets mole story progress for a specific day.
        /// </summary>
        internal static bool GetMoleProgress(DayOfWeek dayOfWeek)
        {
            if (Instance == null)
            {
                DebugHelper.LogWarning(null, "GameManager.GetMoleProgress: Instance is null.");
                return false;
            }

            return Instance.progressManager.GetStoryProgress(dayOfWeek, PlayerType.Mole);
        }

        #endregion

        #region Pause Management

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

        #endregion
    }
}