using System;
using System.Collections;
using UnityEngine;

public class GameInspector : MonoBehaviour
{
    private static GameInspector _instance;
    public static bool IsActive => _instance != null;

    GameModeData _currentGameMode;
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

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        var a = Enum.GetValues(typeof(PlayerType)).Length;
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Start()
    {
        if(_currentGameMode == null)
        {
            Debug.LogError("GameInspector: No GameModeData assigned!");
            return;
        }

        if(CurrentGameMode.timeLimitInMinutes > 0)      
            StartCoroutine(GameTimer());
        
    }

    IEnumerator GameTimer()
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameInspector.CarrotPicked: Instance is null.");
            yield break;
        }

        if (CurrentGameMode == null)
        {
            Debug.LogWarning("GameInspector.CarrotPicked: CurrentGameMode is null.");
            yield break;
        }

        if (_currentGameMode.timeLimitInMinutes <= 0.1f)
            yield break;
        
        yield return new WaitForSeconds(_currentGameMode.timeLimitInMinutes);

        Debug.Log($"Time end");
        GameManager.GamePlayTimeEnd();
    }

    public static void CarrotPicked(PlayerType player)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameInspector.CarrotPicked: Instance is null.");
            return;
        }

        if (CurrentGameMode == null)
        {
            Debug.LogWarning("GameInspector.CarrotPicked: CurrentGameMode is null.");
            return;
        }

        Debug.Log($"{player} picked the carrot!");

        if(CurrentGameMode.carrotGoal == 0)
            return;

        _instance.CarrotCount[(int)player]++;
        if(_instance.CarrotCount[(int)player] >= CurrentGameMode.carrotGoal)
        {
            GameManager.GamePlayCarrotGoal();
        }
    }
}