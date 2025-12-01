using System;
using System.Collections;
using System.Collections.Generic;
using Interface.Element;
using UnityEngine;

namespace Interface
{
    public class PanelAnimationHandler
    {
        private readonly PanelAnimationType _animationType;
        private readonly SlideDirection _slideDirection;
        private readonly float _slideDistance;
        private readonly float _bounceAmount;
        private readonly float _animationDuration;
        private readonly RectTransform _panelRect;
        private readonly Vector2 _originalPosition;
        private readonly List<InterfaceElement> _childElements;
        private readonly Action<float> _setAlphaAction;
        private readonly Action<bool> _setActiveAction;

        public PanelAnimationHandler(
            PanelAnimationType animationType,
            SlideDirection slideDirection,
            float slideDistance,
            float bounceAmount,
            float animationDuration,
            RectTransform panelRect,
            Vector2 originalPosition,
            List<InterfaceElement> childElements,
            Action<float> setAlphaAction,
            Action<bool> setActiveAction)
        {
            _animationType = animationType;
            _slideDirection = slideDirection;
            _slideDistance = slideDistance;
            _bounceAmount = bounceAmount;
            _animationDuration = animationDuration;
            _panelRect = panelRect;
            _originalPosition = originalPosition;
            _childElements = childElements;
            _setAlphaAction = setAlphaAction;
            _setActiveAction = setActiveAction;
        }

        public IEnumerator Animate(AnimationStatus animationStatus)
        {
            if (animationStatus == AnimationStatus.Show)
            {
                _setActiveAction?.Invoke(true);
            }

            float duration = _animationDuration > 0f ? _animationDuration : 0.25f;
            List<IEnumerator> activeAnimations = new List<IEnumerator>();

            if ((_animationType & PanelAnimationType.Fade) != 0)
            {
                activeAnimations.Add(AnimateFade(animationStatus, duration));
            }

            if ((_animationType & PanelAnimationType.Slide) != 0 && _panelRect != null)
            {
                activeAnimations.Add(AnimateSlide(animationStatus, duration));
            }

            if (activeAnimations.Count > 0)
            {
                yield return StartCoroutinesParallel(activeAnimations);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private IEnumerator AnimateFade(AnimationStatus animationStatus, float duration)
        {
            if (animationStatus == AnimationStatus.Show)
            {
                _setAlphaAction?.Invoke(0f);
                foreach (var element in _childElements)
                {
                    element?.SetAlpha(0f);
                }
            }

            float animationTimer = 0f;

            while (animationTimer < duration)
            {
                animationTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(animationTimer / duration);

                float targetAlpha = animationStatus == AnimationStatus.Show ? 1f : 0f;
                float startAlpha = animationStatus == AnimationStatus.Show ? 0f : 1f;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

                _setAlphaAction?.Invoke(currentAlpha);

                foreach (var element in _childElements)
                {
                    element?.SetAlpha(currentAlpha);
                }

                yield return null;
            }

            float finalAlpha = animationStatus == AnimationStatus.Show ? 1f : 0f;
            _setAlphaAction?.Invoke(finalAlpha);

            foreach (var element in _childElements)
            {
                element?.SetAlpha(finalAlpha);
            }
        }

        private IEnumerator AnimateSlide(AnimationStatus animationStatus, float duration)
        {
            Vector2 slideVector = GetSlideDirectionVector(_slideDirection);

            Vector2 startPosition;
            Vector2 targetPosition;
            Vector2 bounceStartPosition;

            if (animationStatus == AnimationStatus.Show)
            {
                startPosition = _originalPosition + slideVector * _slideDistance;
                targetPosition = _originalPosition;
                bounceStartPosition = _originalPosition - slideVector * _bounceAmount;
            }
            else
            {
                startPosition = _originalPosition;
                targetPosition = _originalPosition + slideVector * _slideDistance;
                bounceStartPosition = _originalPosition - slideVector * _bounceAmount;
            }

            _panelRect.anchoredPosition = animationStatus == AnimationStatus.Show ? startPosition : bounceStartPosition;

            const float bounceDuration = 0.2f;
            float slideDuration = duration - bounceDuration;
            float animationTimer = 0f;

            while (animationTimer < duration)
            {
                animationTimer += Time.deltaTime;
                Vector2 currentPosition;

                if (animationStatus == AnimationStatus.Show)
                {
                    if (animationTimer < bounceDuration)
                    {
                        float bounceProgress = animationTimer / bounceDuration;
                        bounceProgress = 1f - Mathf.Pow(1f - bounceProgress, 3f);
                        currentPosition = Vector2.Lerp(startPosition, bounceStartPosition, bounceProgress);
                    }
                    else
                    {
                        float slideProgress = (animationTimer - bounceDuration) / slideDuration;
                        slideProgress = 1f - Mathf.Pow(1f - slideProgress, 2f);
                        currentPosition = Vector2.Lerp(bounceStartPosition, targetPosition, slideProgress);
                    }
                }
                else
                {
                    if (animationTimer < bounceDuration)
                    {
                        float bounceProgress = animationTimer / bounceDuration;
                        bounceProgress = 1f - Mathf.Pow(1f - bounceProgress, 3f);
                        currentPosition = Vector2.Lerp(startPosition, bounceStartPosition, bounceProgress);
                    }
                    else
                    {
                        float slideProgress = (animationTimer - bounceDuration) / slideDuration;
                        slideProgress = Mathf.Pow(slideProgress, 2f);
                        currentPosition = Vector2.Lerp(bounceStartPosition, targetPosition, slideProgress);
                    }
                }

                _panelRect.anchoredPosition = currentPosition;
                yield return null;
            }

            _panelRect.anchoredPosition = targetPosition;
        }

        private IEnumerator StartCoroutinesParallel(List<IEnumerator> coroutines)
        {
            if (coroutines == null || coroutines.Count == 0)
            {
                yield break;
            }

            while (coroutines.Count > 0)
            {
                for (int i = coroutines.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (!coroutines[i].MoveNext())
                        {
                            coroutines.RemoveAt(i);
                        }
                    }
                    catch
                    {
                        coroutines.RemoveAt(i);
                    }
                }

                yield return null;
            }
        }

        private Vector2 GetSlideDirectionVector(SlideDirection direction)
        {
            switch (direction)
            {
                case SlideDirection.Up:
                    return Vector2.up;
                case SlideDirection.Down:
                    return Vector2.down;
                case SlideDirection.Left:
                    return Vector2.left;
                case SlideDirection.Right:
                    return Vector2.right;
                default:
                    return Vector2.left;
            }
        }
    }
}

