using Extensions;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        // --- Random Rotation Group ---
        [BoxGroup("Random Rotation")]
        [SerializeField] protected bool _randomRotation = false;

        [ShowIf("_randomRotation")]
        [BoxGroup("Random Rotation")]
        [SerializeField] protected AxisFlags _affectedAxes;

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

        // --- Private Fields ---
        protected Coroutine _activeAnimation;
        protected List<ScaleElement> _scaleElements = new List<ScaleElement>();
        protected bool _isPaused = false;
        protected float _currentProgress = 0f;
        private bool _isReady;

        // --- Public Properties ---
        public float Progress => _currentProgress;
        public bool IsReady => _isReady;
        public bool IsAnimating => _activeAnimation != null;
        public bool IsPaused => _isPaused;

        // --- Public Methods ---
        public virtual void Pause() => _isPaused = true;
        public virtual void Resume() => _isPaused = false;

        public void SetDuration(float value)
        {
            if (value < 0f)
            {
                Debug.LogWarning($"Duration cannot be negative. Setting to 0.1f.", this);
                value = 0.1f;
            }
            _morphingTime = value;
            _scaleUpDuration = value;
        }

        protected class ScaleElement
        {
            public readonly Vector3 OriginalScale;
            public readonly Vector3 OriginalPosition;
            public readonly Quaternion OriginalRotation;
            public readonly Quaternion TargetRotation;
            public readonly Transform Model;
            public readonly float Duration;
            public float CurrentProgress;

            public ScaleElement(Transform model, float baseDuration, float randomness, Quaternion targetRotation)
            {
                Model = model;
                OriginalScale = model.localScale;
                OriginalPosition = model.localPosition;
                OriginalRotation = model.localRotation;
                TargetRotation = targetRotation;
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
            if (!TryGetComponent(out _outline))
            {
                DebugHelper.LogError(this, "You forgot to add component Outline to this interactable!");
                return;
            }
            _outline.enabled = false;
        }

        protected virtual void InitializeScaleElements()
        {
            if (_model == null)
            {
                Debug.LogError("Model transform is not assigned! Cannot initialize scale elements.", this);
                return;
            }

            _scaleElements = new List<ScaleElement>();
            foreach (Transform child in _model.transform)
            {
                Quaternion targetRotation = GetTargetRotation(child.localRotation.eulerAngles);
                _scaleElements.Add(new ScaleElement(child, _scaleUpDuration, _timeRandomness, targetRotation));
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
            StopActiveAnimation();

            _activeAnimation = _animationType switch
            {
                AnimationType.Scale => StartCoroutine(AnimateScale(show)),
                AnimationType.Position => StartCoroutine(AnimatePosition(show)),
                AnimationType.Both => StartCoroutine(AnimateBoth(show)),
                _ => null
            };
        }

        protected virtual void StopActiveAnimation()
        {
            if (_activeAnimation != null)
            {
                StopCoroutine(_activeAnimation);
                _activeAnimation = null;
            }
        }

        protected virtual IEnumerator AnimateScale(bool grow)
        {
            _isPaused = false;
            InitializeScaleAnimation();

            bool isAnimating = true;
            while (isAnimating)
            {
                if (!_isPaused)
                {
                    isAnimating = UpdateScaleAnimation(grow);
                }
                yield return null;
            }

            FinalizeScaleAnimation(grow);
        }

        private void InitializeScaleAnimation()
        {
            // Apply random rotation at the start if enabled, and preserve positions
            // This prevents objects from appearing at wrong positions when scaling from zero
            // (which can happen if pivot point is not at center or if position was modified elsewhere)
            foreach (var element in _scaleElements)
            {
                element.Model.localPosition = element.OriginalPosition;
                element.Model.localRotation = element.TargetRotation;
            }
        }

        private bool UpdateScaleAnimation(bool grow)
        {
            bool isAnimating = false;
            float deltaTime = Time.deltaTime;
            float totalProgress = 0f;

            foreach (var element in _scaleElements)
            {
                UpdateElementProgress(element, grow, deltaTime);
                totalProgress += element.CurrentProgress;

                float easedProgress = ApplyEasing(element.CurrentProgress);
                element.Model.localScale = Vector3.Lerp(Vector3.zero, element.OriginalScale, easedProgress);
                
                // Ensure position and rotation remain unchanged during scale animation
                element.Model.localPosition = element.OriginalPosition;
                element.Model.localRotation = element.TargetRotation;
                
                if (grow ? element.CurrentProgress < 1f : element.CurrentProgress > 0f)
                {
                    isAnimating = true;
                }
            }

            if (_scaleElements.Count > 0)
            {
                _currentProgress = totalProgress / _scaleElements.Count;
            }

            return isAnimating;
        }

        private void UpdateElementProgress(ScaleElement element, bool grow, float deltaTime)
        {
            if (grow)
                element.CurrentProgress += deltaTime / element.Duration;
            else
                element.CurrentProgress -= deltaTime / element.Duration;

            element.CurrentProgress = Mathf.Clamp01(element.CurrentProgress);
        }

        private void FinalizeScaleAnimation(bool grow)
        {
            if (grow)
                _isReady = true;
            _activeAnimation = null;
            _isPaused = false;
        }
        private Quaternion GetTargetRotation(Vector3 baseRotation)
        {
            return _randomRotation 
                ? GetRandomRotation(_affectedAxes, baseRotation)
                : Quaternion.Euler(baseRotation);
        }

        private Quaternion GetRandomRotation(AxisFlags flags, Vector3 baseRotation)
        {
            if (flags == AxisFlags.None)
                return Quaternion.Euler(baseRotation);

            Vector3 rotation = baseRotation;
            if (flags.HasFlag(AxisFlags.X)) rotation.x = Random.value * 360f;
            if (flags.HasFlag(AxisFlags.Y)) rotation.y = Random.value * 360f;
            if (flags.HasFlag(AxisFlags.Z)) rotation.z = Random.value * 360f;
            return Quaternion.Euler(rotation);
        }

        protected virtual IEnumerator AnimatePosition(bool show)
        {
            if (_model == null)
            {
                Debug.LogError("Model transform is not assigned for position animation!", this);
                yield break;
            }

            _isPaused = false;
            Vector3 randomOffset = ApplyRandomPositionOffset(show);
            var animationData = PreparePositionAnimation(show, randomOffset);
            
            PlayParticles(animationData.Particles);
            InitializePositionAnimation(animationData);

            yield return AnimatePositionCoroutine(animationData);

            FinalizePositionAnimation(animationData);
        }

        private Vector3 ApplyRandomPositionOffset(bool show)
        {
            if (!show || !_applyRandomPosition || _position1 == null)
                return Vector3.zero;

            return new Vector3(
                Random.Range(-_randomPositionRange, _randomPositionRange),
                0f,
                Random.Range(-_randomPositionRange, _randomPositionRange));
        }

        private PositionAnimationData PreparePositionAnimation(bool show, Vector3 randomOffset)
        {
            Transform startTransform = show ? _position0 : _position1;
            Transform endTransform = show ? _position1 : _position0;
            
            Vector3 startPosition = startTransform != null ? startTransform.localPosition : _model.localPosition;
            Vector3 endPosition = endTransform != null ? endTransform.localPosition : _model.localPosition;
            
            // Apply random offset to end position if showing
            if (show && randomOffset != Vector3.zero && _position1 != null)
            {
                endPosition += randomOffset;
            }

            Quaternion targetRotation = GetTargetRotationForPosition(endTransform);
            Quaternion startRotation = GetStartRotationForPosition(startTransform, targetRotation);

            return new PositionAnimationData
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                StartRotation = startRotation,
                TargetRotation = targetRotation,
                Particles = show ? _particleSystemOnShow : _particleSystemOnHide,
                DestroyOnFinish = !show,
                EndTransform = endTransform
            };
        }

        private Quaternion GetTargetRotationForPosition(Transform endTransform)
        {
            if (_randomRotation)
            {
                return GetRandomRotation(_affectedAxes, _model.localRotation.eulerAngles);
            }
            return endTransform != null ? endTransform.localRotation : _model.localRotation;
        }

        private Quaternion GetStartRotationForPosition(Transform startTransform, Quaternion targetRotation)
        {
            Quaternion startRotation = startTransform != null ? startTransform.localRotation : _model.localRotation;
            // If random rotation is enabled, apply it from the start instead of interpolating
            return _randomRotation ? targetRotation : startRotation;
        }

        private void PlayParticles(ParticleSystem particles)
        {
            if (particles != null)
                particles.SafePlay();
        }

        private void InitializePositionAnimation(PositionAnimationData data)
        {
            if (_position0 != null)
            {
                _model.SetLocalPositionAndRotation(data.StartPosition, data.StartRotation);
            }
            else if (_randomRotation)
            {
                // Apply random rotation immediately if no start transform
                _model.localRotation = data.TargetRotation;
            }
        }

        private IEnumerator AnimatePositionCoroutine(PositionAnimationData data)
        {
            float elapsedTime = 0f;

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

                Quaternion currentRotation = GetCurrentRotation(data, progress);
                
                _model.SetLocalPositionAndRotation(
                    Vector3.Slerp(data.StartPosition, data.EndPosition, progress),
                    currentRotation);

                yield return null;
            }
        }

        private Quaternion GetCurrentRotation(PositionAnimationData data, float progress)
        {
            // If random rotation is enabled, maintain it throughout the animation (no interpolation)
            return _randomRotation 
                ? data.TargetRotation 
                : Quaternion.Lerp(data.StartRotation, data.TargetRotation, progress);
        }

        private void FinalizePositionAnimation(PositionAnimationData data)
        {
            // Use target rotation (which is random if enabled) for final position
            _model.SetLocalPositionAndRotation(data.EndPosition, data.TargetRotation);
            data.EndTransform?.SetLocalPositionAndRotation(data.EndPosition, data.TargetRotation);

            _activeAnimation = null;
            _isPaused = false;
            _isReady = true;
            
            if (data.DestroyOnFinish)
            {
                data.Particles?.DetachAndDestroy();
                Destroy(gameObject);
            }
            else
            {
                data.Particles?.SafeStop();
            }
        }

        private struct PositionAnimationData
        {
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public Quaternion StartRotation;
            public Quaternion TargetRotation;
            public ParticleSystem Particles;
            public bool DestroyOnFinish;
            public Transform EndTransform;
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

        private Outline _outline;
        private Coroutine _outlineCoroutine;
        private const float MIN_WIDTH = 0f;
        private const float MAX_WIDTH = 10f;
        private const float DURATION = 0.5f;

        public void LightUp(PlayerType playerType) => StartEffect(true);
        public void LightDown(PlayerType playerType) => StartEffect(false);

        private void StartEffect(bool increase)
        {
            if (_outline == null) return;

            if (_outlineCoroutine != null)
                StopCoroutine(_outlineCoroutine);

            if (increase)
                _outline.enabled = true;

            _outlineCoroutine = StartCoroutine(AnimateOutline(increase));
        }

        private IEnumerator AnimateOutline(bool increase)
        {
            float startWidth = _outline.OutlineWidth;
            float targetWidth = increase ? MAX_WIDTH : MIN_WIDTH;
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / DURATION;

                _outline.OutlineWidth = Mathf.Lerp(startWidth, targetWidth, elapsed);

                yield return null;
            }

            if (!increase)
                _outline.enabled = false;

            _outline.OutlineWidth = targetWidth;
            _outlineCoroutine = null;
        }

    }
}