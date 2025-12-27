using Extensions;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using static UnityEngine.ParticleSystem;
using Random = UnityEngine.Random;

namespace RabbitVsMole.InteractableGameObject.Base
{
    public abstract class VisualBase : MonoBehaviour
    {
        public enum AnimationType
        {
            Scale,
            Position,
            Both
        }

        public enum EaseType
        {
            Linear,
            SmoothStep,
            EaseOutCubic
        }

        [Flags]
        public enum AxisFlags
        {
            None = 0,
            X = 1 << 0, // 1
            Y = 1 << 1, // 2
            Z = 1 << 2  // 4
        }

        [Header("Animation Settings")]
        [SerializeField] protected AnimationType _animationType;

        // --- Scale Group ---
        [ShowIf("IsScaleOrBoth")]
        [BoxGroup("Scale Animation Settings")]
        [SerializeField] protected float _scaleUpDuration = 1.0f;

        [ShowIf("IsScaleOrBoth")]
        [BoxGroup("Scale Animation Settings")]
        [SerializeField] protected float _timeRandomness = 0.2f;

        [ShowIf("IsScaleOrBoth")]
        [BoxGroup("Scale Animation Settings")]
        [SerializeField] protected EaseType _easeType = EaseType.SmoothStep;

        // --- Position Group ---
        [ShowIf("IsPositionOrBoth")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected Transform _position0;

        [ShowIf("IsPositionOrBoth")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected Transform _position1;

        [ShowIf("IsPositionOrBoth")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected float _morphingTime = 1.0f;

        // Nested conditions inside a group:
        [ShowIf("ShouldShowRotation")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected bool _randomRotation = false;

        [ShowIf("ShouldShowAxisFlags")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] private AxisFlags _affectedAxes;

        [ShowIf("IsPositionOrBoth")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected bool _shake = false;

        [ShowIf("IsPositionOrBoth")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected float _shakeIntensity = 0.1f;

        [ShowIf("IsPositionOrBoth")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected bool _applyRandomPosition = true;

        [ShowIf("IsPositionOrBoth")]
        [BoxGroup("Position Animation Settings")]
        [SerializeField] protected float _randomPositionRange = 0.5f;

        // --- Visuals ---
        [BoxGroup("Visual References")]
        [SerializeField] protected Transform _model;
        [BoxGroup("Visual References")]
        [SerializeField] protected ParticleSystem _particleSystemOnShow;
        [BoxGroup("Visual References")]
        [SerializeField] protected ParticleSystem _particleSystemOnHide;

        // --- Helper Methods ---
        private bool IsScaleOrBoth() => _animationType == AnimationType.Scale || _animationType == AnimationType.Both;
        private bool IsPositionOrBoth() => _animationType == AnimationType.Position || _animationType == AnimationType.Both;
        private bool ShouldShowRotation() => IsPositionOrBoth();
        private bool ShouldShowAxisFlags() => IsPositionOrBoth() && _randomRotation;

        protected Coroutine _activeAnimation;
        protected List<ScaleElement> _scaleElements = new List<ScaleElement>();
        protected bool _isPaused = false;
        protected float _currentProgress = 0f;

        public float Progress => _currentProgress;
        public bool IsReady => _isReady;
        private bool _isReady;
        public bool IsAnimating => _activeAnimation != null;

        public bool IsPaused => _isPaused;
        public virtual void Pause() =>
            _isPaused = true;

        public virtual void Resume() =>
            _isPaused = false;

        public void SetDuration(float value)
        {
            _morphingTime = value;
            _scaleUpDuration = value;
        }

        protected class ScaleElement
        {
            public readonly Vector3 OriginalScale;
            public readonly Transform Model;
            public readonly float Duration;
            public float CurrentProgress;

            public ScaleElement(Transform model, float baseDuration, float randomness)
            {
                Model = model;
                OriginalScale = model.localScale;
                Duration = Mathf.Max(0.1f, baseDuration + Random.Range(-randomness, randomness));
                CurrentProgress = 0;
            }
        }

        protected virtual void Awake()
        {
            if (_animationType == AnimationType.Scale || _animationType == AnimationType.Both)
            {
                InitializeScaleElements();
            }
        }

        protected virtual void InitializeScaleElements()
        {
            _scaleElements = new List<ScaleElement>();
            foreach (Transform child in _model.transform)
            {
                _scaleElements.Add(new ScaleElement(child, _scaleUpDuration, _timeRandomness));
                child.localScale = Vector3.zero;
            }
        }
        void Start()
        {
            Show();
        }

        public virtual void Show()
        {
            StartAnimation(true);
        }

        public virtual void Hide()
        {
            StartAnimation(false);
        }


        protected virtual void StartAnimation(bool show)
        {
            if (_activeAnimation != null)
            {
                StopCoroutine(_activeAnimation);
            }

            if (_animationType == AnimationType.Scale)
            {
                _activeAnimation = StartCoroutine(AnimateScale(show));
            }
            else if (_animationType == AnimationType.Position)
            {
                _activeAnimation = StartCoroutine(AnimatePosition(show));
            }
            else if (_animationType == AnimationType.Both)
            {
                _activeAnimation = StartCoroutine(AnimateBoth(show));
            }
        }

        protected virtual IEnumerator AnimateScale(bool grow)
        {
            bool isAnimating = true;
            _isPaused = false;

            while (isAnimating)
            {
                if (!_isPaused)
                {
                    isAnimating = false;
                    float deltaTime = Time.deltaTime;
                    float totalProgress = 0f;

                    foreach (var element in _scaleElements)
                    {
                        if (grow)
                            element.CurrentProgress += deltaTime / element.Duration;
                        else
                            element.CurrentProgress -= deltaTime / element.Duration;

                        element.CurrentProgress = Mathf.Clamp01(element.CurrentProgress);
                        totalProgress += element.CurrentProgress;

                        float easedProgress = ApplyEasing(element.CurrentProgress);
                        element.Model.localScale = Vector3.Lerp(Vector3.zero, element.OriginalScale, easedProgress);
                        
                        if (grow ? element.CurrentProgress < 1f : element.CurrentProgress > 0f)
                        {
                            isAnimating = true;
                        }
                    }

                    if (_scaleElements.Count > 0)
                    {
                        _currentProgress = totalProgress / _scaleElements.Count;
                    }
                }

                yield return null;
            }
            if(grow)
                _isReady = true;
            _activeAnimation = null;
            _isPaused = false;
        }
        private Quaternion GetRandomRotation(AxisFlags flags, Vector3 baseRotation = default)
        {
            if (flags.HasFlag(AxisFlags.X)) baseRotation.x = Random.value * 360f;
            if (flags.HasFlag(AxisFlags.Y)) baseRotation.y = Random.value * 360f;
            if (flags.HasFlag(AxisFlags.Z)) baseRotation.z = Random.value * 360f;
            return Quaternion.Euler(baseRotation);
        }

        protected virtual IEnumerator AnimatePosition(bool show)
        {
            if (_model == null)
            {
                Debug.LogError("Model transform is not assigned for position animation!", this);
                yield break;
            }

            if (show && _applyRandomPosition)
            {
                _position1.localPosition += new Vector3(
                    Random.Range(-_randomPositionRange, _randomPositionRange),
                    0f,
                    Random.Range(-_randomPositionRange, _randomPositionRange));
            }

            Transform startTransform = show ? _position0 : _position1;
            Transform endTransform = show ? _position1 : _position0;
            ParticleSystem particles = show ? _particleSystemOnShow : _particleSystemOnHide;
            bool destroyOnFinish = !show;

            float elapsedTime = 0f;
            _isPaused = false;

            Quaternion startRotation = startTransform != null ? startTransform.localRotation : _model.localRotation;
            Quaternion endRotation = _randomRotation
                ? GetRandomRotation(_affectedAxes, _model.transform.rotation.eulerAngles)
                : (endTransform != null ? endTransform.localRotation : _model.localRotation);

            Vector3 startPosition = startTransform != null ? startTransform.localPosition : _model.localPosition;
            Vector3 endPosition = endTransform != null ? endTransform.localPosition : _model.localPosition;

            if (particles != null)
                particles.SafePlay();

            if (startTransform != null)
            {
                _model.SetLocalPositionAndRotation(startPosition, startRotation);
            }

            while (elapsedTime < _morphingTime)
            {
                if (!_isPaused)
                {
                    elapsedTime += Time.deltaTime;
                }

                float progress = Mathf.Clamp01(elapsedTime / _morphingTime);
                _currentProgress = progress;

                if (_shake)
                    progress = Mathf.Clamp01(progress + Random.Range(-_shakeIntensity, _shakeIntensity));

                _model.SetLocalPositionAndRotation(
                    Vector3.Slerp(startPosition, endPosition, progress),
                    Quaternion.Lerp(startRotation, endRotation, progress));

                yield return null;
            }

            _model.SetLocalPositionAndRotation(endPosition, endRotation);
            endTransform?.SetLocalPositionAndRotation(endPosition, endRotation);

            _activeAnimation = null;
            _isPaused = false;
            _isReady = true;
            
            if (destroyOnFinish)
            {
                particles.DetachAndDestroy();
                Destroy(gameObject);
            }
            else
            {
                particles.SafeStop();
            }
        }

        protected virtual IEnumerator AnimateBoth(bool show)
        {
            Coroutine scaleCoroutine = StartCoroutine(AnimateScale(show));
            Coroutine positionCoroutine = StartCoroutine(AnimatePosition(show));

            yield return scaleCoroutine;
            yield return positionCoroutine;

            _activeAnimation = null;
        }

        protected virtual float ApplyEasing(float linearT)
        {
            return _easeType switch
            {
                EaseType.Linear => linearT,
                EaseType.SmoothStep => Mathf.SmoothStep(0f, 1f, linearT),
                EaseType.EaseOutCubic => 1f - Mathf.Pow(1f - linearT, 3f),
                _ => linearT,
            };
        }
    }
}