using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Globalny mened¿er audio, który u¿ywa systemu Addressables do ³adowania dŸwiêków na ¿¹danie.
/// </summary>
[Obsolete("new manager: AudioManager", true)]
public class SoundEffectManager : MonoBehaviour
{
    public static SoundEffectManager Instance { get; private set; }

    private readonly Dictionary<string, AudioClip> loadedSounds = new();

    private AudioSource audioSource;
    private float _volume = 1f;
    private bool _mute;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private static bool CreateInstance()
    {
        if (Instance != null) return true;

        var handle = Addressables.InstantiateAsync("Assets/Prefabs/SoundEffectManager.prefab", Vector3.zero, Quaternion.identity, null);
        handle.WaitForCompletion();

        if (handle.Status == AsyncOperationStatus.Succeeded)
            return true;
        
        Debug.LogError($"Failed to load SoundEffectManager");

        return false;
    }

    /// <summary>
    /// Set the volume level of sounds.
    /// It must be from 0.0f to 1.0f
    /// </summary>
    /// <param name="level">Sound level</param>
    public static void SetAudioLevel(float level)
    {
        if (!CreateInstance()) return;
        Instance._volume = Mathf.Clamp01(level);
    }

    public static void Mute(bool mute)
    {
        if (Instance == null) return;
        Instance._mute = mute;
    }

    /// <summary>
    /// Odtwarza jednorazowy dŸwiêk po nazwie. Jeœli dŸwiêk nie jest za³adowany,
    /// ³aduje go asynchronicznie, a nastêpnie odtwarza.
    /// Nazwa dŸwiêku musi byæ zgodna z jego adresem (Addressable Name) w Unity.
    /// </summary>
    /// <param name="soundAddress">Adres (nazwa) dŸwiêku z systemu Addressables.</param>
    public static void PlaySound(string soundAddress)
    {
        if (!CreateInstance())
            return;

        if (!Instance.loadedSounds.ContainsKey(soundAddress))
        {
            PreloadSound(soundAddress);
        }

        if (!Instance.loadedSounds.ContainsKey(soundAddress))
        {
            Debug.LogError($"SoundEffectManager->PlaySound({soundAddress}): error. Sound not found.");
            return;
        }

        Instance.audioSource.PlayOneShot(Instance.loadedSounds[soundAddress]);
    }

    /// <summary>
    /// Odtwarza jednorazowy dŸwiêk.
    /// </summary>
    /// <param name="sound">Dzwiêk</param>
    public static void PlaySound(AudioClip sound)
    {
        if (!CreateInstance())
            return;

        if (sound == null) return;

        if (!Instance._mute)
            Instance.audioSource.PlayOneShot(sound, Instance._volume);
    }

    public static async void PreloadSound(string soundAddress)
    {
        if (!CreateInstance())
            return;

        if (Instance.loadedSounds.ContainsKey(soundAddress))
            return;

        var handle = Addressables.LoadAssetAsync<AudioClip>(soundAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            Instance.loadedSounds[soundAddress] = handle.Result;
        else
            Debug.LogError($"Failed to load sound with address: {soundAddress}");
    }
}
