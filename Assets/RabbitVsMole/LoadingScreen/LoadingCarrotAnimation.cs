using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RabbitVsMole
{
    public class LoadingCarrotAnimation : MonoBehaviour
    {
        [SerializeField] private List<Sprite> _animationFrames = new List<Sprite>();
        [SerializeField] private float _frameRate = 3f;
        [SerializeField] private float _growRate = 2f;
        [SerializeField] private float _shakeIntensity = 5f;
        [SerializeField] private float _shakeDuration = 0.1f;
        [SerializeField] private float _maxDeltaStep = 1f / 30f; // prevents fast-forwarding after hitches
        private Image _spriteRenderer;
        private int _currentFrame;
        private float _timer;
        private Vector3 _originalPosition;

        void Awake()
        {
            _spriteRenderer = GetComponent<Image>();
            _originalPosition = transform.localPosition;
            StartCoroutine(UpdateImage());
        }

        IEnumerator UpdateImage()
        {
            float frameDuration = 1f / _frameRate;
            while (true)
            {
                float delta = Mathf.Min(Time.unscaledDeltaTime, frameDuration); // drop oversized steps so we do not catch up
                _timer += delta;

                if (_timer >= frameDuration)
                {
                    _timer = 0f; // do not accumulate backlog; keeps animation pace steady after a freeze
                    AdvanceFrame();
                }
                yield return null;
            }
        }

        private void AdvanceFrame()
        {
            _currentFrame = (_currentFrame + 1) % _animationFrames.Count;
            _spriteRenderer.sprite = _animationFrames[_currentFrame];
            StartCoroutine(ShakeAnimation());
            if (_currentFrame == 0)
                StartCoroutine(GrowImage());
        }

        IEnumerator GrowImage()
        {
            _spriteRenderer.transform.localScale = Vector3.zero;
            while (_spriteRenderer.transform.localScale.x < 1f)
            {
                float delta = Mathf.Min(Time.unscaledDeltaTime, _maxDeltaStep);
                _spriteRenderer.transform.localScale += Vector3.one * _growRate * delta;
                yield return null;
            }
            _spriteRenderer.transform.localScale = Vector3.one;
        }

        IEnumerator ShakeAnimation()
        {
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                float delta = Mathf.Min(Time.unscaledDeltaTime, _maxDeltaStep);
                elapsed += delta;
                float progress = Mathf.Clamp01(elapsed / _shakeDuration);
                float shakeAmount = _shakeIntensity * (1f - progress);
                Vector3 randomOffset = new Vector3(
                    Random.Range(-shakeAmount, shakeAmount),
                    Random.Range(-shakeAmount, shakeAmount),
                    0f
                );
                transform.localPosition = _originalPosition + randomOffset;
                yield return null;
            }
            transform.localPosition = _originalPosition;
        }

    }
}