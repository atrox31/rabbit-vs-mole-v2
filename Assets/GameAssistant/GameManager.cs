using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    private DayOfWeek _currentDayOfWeek = DayOfWeek.Monday;
    public static DayOfWeek CurrentDayOfWeek => DayOfWeek.Monday;  // Temporarily locked to Monday for testing
    //public static DayOfWeek CurrentDayOfWeek { get { return _instance._currentDayOfWeek; } set { _instance._currentDayOfWeek = value; } }

    [Header("Music")]
    [SerializeField] private MusicPlaylistSO MusicForGameplay;
    [SerializeField] private AudioClip MusicForMainMenu;
    [SerializeField] private AudioClip MusicForVictory;
    [SerializeField] private AudioClip MusicForDefeat;
    [SerializeField] private AudioClip MusicForLoading;
    public enum MusicType { Gameplay, Victory, Defeat, MainMenu };

    // player stats and settings
    private Dictionary<DayOfWeek, bool> _goldenCarrotPickState = new Dictionary<DayOfWeek, bool>();
    private Dictionary<DayOfWeek, bool> _rabbitStoryProgress = new Dictionary<DayOfWeek, bool>();

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
            ResetGoldenCarrotProgress();
        }

        yield return new WaitForSecondsRealtime(5.0f);

        GameSceneManager.ChangeScene(GameSceneManager.SceneType.MainMenu);
    }

    private void LoadPlayerPrefs()
    {
        _goldenCarrotPickState = GoldenCarrotDataManager.LoadStatus();
        _rabbitStoryProgress = GameProgressDataManager.LoadStatus();
        AudioManager.SetAmbientVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.AMBIENT_VOLUME, 1.0f));
        AudioManager.SetSFXVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.SFX_VOLUME, 1.0f));
        AudioManager.SetMusicVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.MUSIC_VOLUME, 1.0f));
        AudioManager.SetDialoguesVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.DIALOGUES_VOLUME, 1.0f));
        AudioManager.SetMasterVolume(PlayerPrefs.GetFloat(PlayerPrefsConst.MASTER_VOLUME, 1.0f));
    }

    private void SavePlayerPrefs()
    {
        GoldenCarrotDataManager.SaveStatus(_goldenCarrotPickState);
        GameProgressDataManager.SaveStatus(_rabbitStoryProgress);
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
        _instance._goldenCarrotPickState[_instance._currentDayOfWeek] = true;
    }
    internal static bool IsGoldenCarrotCollected(DayOfWeek dayOfWeek)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.IsGoldenCarrotCollected: Instance is null.");
            return default;
        }
        return _instance._goldenCarrotPickState[dayOfWeek];
    }

    /// <summary>
    /// Sets a specific index in the rabbit story progress array.
    /// </summary>
    public static void SetRabbitStoryProgress(DayOfWeek dayOfWeek, bool value)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.SetRabbitStoryProgress: Instance is null.");
            return;
        }

        _instance._rabbitStoryProgress[dayOfWeek] = value;
        _instance.SavePlayerPrefs();
    }

    /// <summary>
    /// Gets a specific index in the rabbit story progress array.
    /// </summary>
    public static bool GetRabbitStoryProgress(DayOfWeek dayOfWeek)
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.GetRabbitStoryProgress: Instance is null.");
            return false;
        }

        return _instance._rabbitStoryProgress[dayOfWeek];
    }

    public static void ResetRabbitStoryProgress()
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.ResetRabbitStoryProgress: Instance is null.");
            return;
        }
        foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
        {
            _instance._rabbitStoryProgress[(DayOfWeek)day] = false;
        }
        _instance.SavePlayerPrefs();
    }

    public static void ResetGoldenCarrotProgress()
    {
        if (_instance == null)
        {
            Debug.LogWarning("GameManager.ResetGoldenCarrotProgress: Instance is null.");
            return;
        }
        foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
        {
            _instance._goldenCarrotPickState[(DayOfWeek)day] = false;
        }
        _instance.SavePlayerPrefs();
    }

    public static void CompliteCurrentDay()
    {
        SetRabbitStoryProgress(CurrentDayOfWeek, true);
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

}
