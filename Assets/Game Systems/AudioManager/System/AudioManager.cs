using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

/*
 * HOW TO USE:
 * 1. Create an AudioMixer (e.g., "MainMixer") with 4 Groups: "Music", "SFX", "Dialogues", "Ambient".
 * 2. Expose the 'Volume' parameter for each group and rename them:
 * - "MusicVolume"
 * - "SFXVolume"
 * - "DialoguesVolume"
 * - "AmbientVolume"
 * 3. Create an empty GameObject "AudioManager" and attach this script.
 * 4. Drag the Mixer and the 4 Groups into the corresponding public fields in the Inspector.
 * 5. Create a prefab of this "AudioManager" and place it in your FIRST scene (e.g., Boot or Menu).
 * 6. (Optional) For 3D sound pooling, create a simple prefab with only an AudioSource component 
 * and assign it to the 'sfx3DPrefab' field.
 * 7. Create MusicPlaylistSO assets using standard AudioClip references.
 */

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public enum AudioChannel { Music, SFX, Dialogue, Ambient}

    [Header("Audio Mixer")]
    [Tooltip("The main mixer for the game.")]
    [SerializeField]
    private AudioMixer mainMixer;
    [Tooltip("The AudioMixerGroup for music.")]
    [SerializeField]
    private AudioMixerGroup musicGroup;
    [Tooltip("The AudioMixerGroup for sound effects.")]
    [SerializeField]
    private AudioMixerGroup sfxGroup;
    [Tooltip("The AudioMixerGroup for dialogues.")]
    [SerializeField]
    private AudioMixerGroup dialogueGroup;
    [Tooltip("The AudioMixerGroup for ambient sounds.")]
    [SerializeField]
    private AudioMixerGroup ambientGroup;

    [Header("Configuration")]
    [Tooltip("Time in seconds for music tracks to crossfade.")]
    [SerializeField]
    private float _musicFadeTime = 1.5f;
    [Tooltip("Prefab used for pooling 3D sounds. Must have an AudioSource component.")]
    [SerializeField]
    private GameObject _sfx3DPrefab;
    [Tooltip("Initial size of the 3D sound object pool.")]
    [SerializeField]
    private const int _sfxPoolSize = 20;

    // --- Internal Fields ---

    // Audio lisner in scene
    private AudioListener _audioLisner;
    private AudioListener _selfAudioLisner;
    private bool _haveAudioLisner => _audioLisner != null;

    // Audio sources
    private AudioSource _musicSourceA;
    private AudioSource _musicSourceB;
    private AudioSource _activeMusicSource;
    private AudioSource _uiSfxSource; // For 2D/UI sounds

    // Caching & Pooling
    // Restored: Dictionary for caching Addressable
    private Dictionary<string, AudioClip> _loadedClips = new Dictionary<string, AudioClip>(32); 
    private Dictionary<string, Task<AudioClip>> _loadingTasks = new Dictionary<string, Task<AudioClip>>();
    private readonly object _cacheLock = new object();
    // Cache for non-Addressable AudioClips to prevent reloading from disk
    // Using ConcurrentDictionary for thread-safe operations without blocking
    private ConcurrentDictionary<int, AudioClip> _nonAddressableClipCache = new ConcurrentDictionary<int, AudioClip>();
    private Queue<AudioSource> _sfxPool = new Queue<AudioSource>(20);
    private Coroutine _musicFadeCoroutine;

    // Music Playlist
    private MusicPlaylistSO _currentPlaylist;
    private bool _isPlaylistPlaying = false;
    // Remains: AudioClip for local music
    private AudioClip _lastPlayedTrack = null;
    // Cache for filtered playlist to avoid LINQ allocations
    private List<AudioClip> _cachedPlayableTracks = new List<AudioClip>(16);

    // getters
    internal static AudioMixerGroup AmbienceMixerGrup => _instance != null ? _instance.ambientGroup : null;
    internal static bool IsInstanceActive => _instance != null;

    #region --- Unity Methods (Initialization) ---

    void Awake()
    {
        Debug.Log("AudioManager: Awake called.");
        // Implement the Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            InitializeSfxPool();
            _selfAudioLisner = gameObject.GetOrAddComponent<AudioListener>();
        }
        else
        {
            Destroy(gameObject);
        }
        Debug.Log("AudioManager: Singleton instance set.");
    }

    void Update()
    {
        // Handle playlist progression
        if (_isPlaylistPlaying && _activeMusicSource != null && !_activeMusicSource.isPlaying && _musicFadeCoroutine == null)
        {
            PlayNextInPlaylist();
        }
    }

    /// <summary>
    /// Creates the dedicated AudioSource components required by the manager.
    /// </summary>
    private void InitializeAudioSources()
    {
        // Create music sources (two for crossfading)
        _musicSourceA = gameObject.AddComponent<AudioSource>();
        _musicSourceA.outputAudioMixerGroup = musicGroup;
        _musicSourceA.loop = true; // Loop by default
        _musicSourceA.playOnAwake = false;

        _musicSourceB = gameObject.AddComponent<AudioSource>();
        _musicSourceB.outputAudioMixerGroup = musicGroup;
        _musicSourceB.loop = true; // Loop by default
        _musicSourceB.playOnAwake = false;

        _activeMusicSource = _musicSourceA;

        // Create UI/2D SFX source
        _uiSfxSource = gameObject.AddComponent<AudioSource>();
        _uiSfxSource.outputAudioMixerGroup = sfxGroup;
        _uiSfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Populates the object pool for 3D sounds.
    /// </summary>
    private void InitializeSfxPool()
    {
        if (_sfx3DPrefab == null)
        {
            Debug.LogWarning("AudioManager: 'sfx3DPrefab' is not set. 3D sounds will be created dynamically (less optimal).");
            return;
        }

        // Pre-warm the pool
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var source = CreatePooledSource();
            if (source != null)
            {
                source.gameObject.SetActive(false);
                source.gameObject.transform.parent = transform;
                _sfxPool.Enqueue(source);
            }
        }
    }

    #endregion

    #region --- Volume & Mixer Control (Static Interface) ---
    /// <summary>
    /// Internal method to set the volume parameter on the Audio Mixer.
    /// </summary>
    private void SetVolume(string channel, float volume)
    {
        string parameterName = channel + "Volume"; // e.g., "MusicVolume"

        // Convert linear volume (0-1) to logarithmic (dB)
        float dbVolume = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1.0f)) * 20;
        mainMixer.SetFloat(parameterName, dbVolume);
    }

    /// <summary>
    /// Internal method to get the volume parameter from the Audio Mixer.
    /// </summary>
    private float GetVolume(string channel)
    {
        string parameterName = channel + "Volume"; // e.g., "MusicVolume"
        if (mainMixer.GetFloat(parameterName, out float dbValue))
        {
            // Convert logarithmic (dB) back to linear (0-1)
            return Mathf.Pow(10, dbValue / 20);
        }
        return 0f;
    }

    // Static Setters    
    public static void SetMasterVolume(float volume)
    {
        if (_instance == null) return;
        _instance.SetVolume("Master", volume);
    }
    public static void SetMusicVolume(float volume)
    {
        if (_instance == null) return;
        _instance.SetVolume("Music", volume);
    }
    public static void SetSFXVolume(float volume)
    {
        if (_instance == null) return;
        _instance.SetVolume("SFX", volume);
    }
    public static void SetDialoguesVolume(float volume)
    {
        if (_instance == null) return;
        _instance.SetVolume("Dialogues", volume);
    }
    public static void SetAmbientVolume(float volume)
    {
        if (_instance == null) return;
        _instance.SetVolume("Ambient", volume);
    }

    // Static Getters
    public static float GetMasterVolume()
    {
        if (_instance == null) return 0f;
        return _instance.GetVolume("Master");
    }
    public static float GetMusicVolume()
    {
        if (_instance == null) return 0f;
        return _instance.GetVolume("Music");
    }
    public static float GetSFXVolume()
    {
        if (_instance == null) return 0f;
        return _instance.GetVolume("SFX");
    }
    public static float GetDialoguesVolume()
    {
        if (_instance == null) return 0f;
        return _instance.GetVolume("Dialogues");
    }
    public static float GetAmbientVolume()
    {
        if (_instance == null) return 0f;
        return _instance.GetVolume("Ambient");
    }

    #endregion

    #region --- Music Control (Static Interface) ---

    /// <summary>
    /// Plays a music track, fading out any currently playing track.
    /// </summary>
    /// <param name="clip">The music clip to play (from Inspector).</param>
    /// <param name="loop">Should this track loop?</param>
    public static void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (_instance == null) return;
        if (clip == null) return;
        
        // Get or cache non-Addressable clips to prevent reloading from disk
        AudioClip cachedClip = _instance.GetOrCacheNonAddressableClip(clip);
        
        _instance._isPlaylistPlaying = false;
        // The _lastPlayedTrack is set to the clip itself
        _instance._lastPlayedTrack = cachedClip;
        _instance.StartMusicFade(cachedClip, loop);
    }

    /// <summary>
    /// Plays a list of music tracks from a ScriptableObject in random order.
    /// </summary>
    /// <param name="playlist">The ScriptableObject containing the music list.</param>
    public static void PlayMusicPlaylist(MusicPlaylistSO playlist)
    {
        if (_instance == null) return;
        if (playlist == null || playlist.musicTracks.Count == 0)
        {
            Debug.LogError("AudioManager: Playlist is null or empty.");
            return;
        }

        _instance._currentPlaylist = playlist;
        _instance._isPlaylistPlaying = true;
        _instance._lastPlayedTrack = null; // Clear last played track on new playlist
        _instance.PlayNextInPlaylist();
    }

    /// <summary>
    /// Stops the music with a fade out.
    /// </summary>
    public static void StopMusic()
    {
        if (_instance == null) return;
        _instance._isPlaylistPlaying = false;
        _instance._lastPlayedTrack = null;
        _instance.StartMusicFade(null, false);
    }

    /// <summary>
    /// Pauses the currently playing music track.
    /// </summary>
    public static void PauseMusic()
    {
        if (_instance == null) return;
        if (_instance._activeMusicSource.isPlaying)
        {
            _instance._activeMusicSource.Pause();
        }
    }

    /// <summary>
    /// Resumes the currently paused music track.
    /// </summary>
    public static void ResumeMusic()
    {
        if (_instance == null) return;
        if (!_instance._activeMusicSource.isPlaying && _instance._activeMusicSource.clip != null)
        {
            _instance._activeMusicSource.UnPause();
        }
    }

    /// <summary>
    /// Internal: Plays the next random track from the current playlist.
    /// </summary>
    private void PlayNextInPlaylist() // Changed from async void
    {
        if (_currentPlaylist == null || _currentPlaylist.musicTracks.Count == 0)
        {
            _isPlaylistPlaying = false;
            return;
        }

        // Clear and reuse cached list to avoid allocations
        _cachedPlayableTracks.Clear();
        
        if (_currentPlaylist.AvoidImmediateRepeat)
        {
            // Filter out the last played track to avoid immediate repeats (manual filter to avoid LINQ allocation)
            foreach (var track in _currentPlaylist.musicTracks)
            {
                if (track != _lastPlayedTrack)
                {
                    _cachedPlayableTracks.Add(track);
                }
            }
        }
        else
        {
            // Add all tracks without filtering
            foreach (var track in _currentPlaylist.musicTracks)
            {
                _cachedPlayableTracks.Add(track);
            }
        }

        // If filtering left no tracks (e.g., only one song in playlist), just use the full list
        if (_cachedPlayableTracks.Count == 0)
        {
            _cachedPlayableTracks.Clear();
            foreach (var track in _currentPlaylist.musicTracks)
            {
                _cachedPlayableTracks.Add(track);
            }
        }

        // Select a random track from the filtered list
        int randomIndex = Random.Range(0, _cachedPlayableTracks.Count);
        AudioClip nextTrackClip = _cachedPlayableTracks[randomIndex];

        // Get or cache non-Addressable clips to prevent reloading from disk
        AudioClip cachedClip = nextTrackClip != null ? GetOrCacheNonAddressableClip(nextTrackClip) : null;

        // Store this as the last played track
        _lastPlayedTrack = cachedClip;

        // Play the clip directly
        StartMusicFade(cachedClip, true);
    }

    /// <summary>
    /// Starts the crossfade coroutine.
    /// </summary>
    private void StartMusicFade(AudioClip newClip, bool loop)
    {
        if (_musicFadeCoroutine != null)
        {
            StopCoroutine(_musicFadeCoroutine);
        }
        _musicFadeCoroutine = StartCoroutine(FadeTrackCoroutine(newClip, loop));
    }

    /// <summary>
    /// Coroutine to crossfade between two music sources.
    /// </summary>
    private IEnumerator FadeTrackCoroutine(AudioClip newClip, bool loop)
    {
        AudioSource oldSource = _activeMusicSource;
        AudioSource newSource = (_activeMusicSource == _musicSourceA) ? _musicSourceB : _musicSourceA;

        // Set up the new source
        newSource.clip = newClip;
        newSource.loop = loop;
        newSource.volume = 0;
        if (newClip != null)
        {
            newSource.Play();
        }

        _activeMusicSource = newSource; // It's now the active one

        // Fade in/out
        float timer = 0;
        while (timer < _musicFadeTime)
        {
            float t = timer / _musicFadeTime;
            oldSource.volume = Mathf.Lerp(1, 0, t);
            newSource.volume = Mathf.Lerp(0, 1, t);

            timer += Time.unscaledDeltaTime; // Use unscaled time for fades
            yield return null;
        }

        // Finalize
        oldSource.Stop();
        oldSource.clip = null;
        newSource.volume = 1;

        _musicFadeCoroutine = null;
    }

    #endregion

    #region --- Sound Effects (2D & 3D) ---

    /// <summary>
    /// Plays a 2D sound (e.g., UI click) from an Inspector-assigned clip.
    /// </summary>
    public static void PlaySoundUI(AudioClip clip, float volumeScale = 1.0f)
    {
        if (_instance == null) return;
        if (clip == null) return;
        
        // Get or cache non-Addressable clips to prevent reloading from disk
        AudioClip cachedClip = _instance.GetOrCacheNonAddressableClip(clip);
        
        _instance._uiSfxSource.PlayOneShot(cachedClip, volumeScale);
    }

    /// <summary>
    /// Loads and plays a 2D sound from Addressables.
    /// </summary>
    // Restored: SFX 2D with Addressables
    public static async Task PlaySoundUI(string addressableKey, float volumeScale = 1.0f)
    {
        if (_instance == null) return;
        AudioClip clip = await LoadClipAsync(addressableKey);
        if (clip != null)
        {
            PlaySoundUI(clip, volumeScale);
        }
    }

    /// <summary>
    /// Plays a 3D sound at a specific world position (Uses SFX Group).
    /// </summary>
    public static void PlaySound3D(AudioClip clip, Vector3 position, AudioChannel channel = AudioChannel.SFX)
    {
        if (_instance == null) return;
        if (clip == null) return;
        
        // Get or cache non-Addressable clips to prevent reloading from disk
        AudioClip cachedClip = _instance.GetOrCacheNonAddressableClip(clip);
        
        _instance.PlaySoundAtPosition(
            cachedClip, 
            _instance._haveAudioLisner 
                ? position 
                : _instance.transform.position, 
            channel);
    }

    /// <summary>
    /// Loads and plays a 3D sound from Addressables (Uses SFX Group).
    /// </summary>
    public static async void PlaySound3D(string addressableKey, Vector3 position, AudioChannel channel = AudioChannel.SFX)
    {
        if (_instance == null) return;
        await _instance.InternalPlaySoundAtPositionAsync(
            addressableKey, 
            _instance._haveAudioLisner 
                ? position 
                : _instance.transform.position, 
            channel);
    }
    
    /// <summary>
    /// Plays a 3D dialogue line at a specific world position (Uses Dialogue Group).
    /// </summary>
    public static void PlayDialogue3D(AudioClip clip, Vector3 position)
    {
        if (_instance == null) return;
        if (clip == null) return;
        
        // Get or cache non-Addressable clips to prevent reloading from disk
        AudioClip cachedClip = _instance.GetOrCacheNonAddressableClip(clip);
        
        _instance.PlaySoundAtPosition(
            cachedClip, 
            _instance._haveAudioLisner 
                ? position 
                : _instance.transform.position, 
            AudioChannel.Dialogue);

    }
    /// <summary>
    /// Plays a dialogue line (Uses Dialogue Group).
    /// </summary>
    public static void PlayDialogue(AudioClip clip) => PlayDialogue3D(clip, _instance.transform.position);

    /// <summary>
    /// Loads and plays a 3D dialogue line from Addressables (Uses Dialogue Group).
    /// </summary>
    public static async void PlayDialogue3D(string addressableKey, Vector3 position)
    {
        if (_instance == null) return;
        await _instance.InternalPlaySoundAtPositionAsync(
            addressableKey, 
            _instance._haveAudioLisner 
                ? position 
                : _instance.transform.position, 
            AudioChannel.Dialogue);
    }

    /// <summary>
    /// Plays a dialogue line from Addressables (Uses Dialogue Group).
    /// </summary>
    public static void PlayDialogue(string addressableKey) => PlayDialogue3D(addressableKey, _instance.transform.position);

    // --- Private Implementation ---

    /// <summary>
    /// Internal: Loads a clip and plays it at a position (Async helper).
    /// </summary>
    // Restored: Internal async helper for 3D sounds
    private async Task InternalPlaySoundAtPositionAsync(string addressableKey, Vector3 position, AudioChannel channel)
    {
        AudioClip clip = await LoadClipAsync(addressableKey);
        if (clip != null)
        {
            PlaySoundAtPosition(clip, position, channel);
        }
    }

    /// <summary>
    /// Internal: Core logic for playing a 3D sound.
    /// </summary>
    private void PlaySoundAtPosition(AudioClip clip, Vector3 position, AudioChannel channel)
    {
        if (clip == null) return;

        AudioSource source = GetPooledSource();
        if (source == null) return; // Could be null if no prefab is set AND pool is empty

        source.transform.position = position;
        source.clip = clip;
        switch (channel)
        {
            case AudioChannel.Music:
                source.outputAudioMixerGroup = musicGroup;
                break;
            case AudioChannel.SFX:
                source.outputAudioMixerGroup = sfxGroup;
                break;
            case AudioChannel.Dialogue:
                source.outputAudioMixerGroup = dialogueGroup;
                break;
            case AudioChannel.Ambient:
                source.outputAudioMixerGroup = ambientGroup;
                break;
            default:
                break;
        }

        // Configure 3D settings
        source.spatialBlend = 1.0f;
        source.minDistance = 1.0f;
        source.maxDistance = 50.0f;

        source.Play();

        // Return to pool after it's done
        StartCoroutine(ReturnSourceToPool(source, clip.length));
    }

    #endregion

    #region --- Pooling & Loading (Static/Instance Logic) ---

    /// <summary>
    /// Gets an available AudioSource from the pool.
    /// </summary>
    private AudioSource GetPooledSource()
    {
        if (_sfxPool.Count > 0)
        {
            AudioSource source = _sfxPool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        // Pool is empty, create a new one if prefab is set
        if (_sfx3DPrefab != null)
        {
            Debug.LogWarning("SFX Pool was empty. Creating new instance.");
            return CreatePooledSource();
        }

        // Fallback: create a temporary GameObject (NOT recommended)
        Debug.LogError("SFX Pool empty and no prefab set. Creating temporary GameObject.");
        GameObject tempGO = new GameObject("TempAudioSource");
        // Ensure the temp GO is destroyed later if not returned to pool logic
        return tempGO.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Helper to create a new pooled AudioSource from the prefab.
    /// </summary>
    private AudioSource CreatePooledSource()
    {
        if (_sfx3DPrefab == null) return null;

        GameObject instance = Instantiate(_sfx3DPrefab, transform); // Parent to manager
        AudioSource source = instance.GetComponent<AudioSource>();
        if (source == null)
        {
            Debug.LogError("AudioManager: 'sfx3DPrefab' does NOT have an AudioSource component!");
            Destroy(instance);
            return null;
        }
        return source;
    }

    /// <summary>
    /// Coroutine to return a 3D sound source to the pool after it finishes playing.
    /// </summary>
    private IEnumerator ReturnSourceToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (source == null) yield break; // Safety check if object was destroyed

        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);

        // Check if this was a pooled object (has the manager as a parent)
        if (_sfx3DPrefab != null && source.transform.parent == transform)
        {
            _sfxPool.Enqueue(source);
        }
        else
        {
            Destroy(source.gameObject); // This was a temporary fallback or not pooled
        }
    }

    /// <summary>
    /// Loads an AudioClip from Addressables using a string key.
    /// Caches the result for future calls.
    /// </summary>
    // Restored: Addressables loading using string key
    public static async Task<AudioClip> LoadClipAsync(string key)
    {
        if (_instance == null)
        {
            Debug.LogError("AudioManager: Instance is not yet initialized for loading.");
            return null;
        }

        if (string.IsNullOrEmpty(key)) return null;

        Task<AudioClip> loadTask = null;
        lock (_instance._cacheLock)
        {
            // Fast path: Check cache first
            if (_instance._loadedClips.TryGetValue(key, out AudioClip clip))
            {
                return clip; // Return immediately from cache
            }

            // Check if already loading - join existing task
            if (_instance._loadingTasks.TryGetValue(key, out loadTask))
            {
                // Already loading, join the existing task
            }
            else
            {
                // Start new load task
                loadTask = _instance.LoadAndCacheClipAsync(key);
                _instance._loadingTasks.Add(key, loadTask);
            }
        }
        try
        {
            AudioClip resultClip = await loadTask;
            return resultClip;
        }
        catch (Exception ex)
        {
            Debug.LogError($"AudioManager: Load failed for {key}. {ex.Message}");
            return null;
        }
    }
    private async Task<AudioClip> LoadAndCacheClipAsync(string key)
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<AudioClip>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                AudioClip clip = handle.Result;
                lock (_cacheLock)
                {
                    if (!_loadedClips.ContainsKey(key))
                    {
                        _loadedClips[key] = clip;
                    }
                }
                return clip;
            }
            else
            {
                Debug.LogError($"AudioManager: Failed to load sound with address: {key}");
                throw new Exception($"Failed to load Addressable. Status: {handle.Status}");
            }
        }
        finally
        {
            lock (_cacheLock)
            {
                if (_loadingTasks.ContainsKey(key))
                {
                    _loadingTasks.Remove(key);
                }
            }
        }
    }

    /// <summary>
    /// Call this when changing scenes to clear non-essential cached sounds.
    /// </summary>
    // Restored: Clearing SFX cache
    public static void ClearSoundCache()
    {
        if (_instance == null) return;

        lock (_instance._cacheLock)
        {
            foreach (var pair in _instance._loadedClips)
            {
                Addressables.Release(pair.Value);
            }
            _instance._loadedClips.Clear();
            Debug.Log("AudioManager: Sound cache cleared.");
        }
        
        // Clear non-Addressable clip cache (ConcurrentDictionary.Clear is thread-safe)
        _instance._nonAddressableClipCache.Clear();
        Debug.Log("AudioManager: Non-Addressable sound cache cleared.");
    }
    
    /// <summary>
    /// Gets or caches a non-Addressable AudioClip to prevent reloading from disk.
    /// Uses instance ID as the cache key to ensure uniqueness.
    /// Returns the cached clip if it exists, otherwise caches and returns the provided clip.
    /// Optimized: Uses ConcurrentDictionary for lock-free thread-safe operations.
    /// </summary>
    private AudioClip GetOrCacheNonAddressableClip(AudioClip clip, bool forceLoad = false)
    {
        if (clip == null) return null;
        
        int clipInstanceID = clip.GetInstanceID();
        
        // Fast path: Try to get from cache first (lock-free with ConcurrentDictionary)
        if (_nonAddressableClipCache.TryGetValue(clipInstanceID, out AudioClip cachedClip))
        {
            // If forceLoad is true, ensure the clip is loaded even if cached
            if (forceLoad && cachedClip != null)
            {
                ForceLoadAudioClip(cachedClip);
            }
            return cachedClip; // Return cached clip to ensure we use the same instance
        }
        
        // Add to cache if not present - GetOrAdd is atomic and thread-safe
        // This ensures we only cache once even if called from multiple threads
        AudioClip result = _nonAddressableClipCache.GetOrAdd(clipInstanceID, clip);
        
        // Force load if requested
        if (forceLoad && result != null)
        {
            ForceLoadAudioClip(result);
        }
        
        return result;
    }
    
    /// <summary>
    /// Forces Unity to load AudioClip data from disk by accessing properties that require loading.
    /// This prevents lag when the clip is first used during gameplay.
    /// </summary>
    private void ForceLoadAudioClip(AudioClip clip)
    {
        if (clip == null) return;
        
        // Access properties that force Unity to load the audio data from disk
        // This ensures the clip is fully loaded before gameplay starts
        try
        {
            // Accessing length and samples forces Unity to load the audio data
            float length = clip.length;
            int samples = clip.samples;
            int channels = clip.channels;
            int frequency = clip.frequency;
            
            // Try to load audio data explicitly if not already loaded
            if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Loading)
            {
                clip.LoadAudioData();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"AudioManager: Could not force load AudioClip '{clip.name}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Preloads non-Addressable AudioClips into cache and forces them to load from disk.
    /// Call this during loading screens or initialization to pre-cache frequently used clips.
    /// This prevents lag when clips are first used during gameplay.
    /// </summary>
    public static void PreloadClips(params AudioClip[] clips)
    {
        if (_instance == null || clips == null) return;
        
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                _instance.GetOrCacheNonAddressableClip(clip, forceLoad: true);
            }
        }
    }
    
    /// <summary>
    /// Preloads non-Addressable AudioClips from a list and forces them to load from disk.
    /// This prevents lag when clips are first used during gameplay.
    /// </summary>
    public static void PreloadClips(List<AudioClip> clips)
    {
        if (_instance == null || clips == null) return;
        
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                _instance.GetOrCacheNonAddressableClip(clip, forceLoad: true);
            }
        }
    }
    
    /// <summary>
    /// Loads an AudioClip from Addressables synchronously using WaitForCompletion.
    /// Use this during Awake() to load sounds before the scene becomes active.
    /// Caches the result for future calls.
    /// </summary>
    public static AudioClip LoadClipSync(string key)
    {
        if (_instance == null)
        {
            Debug.LogError("AudioManager: Instance is not yet initialized for loading.");
            return null;
        }

        if (string.IsNullOrEmpty(key)) return null;

        lock (_instance._cacheLock)
        {
            // Fast path: Check cache first
            if (_instance._loadedClips.TryGetValue(key, out AudioClip clip))
            {
                return clip; // Return immediately from cache
            }
        }

        // Load synchronously using WaitForCompletion
        var handle = Addressables.LoadAssetAsync<AudioClip>(key);
        handle.WaitForCompletion();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip clip = handle.Result;
            lock (_instance._cacheLock)
            {
                if (!_instance._loadedClips.ContainsKey(key))
                {
                    _instance._loadedClips[key] = clip;
                }
            }
            return clip;
        }
        else
        {
            Debug.LogError($"AudioManager: Failed to load sound with address: {key}. Status: {handle.Status}");
            return null;
        }
    }

    public static void AudioLisnerRegister(AudioListener audioListener)
    {
        Debug.Log("SD");
        if (_instance._haveAudioLisner)
        {
            audioListener.enabled = false;
        }
        else
        {
            _instance._audioLisner = audioListener;
            _instance._selfAudioLisner.enabled = false;
        }
    }

    public static void AudioLisnerDelelete()
    {
        _instance._audioLisner = null;
        if(_instance._selfAudioLisner != null)
            _instance._selfAudioLisner.enabled = true;
    }

}
#endregion