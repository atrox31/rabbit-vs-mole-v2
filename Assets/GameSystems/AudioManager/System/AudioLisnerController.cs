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
            // error here are always appear when You closing game via STOP PLAY MODE button
            // there is no need ot guard this
            // if this error appear in normal game You shud be worried
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