using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Element
{
    public class InterfaceElement : MonoBehaviour
    {
        [Header("Element References")]
        protected List<Image> _elementImages = new List<Image>();
        protected List<TextMeshProUGUI> _elementTextMeshes = new List<TextMeshProUGUI>();

        [Header("Element Settings")]
        [SerializeField] protected bool _startActive = true;
        [SerializeField] protected float _animationDuration = 0.25f;

        protected float _animationTimer = 0f;
        protected bool _isAnimating = false;
        protected bool _isVisible = false;

        protected virtual void Setup() 
        {
            CollectUIComponents();
        }

        protected virtual void CollectUIComponents()
        {
            _elementImages.Clear();
            _elementTextMeshes.Clear();

            Image[] allImages = GetComponentsInChildren<Image>(false);
            foreach (var img in allImages)
            {
                if (img == null) continue;
                
                if (!_elementImages.Contains(img))
                {
                    _elementImages.Add(img);
                }
            }

            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(false);
            foreach (var txt in allTexts)
            {
                if (txt == null) continue;
                
                if (!_elementTextMeshes.Contains(txt))
                {
                    _elementTextMeshes.Add(txt);
                }
            }
        }

        protected virtual void Awake()
        {
            gameObject.SetActive(_startActive);
            _isVisible = _startActive;
            Setup();
        }

        public virtual void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);

            foreach (var image in _elementImages)
            {
                if (image != null)
                {
                    var imageColor = image.color;
                    imageColor.a = alpha;
                    image.color = imageColor;
                }
            }

            foreach (var text in _elementTextMeshes)
            {
                if (text != null)
                {
                    var textColor = text.color;
                    textColor.a = alpha;
                    text.color = textColor;
                }
            }
        }

        public virtual void Show()
        {
            if (_isAnimating && _isVisible)
                return;

            gameObject.SetActive(true);
            _isVisible = true;
            StartCoroutine(AnimateCoroutine(AnimationStatus.Show));
        }

        public virtual void Hide()
        {
            if (_isAnimating && !_isVisible)
                return;

            StartCoroutine(AnimateCoroutine(AnimationStatus.Hide));
        }

        protected virtual IEnumerator AnimateCoroutine(AnimationStatus animationStatus)
        {
            _isAnimating = true;
            _animationTimer = 0f;

            InterfaceElement[] childElements = GetComponentsInChildren<InterfaceElement>(true);
            List<InterfaceElement> filteredChildren = new List<InterfaceElement>();
            
            foreach (var element in childElements)
            {
                if (element != null && element != this)
                {
                    filteredChildren.Add(element);
                }
            }

            if (animationStatus == AnimationStatus.Show)
            {
                SetAlpha(0f);
                foreach (var element in filteredChildren)
                {
                    if (element != null)
                    {
                        element.SetAlpha(0f);
                    }
                }
            }

            while (_animationTimer < _animationDuration)
            {
                _animationTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_animationTimer / _animationDuration);
                
                float targetAlpha = animationStatus == AnimationStatus.Show ? 1f : 0f;
                float startAlpha = animationStatus == AnimationStatus.Show ? 0f : 1f;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

                SetAlpha(currentAlpha);

                foreach (var element in filteredChildren)
                {
                    if (element != null)
                    {
                        element.SetAlpha(currentAlpha);
                    }
                }

                yield return null;
            }

            float finalAlpha = animationStatus == AnimationStatus.Show ? 1f : 0f;
            SetAlpha(finalAlpha);
            
            foreach (var element in filteredChildren)
            {
                if (element != null)
                {
                    element.SetAlpha(finalAlpha);
                }
            }

            if (animationStatus == AnimationStatus.Hide)
            {
                _isVisible = false;
                gameObject.SetActive(false);
            }

            _isAnimating = false;
        }

        public RectTransform GetRectTransform()
        {
            return GetComponent<RectTransform>();
        }

        public bool IsVisible => _isVisible;
        public bool IsAnimating => _isAnimating;
    }
}