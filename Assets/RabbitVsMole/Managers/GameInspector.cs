using DebugTools;
using GameSystems;
using PlayerManagementSystem;
using RabbitVsMole.Events;
using RabbitVsMole.GameData;
using RabbitVsMole.GameData.Mutator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitVsMole
{
    /// <summary>
    /// Manages a single game session.
    /// Tracks session state (carrot counts, timer), evaluates win conditions,
    /// and communicates with GameManager via events.
    /// </summary>
    public class GameInspector : MonoBehaviour
    {
        private const float LAST_SECONDS_IN_GAME = 10f;

        // Event fired when game ends with win result
        public event Action<WinConditionEvaluator.WinResult> OnGameEnded;

        [SerializeField] private GameUI gameUIPrefab;
        private GameUI _gameUIInstance;

        private GameModeData _currentGameMode;
        private GameStats _currentGameStats;
        private PlayerType _currentPlayerOnStory;
        private PlayerControlAgent _rabbitControlAgent;
        private PlayerControlAgent _moleControlAgent;
        private readonly int[] _carrotCount = new int[Enum.GetValues(typeof(PlayerType)).Length];
        private Coroutine _gameTimerCoroutine;

        // Public accessors for game state
        public GameUI GameUI => _gameUIInstance;
        public GameModeData CurrentGameMode => _currentGameMode;
        public GameStats GameStats => _currentGameStats;
        public PlayerType CurrentPlayerOnStory => _currentPlayerOnStory;
        public PlayerControlAgent RabbitControlAgent => _rabbitControlAgent;
        public PlayerControlAgent MoleControlAgent => _moleControlAgent;
        public int RabbitCarrotCount => _carrotCount[(int)PlayerType.Rabbit];
        public int MoleCarrotCount => _carrotCount[(int)PlayerType.Mole];
        public bool IsSplitScreen => _rabbitControlAgent == PlayerControlAgent.Human && 
                                     _moleControlAgent == PlayerControlAgent.Human;

        /// <summary>
        /// Initializes game statistics with optional mutators.
        /// </summary>
        public void InitializeGameStats(List<MutatorSO> mutatorList)
        {
            _currentGameStats = new GameStats();
            if (mutatorList != null && mutatorList.Count > 0)
            {
                foreach (var mutator in mutatorList)
                {
                    DebugHelper.Log(this, $"Apply mutator settings: {mutator.name}");
                    mutator.Apply(_currentGameStats);
                }
            }
        }

        /// <summary>
        /// Initializes the game inspector with settings from GameManager.
        /// </summary>
        public void Initialize(GameManager.PlayGameSettings playGameSettings)
        {
            InitializeGameStats(playGameSettings.gameMode.mutators);
            _currentGameMode = playGameSettings.gameMode;
            _currentPlayerOnStory = playGameSettings.playerTypeForStory;
            _rabbitControlAgent = playGameSettings.GetPlayerControlAgent(PlayerType.Rabbit);
            _moleControlAgent = playGameSettings.GetPlayerControlAgent(PlayerType.Mole);
        }

        private void Start()
        {
            if (_currentGameMode == null)
            {
                Debug.LogError("GameInspector: No GameModeData assigned!");
                return;
            }
        }

        private void OnDestroy()
        {
            if (_gameTimerCoroutine != null)
            {
                StopCoroutine(_gameTimerCoroutine);
            }
        }

        /// <summary>
        /// Starts the game session: initializes UI and starts timer.
        /// </summary>
        public void StartGame()
        {
            StartGameTimer();
            _gameUIInstance = Instantiate(gameUIPrefab).GetComponent<GameUI>();
            GameUI.SetInventoryVisible(PlayerType.Rabbit, RabbitControlAgent == PlayerControlAgent.Human);
            GameUI.SetInventoryVisible(PlayerType.Mole, MoleControlAgent == PlayerControlAgent.Human);
        }

        /// <summary>
        /// Stops the game timer.
        /// </summary>
        public void StopTimer()
        {
            if (_gameTimerCoroutine != null)
            {
                StopCoroutine(_gameTimerCoroutine);
                _gameTimerCoroutine = null;
            }
        }

        private void StartGameTimer()
        {
            if (_gameTimerCoroutine != null)
            {
                StopCoroutine(_gameTimerCoroutine);
            }

            _gameTimerCoroutine = StartCoroutine(GameTimer());
        }

        private IEnumerator GameTimer()
        {
            if (_currentGameMode == null)
            {
                DebugHelper.LogWarning(this, "GameInspector.GameTimer: CurrentGameMode is null.");
                yield break;
            }

            bool haveTimeLimit = _currentGameMode.timeLimitInMinutes > 0f;
            float timeLimitInSeconds = _currentGameMode.timeLimitInMinutes * 60f;
            float elapsedTime = 0f;
            int lastSecond = 0;

            DebugHelper.Log(this, $"Timer started for: {timeLimitInSeconds} seconds");

            while (true)
            {
                elapsedTime += Time.deltaTime;

                int currentSecond = Mathf.FloorToInt(elapsedTime);
                if (currentSecond != lastSecond)
                {
                    lastSecond = currentSecond;
                    UpdateTimeDisplay(elapsedTime, haveTimeLimit, timeLimitInSeconds);
                }

                if (haveTimeLimit && elapsedTime > timeLimitInSeconds)
                {
                    DebugHelper.Log(this, "Time limit reached");
                    HandleTimeEnd();
                    yield break;
                }

                yield return null;
            }
        }

        private void UpdateTimeDisplay(float elapsedTime, bool haveTimeLimit, float timeLimitInSeconds)
        {
            float displayTime = haveTimeLimit ? (timeLimitInSeconds - elapsedTime) : elapsedTime;
            displayTime = Mathf.Max(0, displayTime);

            int minutes = Mathf.FloorToInt(displayTime / 60);
            int seconds = Mathf.FloorToInt(displayTime % 60);

            bool isEndingTime = haveTimeLimit && displayTime < LAST_SECONDS_IN_GAME;

            EventBus.Publish(new TimeUpdateEvent
            {
                Minutes = minutes,
                Seconds = seconds,
                IsEndingTime = isEndingTime
            });
        }

        /// <summary>
        /// Called when a player picks a carrot.
        /// Updates count and checks win conditions.
        /// </summary>
        public void CarrotPicked(PlayerType player)
        {
            if (_currentGameMode == null)
            {
                DebugHelper.LogWarning(this, "GameInspector.CarrotPicked: CurrentGameMode is null.");
                return;
            }

            _carrotCount[(int)player]++;
            EventBus.Publish(new CarrotPickEvent
            {
                PlayerType = player,
                Count = _carrotCount[(int)player]
            });

            // Check if carrot goal reached
            if (_currentGameMode.carrotGoal > 0)
            {
                // Cooperation mode: check combined carrot count
                if (_currentGameMode.winCondition == GameModeWinCondition.Cooperation)
                {
                    int totalCarrots = _carrotCount[(int)PlayerType.Rabbit] + _carrotCount[(int)PlayerType.Mole];
                    if (totalCarrots >= _currentGameMode.carrotGoal)
                    {
                        HandleCarrotGoalReached();
                        return;
                    }
                }
                // Other modes: check individual carrot count
                else if (_carrotCount[(int)player] >= _currentGameMode.carrotGoal)
                {
                    HandleCarrotGoalReached();
                }
            }
        }

        /// <summary>
        /// Called when a player has a carrot stolen.
        /// Updates count and publishes event.
        /// </summary>
        public void CarrotStealed(PlayerType player)
        {
            if (_currentGameMode == null)
            {
                DebugHelper.LogWarning(this, "GameInspector.CarrotStealed: CurrentGameMode is null.");
                return;
            }

            _carrotCount[(int)player]--;
            EventBus.Publish(new CarrotPickEvent
            {
                PlayerType = player,
                Count = _carrotCount[(int)player]
            });
        }

        /// <summary>
        /// Handles game end when time limit is reached.
        /// Evaluates winner and triggers game end event.
        /// </summary>
        private void HandleTimeEnd()
        {
            var winResult = WinConditionEvaluator.EvaluateByTime(
                _currentGameMode, 
                RabbitCarrotCount, 
                MoleCarrotCount);

            TriggerGameEnd(winResult);
        }

        /// <summary>
        /// Handles game end when carrot goal is reached.
        /// Evaluates winner and triggers game end event.
        /// </summary>
        private void HandleCarrotGoalReached()
        {
            var winResult = WinConditionEvaluator.EvaluateByGoal(
                _currentGameMode, 
                RabbitCarrotCount, 
                MoleCarrotCount);

            TriggerGameEnd(winResult);
        }

        /// <summary>
        /// Triggers the game end event with the specified win result.
        /// </summary>
        private void TriggerGameEnd(WinConditionEvaluator.WinResult winResult)
        {
            DebugHelper.Log(this, $"Game ended. Winner: {winResult.winner}");
            OnGameEnded?.Invoke(winResult);
        }

#if UNITY_EDITOR

        void OnGUI()
        {
            List<(string label, Action action)> debugOptions = new ()
            {
                ("Carrot Rabbit", () => CarrotPicked(PlayerType.Rabbit)),
                ("Carrot Mole", () => CarrotPicked(PlayerType.Mole)),
            };

            for (int i = 0; i < debugOptions.Count; i++)
            {
                Rect buttonRect = new(10, 10 + (i * 60), 150, 50);
                if (GUI.Button(buttonRect, debugOptions[i].label))
                    debugOptions[i].action.Invoke();
            }
        }

#endif
    }
}