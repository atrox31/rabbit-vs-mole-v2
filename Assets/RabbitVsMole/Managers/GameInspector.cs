using GameSystems;
using NUnit.Framework;
using PlayerManagementSystem;
using RabbitVsMole.Events;
using RabbitVsMole.GameData;
using RabbitVsMole.GameData.Mutator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

namespace RabbitVsMole
{

    public class GameInspector : MonoBehaviour
    {
        private static float LAST_SECONDS_IN_GAME = 10f;
        private static GameInspector _instance;
        public static bool IsActive => _instance != null;

        GameModeData _currentGameMode;
        GameStats _currentGameStats;
        public static GameStats GameStats =>
            _instance._currentGameStats;

        internal static void InicializeGameStats(List<MutatorSO> mutatotList)
        {
            if(!IsActive) return;

            _instance._currentGameStats = new GameStats();
            if(mutatotList != null && mutatotList.Count > 0)
            {
                foreach (var item in mutatotList)
                {
                    item.Apply(_instance._currentGameStats);
                }
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            DebugHelper.Log(this, "GameInspektor is alive!");
            _instance = this;
        }

        [SerializeField] GameUI gameUI;
        private GameUI _instanceOoGameUI;
        public static GameUI GameUI =>
            _instance?._instanceOoGameUI;

        public static GameModeData CurrentGameMode
        {
            get
            {
                if (_instance == null) return null;
                return _instance._currentGameMode;
            }
            set
            {
                if (_instance == null) return;
                _instance._currentGameMode = value;
            }
        }

        private int[] CarrotCount = new int[Enum.GetValues(typeof(PlayerType)).Length];
        private PlayerType _currentPlayerOnStory;
        public static PlayerType CurrentPlayerOnStory
        {
            get
            {
                if (_instance == null) return PlayerType.Rabbit; // Default value
                return _instance._currentPlayerOnStory;
            }
            set
            {
                if (_instance == null) return;
                _instance._currentPlayerOnStory = value;
            }
        }

        private PlayerControlAgent _rabbitControlAgent;
        private PlayerControlAgent _moleControlAgent;
        public static PlayerControlAgent RabbitControlAgent
        {
            get
            {
                if (_instance == null) return PlayerControlAgent.None;
                return _instance._rabbitControlAgent;
            }
            set
            {
                if (_instance == null) return;
                _instance._rabbitControlAgent = value;
            }
        }
        public static PlayerControlAgent MoleControlAgent
        {
            get
            {
                if (_instance == null) return PlayerControlAgent.None;
                return _instance._moleControlAgent;
            }
            set
            {
                if (_instance == null) return;
                _instance._moleControlAgent = value;
            }
        }

        public static bool IsSplitScreen
        {
            get
            {
                if (_instance == null) return false;
                return _instance._rabbitControlAgent == PlayerControlAgent.Human &&
                       _instance._moleControlAgent == PlayerControlAgent.Human;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private Coroutine _gameTimerCoroutine;

        void Start()
        {
            if (_currentGameMode == null)
            {
                Debug.LogError("GameInspector: No GameModeData assigned!");
                return;
            }
        }

        private void StartGameTimer()
        {
            if (_instance == null)
            {
                DebugHelper.LogWarning(null, "GameInspector.StartGameTimer: Instance is null.");
                return;
            }
            if (_instance._gameTimerCoroutine != null)
            {
                _instance.StopCoroutine(_instance._gameTimerCoroutine);
            }

            _instance._gameTimerCoroutine = _instance.StartCoroutine(_instance.GameTimer());
        }

        IEnumerator GameTimer()
        {
            if (_instance == null)
            {
                DebugHelper.LogWarning(this, "GameInspector.GameTimer: Instance is null.");
                yield break;
            }

            if (CurrentGameMode == null)
            {
                DebugHelper.LogWarning(this, "GameInspector.GameTimer: CurrentGameMode is null.");
                yield break;
            }

            bool haveTimeLimit = _currentGameMode.timeLimitInMinutes > 0f;
            float timeLimitInSeconds = _currentGameMode.timeLimitInMinutes * 60f;
            float elapsedTime = 0f;
            int lastSecond = 0;

            DebugHelper.Log(this, $"Count down started for: {timeLimitInSeconds} secconds");
            while (true)
            {
                elapsedTime += Time.deltaTime;

                int currentSecond = Mathf.FloorToInt(elapsedTime);
                if (currentSecond != lastSecond)
                {
                    lastSecond = currentSecond;
                    UpdateTimeDisplay(elapsedTime, haveTimeLimit, timeLimitInSeconds);
                }

                if (haveTimeLimit)
                {
                    if (elapsedTime > timeLimitInSeconds)
                    {
                        DebugHelper.Log(this, $"Time end");
                        GameManager.GamePlayTimeEnd();
                        yield break;
                    }
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

        public static void CarrotPicked(PlayerType player)
        {
            if (_instance == null)
            {
                DebugHelper.LogWarning(null, "GameInspector.CarrotPicked: Instance is null.");
                return;
            }

            if (CurrentGameMode == null)
            {
                DebugHelper.LogWarning(null, "GameInspector.CarrotPicked: CurrentGameMode is null.");
                return;
            }

            _instance.CarrotCount[(int)player]++;
            EventBus.Publish(new CarrotPickEvent
            {
                PlayerType = player,
                Count = _instance.CarrotCount[(int)player]
            });

            if (CurrentGameMode.carrotGoal == 0)
                return;

            if (_instance.CarrotCount[(int)player] >= CurrentGameMode.carrotGoal)
            {
                GameManager.GamePlayCarrotGoal();
            }
        }

        public static void CarrotStealed(PlayerType player)
        {
            if (_instance == null)
            {
                DebugHelper.LogWarning(null, "GameInspector.CarrotPicked: Instance is null.");
                return;
            }

            if (CurrentGameMode == null)
            {
                DebugHelper.LogWarning(null, "GameInspector.CarrotPicked: CurrentGameMode is null.");
                return;
            }

            _instance.CarrotCount[(int)player]--;
            EventBus.Publish(new CarrotPickEvent
            {
                PlayerType = player,
                Count = _instance.CarrotCount[(int)player]
            });
        }

        internal static void StartGame()
        {
            if (_instance == null)
                return;

            _instance.StartGameTimer();
            _instance._instanceOoGameUI = Instantiate(_instance.gameUI).GetComponent<GameUI>();
            GameUI.SetInventoryVisible(PlayerType.Rabbit, RabbitControlAgent == PlayerControlAgent.Human);
            GameUI.SetInventoryVisible(PlayerType.Mole, MoleControlAgent == PlayerControlAgent.Human);
        }

        internal static void Inicialize(List<MutatorSO> mutatotList, GameManager.PlayGameSettings playGameSettings)
        {
            InicializeGameStats(mutatotList);
            CurrentGameMode = playGameSettings.gameMode;
            CurrentPlayerOnStory = playGameSettings.playerTypeForStory;
            RabbitControlAgent = playGameSettings.GetPlayerControlAgent(PlayerType.Rabbit);
            MoleControlAgent = playGameSettings.GetPlayerControlAgent(PlayerType.Mole);
        }
    }
}