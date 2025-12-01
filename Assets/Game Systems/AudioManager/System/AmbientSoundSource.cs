using System.Collections;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

/*
 * This component creates persistent, ambient 3D sound effects.
 * It automatically adds a SphereCollider and uses it as a trigger for optimized playback.
 * The sound only plays when the 'Player' is within the collider's radius.
 */
[RequireComponent(typeof(AudioSource))]
public class AmbientSoundSource : MonoBehaviour
{
    [Header("Audio Clip Source")]
    [Tooltip("Assign the AudioClip directly here (Inspector) or use Addressable Key.")]
    public AudioClip ambientClip;
    [Tooltip("Or, use the Addressable key/name to load the clip.")]
    public string addressableKey;

    [Header("3D Sound Settings")]
    [Tooltip("The radius of the trigger zone and the maximum distance for sound falloff.")]
    public float maxDistance = 30f;
    [Tooltip("The distance at which the sound is at max volume (determines 3D falloff curve).")]
    public float minDistance = 5f;
    [Tooltip("Fade time when player enters or leaves the trigger zone.")]
    public float fadeTime = 1.0f;

    [Header("Editor Visualization")]
    [Tooltip("The color of the Gizmo sphere drawn in the editor for maxDistance.")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.3f); // Cyan with transparency

    private AudioSource _audioSource;
    private AudioClip _loadedAddressableClip;
    private Coroutine _fadeCoroutine;
    private bool _isClipReady = false; // New flag to indicate clip readiness
    private static Dictionary<string, AudioClip> _audioCahe = new();

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        // --- 1. Automatic Collider Setup ---
        SetupCollider();

        // 2. Assign Mixer Group
        // Note: It's safer to check for AudioManager.IsInstanceActive before accessing static properties
        if (AudioManager.IsInstanceActive)
        {
            _audioSource.outputAudioMixerGroup = AudioManager.AmbienceMixerGrup;
            if (_audioSource.outputAudioMixerGroup == null)
                Debug.LogError("AmbientSoundSource: Ambient Group is null. Check AudioManager configuration.");
        }
        else
        {
            Debug.LogError("AmbientSoundSource: AudioManager is not initialized yet.");
        }

        // 3. Configure 3D settings
        _audioSource.spatialBlend = 1.0f;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;

        // Set attenuation based on inspector settings
        _audioSource.minDistance = minDistance;
        _audioSource.maxDistance = maxDistance;

        // Start muted to ensure trigger is the only way to activate
        _audioSource.volume = 0f;

        // 4. Load audio clip synchronously in Awake() to prevent lag during gameplay
        // This ensures sounds are loaded during scene loading, before the scene becomes active
        LoadAudioClip();
    }

    /// <summary>
    /// Loads the AudioClip synchronously in Awake() to prevent lag during gameplay.
    /// Prioritizes clip assigned in Inspector, otherwise loads from Addressables.
    /// </summary>
    private void LoadAudioClip()
    {
        // Prioritize clip assigned in Inspector
        if (ambientClip != null)
        {
            _audioSource.clip = ambientClip;
            _isClipReady = true;
        }
        // If no clip in Inspector, try loading from Addressables synchronously
        else if (!string.IsNullOrEmpty(addressableKey))
        {
            // Load synchronously using WaitForCompletion - this blocks during scene loading
            // which is fine since the scene isn't active yet
            AudioClip clip = AudioManager.LoadClipSync(addressableKey);

            if (clip != null)
            {
                SetAudioCLip(clip);
                Debug.Log($"AmbientSoundSource loaded clip synchronously in Awake: {addressableKey}");
            }
            else
            {
                Debug.LogError($"AmbientSoundSource: Failed to load clip from manager: {addressableKey}");
                _isClipReady = false;
            }
        }
        else
        {
            Debug.LogWarning($"AmbientSoundSource on {gameObject.name}: No AudioClip or Addressable Key provided.");
        }
    }

    private void SetAudioCLip(AudioClip audioClip)
    {
        _loadedAddressableClip = audioClip;
        _audioSource.clip = audioClip;
        _isClipReady = true;
    }

    /// <summary>
    /// Ensures a SphereCollider is present, configured as a trigger, and its radius matches maxDistance.
    /// </summary>
    private void SetupCollider()
    {
        SphereCollider col = gameObject.GetOrAddComponent<SphereCollider>();
       
        col.isTrigger = true;
        col.radius = maxDistance; // Collider radius matches max distance for player detection
    }


    // --- TRIGGER LOGIC FOR OPTIMIZATION ---

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the Player AND the clip is ready
        if (other.CompareTag("Player") && _isClipReady)
        {
            if (_audioSource.clip != null)
            {
                // Start the audio and fade in
                // Using Stop/Play ensures the audio is actively mixing when inside the trigger.
                if (!_audioSource.isPlaying)
                    _audioSource.Play();

                StartFade(1f); // Fade up to full volume
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is the Player
        if (other.CompareTag("Player"))
        {
            // Fade out and stop
            StartFade(0f); // Fade down to 0 volume
        }
    }

    // --- FADE COROUTINE ---

    /// <summary>
    /// Starts the volume fade coroutine.
    /// </summary>
    private void StartFade(float targetVolume)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeCoroutine(targetVolume));
    }

    /// <summary>
    /// Coroutine for fading volume in or out.
    /// </summary>
    private IEnumerator FadeCoroutine(float targetVolume)
    {
        float startVolume = _audioSource.volume;
        float timer = 0f;

        while (timer < fadeTime)
        {
            _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeTime);
            timer += Time.deltaTime;
            yield return null;
        }

        _audioSource.volume = targetVolume;

        // Stop playback completely only when faded out (targetVolume = 0)
        if (targetVolume == 0f)
        {
            _audioSource.Stop();
        }

        _fadeCoroutine = null;
    }

    // --- EDITOR GIZMOS (Visualization) ---

    private void OnValidate()
    {
        // Automatically update the collider radius when maxDistance changes in the Inspector
        if (Application.isPlaying == false)
        {
            SphereCollider col = GetComponent<SphereCollider>();
            if (col != null)
            {
                col.radius = maxDistance;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw only the max distance sphere with the customizable, transparent color (GizmoColor)
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, maxDistance);

        // Use a different color to draw the min distance wireframe, for clarity
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}