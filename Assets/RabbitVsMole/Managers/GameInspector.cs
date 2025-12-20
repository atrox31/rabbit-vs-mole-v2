using PlayerManagementSystem;
using System;
using System.Collections;
using UnityEngine;

namespace RabbitVsMole
{
    public class GameInspector : MonoBehaviour
    {
        private static GameInspector _instance;
        public static bool IsActive => _instance != null;

        GameModeData _currentGameMode;
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
            var a = Enum.GetValues(typeof(PlayerType)).Length;
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

        public static void StartGameTimer()
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

            if (CurrentGameMode.timeLimitInMinutes > 0)
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

            if (_currentGameMode.timeLimitInMinutes <= 0.1f)
                yield break;

            float timeLimitInSeconds = _currentGameMode.timeLimitInMinutes * 60f;
            float elapsedTime = 0f;

            DebugHelper.Log(this, $"Count down started for: {timeLimitInSeconds} secconds");
            // Count down using Time.deltaTime, which respects Time.timeScale
            // When Time.timeScale = 0 (paused), Time.deltaTime = 0, so timer stops
            while (elapsedTime < timeLimitInSeconds)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            DebugHelper.Log(this, $"Time end");
            GameManager.GamePlayTimeEnd();
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

            DebugHelper.Log(null, $"{player} picked the carrot!");

            if (CurrentGameMode.carrotGoal == 0)
                return;

            _instance.CarrotCount[(int)player]++;
            if (_instance.CarrotCount[(int)player] >= CurrentGameMode.carrotGoal)
            {
                GameManager.GamePlayCarrotGoal();
            }
        }
    }
}