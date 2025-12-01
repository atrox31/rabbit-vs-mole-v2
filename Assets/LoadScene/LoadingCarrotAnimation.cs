using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCarrotAnimation : MonoBehaviour
{
    [SerializeField] private List<Sprite> _animationFrames = new List<Sprite>();
    [SerializeField] private float _frameRate = 3f;
    [SerializeField] private float _growRate = 2f;
    [SerializeField] private float _shakeIntensity = 5f;
    [SerializeField] private float _shakeDuration = 0.1f;
    private Image _spriteRenderer;
    private int _currentFrame;
    private float _timer;
    private Vector3 _originalPosition;

    void Awake()
    {
        _spriteRenderer = GetComponent<Image>();
    }

    void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;
        if (_timer >= 1f / _frameRate)
        {
            _timer -= 1f / _frameRate;
            _currentFrame = (_currentFrame + 1) % _animationFrames.Count;
            _spriteRenderer.sprite = _animationFrames[_currentFrame];
            StartCoroutine(ShakeAnimation());
            if(_currentFrame == 0)
                StartCoroutine(GrowImage());
        }
    }

    IEnumerator GrowImage()
    {
        _spriteRenderer.transform.localScale = Vector3.zero;
        while (_spriteRenderer.transform.localScale.x < 1f)
        {
            yield return new WaitForFixedUpdate();
            _spriteRenderer.transform.localScale += Vector3.one * _growRate * Time.fixedDeltaTime;
        }
        _spriteRenderer.transform.localScale = Vector3.one;
    }

    IEnumerator ShakeAnimation()
    {
        float elapsed = 0f;
        while (elapsed < _shakeDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float progress = elapsed / _shakeDuration;
            float shakeAmount = _shakeIntensity * (1f - progress);
            Vector3 randomOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );
            transform.localPosition = _originalPosition + randomOffset;
            yield return new WaitForFixedUpdate();
        }
        transform.localPosition = _originalPosition;
    }

}
