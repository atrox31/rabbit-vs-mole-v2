using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    private DayOfWeek _currentDayOfWeek = DayOfWeek.Monday;
    public static DayOfWeek CurrentDayOfWeek { get { return _instance._currentDayOfWeek; } private set { _instance._currentDayOfWeek = value; } }

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

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        DialogueSystem.TriggerData.TD_GameManagerGet.OnGetValue = Get;
    }

    void OnDestroy()
    {
        DialogueSystem.TriggerData.TD_GameManagerGet.OnGetValue = null;
    }

    private IEnumerator Start()
    {
        LoadPlayerPrefs();
        yield return null;

        if (_debugResetGoldenCarrotState)
        {
            ResetGoldenCarrotProgress(PlayerType.Rabbit);
            ResetGoldenCarrotProgress(PlayerType.Mole);
        }

        yield return new WaitForSecondsRealtime(5.0f);

        GameSceneManager.ChangeScene(GameSceneManager.SceneType.MainMenu);
    }

    private void LoadPlayerPrefs()
    {
        _goldenCarrotPickStateRabbit = GameProgressDataManager.LoadStatus(PlayerPrefsConst.RABBIT_GOLDEN_CARROT_DATA);
        _goldenCarrotPickStateMole = GameProgressDataManager.LoadStatus(PlayerPrefsConst.MOLE_GOLDEN_CARROT_DATA);
        _playerStoryProgressRabbit = GameProgressDataManager.LoadStatus(PlayerPrefsConst.RABBIT_STORY_PROGRESS);
        _playerStoryProgressMole = GameProgressDataManager.LoadStatus(PlayerPrefsConst.MOLE_STORY_PROGRESS);

        AudioManager.SetAmbientVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.AMBIENT_VOLUME, 1.0f));
        AudioManager.SetSFXVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.SFX_VOLUME, 1.0f));
        AudioManager.SetMusicVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.MUSIC_VOLUME, 1.0f));
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
                AudioManager.PlayMusicPlaylist(_instance.MusicForGameplay);
                break;
            case MusicType.Victory:
                AudioManager.PlayMusic(_instance.MusicForVictory);
                break;
            case MusicType.Defeat:
                AudioManager.PlayMusic(_instance.MusicForDefeat);
                break;
            case MusicType.MainMenu:
                AudioManager.PlayMusic(_instance.MusicForMainMenu);
                break;
            default:
                break;
        }
    }

    // --- GAMEPLAY --- //
    public static void GoldenCarrotPick()
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.GoldenCarrotPick: Instance is null.");
            return;
        }

        if (!GameInspector.IsActive)
        {
            Debug.LogWarning("GameManager.GoldenCarrotPick: GameInspector is not active.");
            return;
        }

        switch(GameInspector.CurrentPlayerOnStory)
        {
            case PlayerType.Rabbit:
                _instance._goldenCarrotPickStateRabbit[_instance._currentDayOfWeek] = true;
                break;
            case PlayerType.Mole:
                _instance._goldenCarrotPickStateMole[_instance._currentDayOfWeek] = true;
                break;
            default:
                Debug.LogWarning("GameManager.GoldenCarrotPick: Unknown player type.");
                break;
        }
    }
    internal static bool IsGoldenCarrotCollected(DayOfWeek dayOfWeek)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.IsGoldenCarrotCollected: Instance is null.");
            return false;
        }

        if (!GameInspector.IsActive)
        {
            Debug.LogWarning("GameManager.IsGoldenCarrotCollected: GameInspector is not active.");
            return false;
        }

        switch(GameInspector.CurrentPlayerOnStory)
        {
            case PlayerType.Rabbit:
                return _instance._goldenCarrotPickStateRabbit[dayOfWeek];
            case PlayerType.Mole:
                return _instance._goldenCarrotPickStateMole[dayOfWeek];
            default:
                Debug.LogWarning("GameManager.IsGoldenCarrotCollected: Unknown player type.");
                return false;
        }
    }

    /// <summary>
    /// Sets a specific index in the rabbit story progress array.
    /// </summary>
    public static void SetStoryProgress(DayOfWeek dayOfWeek, bool value)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.SetStoryProgress: Instance is null.");
            return;
        }
        if (!GameInspector.IsActive)
        {
            Debug.LogWarning("GameManager.SetStoryProgress: GameInspector is not active.");
            return;
        }

        switch (GameInspector.CurrentPlayerOnStory)
        {
            case PlayerType.Rabbit:
                _instance._playerStoryProgressRabbit[dayOfWeek] = value;
                break;
            case PlayerType.Mole:
                _instance._playerStoryProgressMole[dayOfWeek] = value;
                break;
            default:
                Debug.LogWarning("GameManager.SetStoryProgress: Unknown player type.");
                return;
        }

        _instance.SavePlayerPrefs();
    }

    /// <summary>
    /// Gets a specific index in the rabbit story progress array.
    /// </summary>
    public static bool GetStoryProgress(DayOfWeek dayOfWeek, PlayerType playerType)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.GetStoryProgress: Instance is null.");
            return false;
        }

        switch(playerType)
        {
            case PlayerType.Rabbit:
                return _instance._playerStoryProgressRabbit[dayOfWeek];
            case PlayerType.Mole:
                return _instance._playerStoryProgressMole[dayOfWeek];
            default:
                Debug.LogWarning("GameManager.GetStoryProgress: Unknown player type.");
                return false;
        }
    }

    public static void ResetStoryProgress(PlayerType playerType)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.ResetStoryProgress: Instance is null.");
            return;
        }
        switch(playerType)
        {
            case PlayerType.Rabbit:
                foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    _instance._playerStoryProgressRabbit[(DayOfWeek)day] = false;
                }
                break;
            case PlayerType.Mole:
                foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    _instance._playerStoryProgressMole[(DayOfWeek)day] = false;
                }
                break;
            default:
                Debug.LogWarning("GameManager.ResetStoryProgress: Unknown player type.");
                return;
        }
        _instance.SavePlayerPrefs();
    }

    public static void ResetGoldenCarrotProgress(PlayerType playerType)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.ResetGoldenCarrotProgress: Instance is null.");
            return;
        }
        switch(playerType)
        {
            case PlayerType.Rabbit:
                foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    _instance._goldenCarrotPickStateRabbit[(DayOfWeek)day] = false;
                }
                break;
            case PlayerType.Mole:
                foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    _instance._goldenCarrotPickStateMole[(DayOfWeek)day] = false;
                }
                break;
            default:
                Debug.LogWarning("GameManager.ResetGoldenCarrotProgress: Unknown player type.");
                return;
        }
        _instance.SavePlayerPrefs();
    }

    public static void CompliteCurrentDay()
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.CompliteCurrentDay: Instance is null.");
            return;
        }
        if (!GameInspector.IsActive)
        {
            Debug.LogWarning("GameManager.CompliteCurrentDay: GameInspector is not active.");
            return;
        }

        switch (GameInspector.CurrentPlayerOnStory)
        {
            case PlayerType.Rabbit:
                SetStoryProgress(_instance._currentDayOfWeek, true);
                break;
            case PlayerType.Mole:
                SetStoryProgress(_instance._currentDayOfWeek, true);
                break;
            default:
                Debug.LogWarning("GameManager.CompliteCurrentDay: Unknown player type.");
                return;
        }
    }

    /// <summary>
    /// Dialogue system questions
    /// </summary>
    public static object Get(string field)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.Get: Instance is null.");
            return null;
        }

         switch (field)
         {
             case "GoldenCarrotPicked":
                 return false;
             default:
                 Debug.LogWarning($"GameManager.Get: Unknown field '{field}'");
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

    public static void PlayGame(GameModeData gameMode, GameSceneManager.SceneType map, DayOfWeek day, PlayerType playerTypeForStory, PlayerControlAgent rabbitControlAgent, PlayerControlAgent moleControlAgent)
    {
        _instance._currentDayOfWeek = day;
        GameSceneManager.ChangeScene(map, () =>
        {
            Instantiate(_instance.GameInspectorPrefab, SceneManager.GetActiveScene());
            GameInspector.CurrentGameMode = gameMode;
            GameInspector.CurrentPlayerOnStory = playerTypeForStory;
            GameInspector.RabbitControlAgent = rabbitControlAgent;
            GameInspector.MoleControlAgent = moleControlAgent;
            
            // Spawn players after GameInspector is set up
            PlayerSpawnSystem.SpawnPlayers();
        });

        Debug.Log($"GameManager: Starting game for {day}, Map: {GetSceneTypeDescription(map)}[{map}], Rabbit: {rabbitControlAgent}, Mole: {moleControlAgent}");
    }

    internal static bool GetMoleProgress(DayOfWeek dayOfWeek)
    {
        if(_instance == null)
        {
            Debug.LogWarning("GameManager.GetMoleProgress: Instance is null.");
            return false;
        }
         return _instance._playerStoryProgressMole[dayOfWeek];
    }

    internal static void GamePlayTimeEnd()
    {
        throw new NotImplementedException();
    }

    internal static void GamePlayCarrotGoal()
    {
        throw new NotImplementedException();
    }
}
