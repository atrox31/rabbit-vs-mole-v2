using Unity.Behavior;
using UnityEngine;

public class AudioLisnerController : MonoBehaviour
{
    private AudioListener _audioListener;
    private bool _isRegistered = false;

    void Awake()
    {
        _audioListener = gameObject.GetOrAddComponent<AudioListener>();
        if (_audioListener != null)
        {
            _audioListener.enabled = false;
        }
    }

    void TryToRegisterAudioLisner()
    {
        if (!_isRegistered && _audioListener != null)
        {
            AudioManager.RegisterNewAudioLisner(_audioListener);
            _isRegistered = true;
        }
    }

    void TryToUnregisterAudioLisner()
    {
        if (_isRegistered && _audioListener != null)
        {
            AudioManager.UnregisterAudioLisner(_audioListener);
            _isRegistered = false;
        }
    }

    void Start()
    {
        TryToRegisterAudioLisner();
    }

    void OnEnable()
    {
        TryToRegisterAudioLisner();
    }

    void OnDisable()
    {
        TryToUnregisterAudioLisner();
    }

    void OnDestroy()
    {
        TryToUnregisterAudioLisner();
    }
}