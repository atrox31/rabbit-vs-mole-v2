using Interface.Element;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System.Collections;

namespace Interface
{
    public class GUIPanel : InterfaceElement
    {
        [Header("Panel References")]
        [SerializeField] private GameObject _panelObject;
        [SerializeField] private GameObject _panelHeader;
        [SerializeField] private TextMeshProUGUI _panelTitle;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private LocalizeStringEvent _localizeTitleEvent;

        [Header("Panel Properties")]
        [SerializeField] private string _panelName;
        private LocalizedString _localizedPanelName;

        [Header("Layout Settings")]
        [SerializeField] private float _elementSpacing = 10f;
        [SerializeField] private float _topPadding = 100f;
        [SerializeField] private float _bottomPadding = 46f;

        [Header("Scroll Settings")]
        [SerializeField] private bool _enableScrollWhenNeeded = true;
        [SerializeField] private float _scrollMargin = 10f;

        private List<InterfaceElement> _elements = new List<InterfaceElement>();
        private List<InterfaceElement> _bottomElements = new List<InterfaceElement>();
        
        // Scroll components
        private ScrollRect _scrollRect;
        private RectTransform _scrollViewport;
        private RectTransform _scrollContent;
        private Scrollbar _verticalScrollbar;
        private RectTransform _originalContentContainer;
        private bool _scrollSetup = false;
        
        // Animation settings
        private PanelAnimationType _animationType = PanelAnimationType.Fade;
        private SlideDirection _slideDirection = SlideDirection.Left;
        private float _slideDistance = 500f;
        private float _bounceAmount = 50f;
        private Vector2 _originalPosition;
        private bool _originalPositionSaved = false;

        protected override void Setup()
        {
            base.Setup();

            if (_panelObject == null)
                _panelObject = gameObject;

            if (_contentContainer == null)
            {
                _contentContainer = GetComponent<RectTransform>();
            }

            if (_panelTitle != null && !_elementTextMeshes.Contains(_panelTitle))
            {
                _elementTextMeshes.Add(_panelTitle);
            }

            if (_panelHeader != null)
            {
                Image headerImage = _panelHeader.GetComponent<Image>();
                if (headerImage != null && !_elementImages.Contains(headerImage))
                {
                    _elementImages.Add(headerImage);
                }
            }

            if (_localizeTitleEvent == null && _panelTitle != null)
            {
                _localizeTitleEvent = _panelTitle.GetComponent<LocalizeStringEvent>();
                if (_localizeTitleEvent == null)
                {
                    _localizeTitleEvent = _panelTitle.gameObject.AddComponent<LocalizeStringEvent>();
                }
                
                if (_localizeTitleEvent != null)
                {
                    _localizeTitleEvent.OnUpdateString.AddListener(OnLocalizedStringUpdate);
                }
            }

            if (_panelTitle != null)
            {
                if (_localizeTitleEvent != null && _localizedPanelName != null)
                {
                    _localizeTitleEvent.StringReference = _localizedPanelName;
                    _localizeTitleEvent.RefreshString();
                }
                else if (!string.IsNullOrEmpty(_panelName))
                {
                    _panelTitle.text = _panelName;
                }
            }
        }

        private void OnLocalizedStringUpdate(string localizedString)
        {
            if (_panelTitle != null)
            {
                _panelTitle.text = localizedString;
            }
        }

        public void SetPanelName(string name)
        {
            _panelName = name;
            _localizedPanelName = null;
            if (_panelTitle != null)
            {
                if (_localizeTitleEvent != null)
                {
                    _localizeTitleEvent.StringReference = null;
                }
                _panelTitle.text = name;
            }
        }

        public void SetPanelName(LocalizedString localizedName)
        {
            _localizedPanelName = localizedName;
            Setup();
            
            if (_panelTitle != null)
            {
                if (_localizeTitleEvent != null)
                {
                    _localizeTitleEvent.StringReference = localizedName;
                    _localizeTitleEvent.RefreshString();
                }
                else
                {
                    localizedName.GetLocalizedStringAsync().Completed += (op) =>
                    {
                        if (op.IsDone && op.Result != null && _panelTitle != null)
                        {
                            _panelTitle.text = op.Result;
                        }
                    };
                }
            }
        }

        public void AddElement(InterfaceElement element, bool isBottomElement = false)
        {
            if (element == null) return;

            if (isBottomElement)
            {
                _bottomElements.Add(element);
            }
            else
            {
                _elements.Add(element);
            }

            element.transform.SetParent(_contentContainer, false);
            
            RectTransform elementRect = element.GetRectTransform();
            if (elementRect != null)
            {
                if (isBottomElement)
                {
                    elementRect.anchorMin = new Vector2(0.5f, 0f);
                    elementRect.anchorMax = new Vector2(0.5f, 0f);
                    elementRect.pivot = new Vector2(0.5f, 0f);
                }
                else
                {
                    elementRect.anchorMin = new Vector2(0.5f, 1f);
                    elementRect.anchorMax = new Vector2(0.5f, 1f);
                    elementRect.pivot = new Vector2(0.5f, 1f);
                }
                
                Interface.Element.GUISlider guiSlider = element as Interface.Element.GUISlider;
                if (guiSlider != null)
                {
                    guiSlider.FixSliderLayout();
                }
            }
            
            RepositionElements();
            
            if (_enableScrollWhenNeeded && gameObject.activeInHierarchy)
            {
                StartCoroutine(CheckScrollAfterLayout());
            }
        }
        
        private IEnumerator CheckScrollAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            CheckAndSetupScroll();
        }
        
        private IEnumerator UpdateScrollAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            yield return null;
            
            RepositionElements();
            
            yield return new WaitForEndOfFrame();
            
            UpdateScrollContentSize();
            
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void RepositionElements()
        {
            if (_contentContainer == null) return;

            RectTransform containerForMiddleElements = _scrollSetup && _scrollContent != null ? _scrollContent : _contentContainer;
            
            float currentY = _scrollSetup ? 0f : -_topPadding;

            foreach (var element in _elements)
            {
                if (element == null) continue;

                RectTransform elementRect = element.GetRectTransform();
                if (elementRect == null) continue;
                
                if (element.transform.parent != containerForMiddleElements)
                {
                    element.transform.SetParent(containerForMiddleElements, false);
                }

                elementRect.anchoredPosition = new Vector2(0, currentY);
                currentY -= (elementRect.sizeDelta.y + _elementSpacing);
            }

            float currentBottomY = _bottomPadding;

            for (int i = _bottomElements.Count - 1; i >= 0; i--)
            {
                var element = _bottomElements[i];
                if (element == null) continue;

                RectTransform elementRect = element.GetRectTransform();
                if (elementRect == null) continue;
                
                if (element.transform.parent != _contentContainer)
                {
                    element.transform.SetParent(_contentContainer, false);
                }

                elementRect.anchoredPosition = new Vector2(0, currentBottomY);
                currentBottomY += (elementRect.sizeDelta.y + _elementSpacing);
            }
            
            if (_scrollSetup)
            {
                UpdateScrollContentSize();
            }
        }

        public void ShowPanel()
        {
            RectTransform panelRect = GetRectTransform();
            if (panelRect != null && !_originalPositionSaved)
            {
                _originalPosition = panelRect.anchoredPosition;
                _originalPositionSaved = true;
            }
            else if (panelRect != null && _originalPositionSaved)
            {
                panelRect.anchoredPosition = _originalPosition;
            }
            
            gameObject.SetActive(true);
            Show();
            
            if (_enableScrollWhenNeeded)
            {
                StartCoroutine(CheckScrollAfterLayout());
            }
        }

        public void HidePanel()
        {
            Hide();
        }
         
        protected override IEnumerator AnimateCoroutine(AnimationStatus animationStatus)
        {
            Transform contentContainerTransform = _contentContainer != null ? _contentContainer : transform;
            
            _isAnimating = true;
            _animationTimer = 0f;
            
            if (_animationDuration <= 0f)
            {
                _animationDuration = 0.25f;
            }

            InterfaceElement[] allElements = GetComponentsInChildren<InterfaceElement>(true);
            List<InterfaceElement> childElements = new List<InterfaceElement>();
            
            foreach (var element in allElements)
            {
                if (element != null && element != this)
                {
                    Transform elementTransform = element.transform;
                    if (elementTransform.parent == contentContainerTransform || 
                        elementTransform.IsChildOf(contentContainerTransform))
                    {
                        childElements.Add(element);
                    }
                }
            }

            RectTransform panelRect = GetRectTransform();
            
            if ((_animationType & PanelAnimationType.Slide) != 0 && panelRect != null)
            {
                if (!_originalPositionSaved)
                {
                    _originalPosition = panelRect.anchoredPosition;
                    _originalPositionSaved = true;
                }
            }

            PanelAnimationHandler animationHandler = new PanelAnimationHandler(
                _animationType,
                _slideDirection,
                _slideDistance,
                _bounceAmount,
                _animationDuration,
                panelRect,
                _originalPosition,
                childElements,
                SetAlpha,
                (active) => gameObject.SetActive(active)
            );

            yield return StartCoroutine(animationHandler.Animate(animationStatus));

            if (animationStatus == AnimationStatus.Hide)
            {
                _isVisible = false;
                gameObject.SetActive(false);
            }

            _isAnimating = false;
        }
        public string PanelName => _panelName;

        public void SetAnimationDuration(float duration)
        {
            _animationDuration = duration;
        }
        
        public void SetAnimationSettings(PanelAnimationType animationType, SlideDirection slideDirection, float slideDistance, float bounceAmount)
        {
            _animationType = animationType;
            _slideDirection = slideDirection;
            _slideDistance = slideDistance;
            _bounceAmount = bounceAmount;
        }

        private void CheckAndSetupScroll()
        {
            if (_contentContainer == null) return;

            float totalContentHeight = CalculateContentHeight();
            RectTransform panelRect = GetRectTransform();
            if (panelRect == null) return;

            float headerHeight = GetHeaderHeight();
            float bottomButtonsHeight = GetBottomButtonsHeight();
            float availableHeight = panelRect.rect.height - headerHeight - bottomButtonsHeight;
            
            bool needsScroll = totalContentHeight > availableHeight;

            if (needsScroll && !_scrollSetup)
            {
                SetupScrollView();
            }
            else if (!needsScroll && _scrollSetup)
            {
                RemoveScrollView();
            }
            
            if (_scrollSetup && _scrollContent != null)
            {
                UpdateScrollContentSize();
            }
        }

        private float CalculateContentHeight()
        {
            if (_elements.Count == 0) return 0f;
            
            float totalHeight = 0f;
            
            foreach (var element in _elements)
            {
                if (element == null) continue;
                RectTransform elementRect = element.GetRectTransform();
                if (elementRect != null)
                {
                    float elementHeight = elementRect.rect.height > 0 ? elementRect.rect.height : elementRect.sizeDelta.y;
                    totalHeight += elementHeight + _elementSpacing;
                }
            }
            
            if (_elements.Count > 0)
            {
                totalHeight -= _elementSpacing;
            }
            
            totalHeight += _scrollMargin;
            
            return Mathf.Max(totalHeight, 0f);
        }
        
        private float GetHeaderHeight()
        {
            if (_panelHeader != null)
            {
                RectTransform headerRect = _panelHeader.GetComponent<RectTransform>();
                if (headerRect != null && headerRect.rect.height > 0)
                {
                    return headerRect.rect.height;
                }
            }
            return _topPadding;
        }
        
        private float GetBottomButtonsHeight()
        {
            float totalHeight = 0f;
            foreach (var element in _bottomElements)
            {
                if (element == null) continue;
                RectTransform elementRect = element.GetRectTransform();
                if (elementRect != null)
                {
                    totalHeight += elementRect.sizeDelta.y + _elementSpacing;
                }
            }
            if (totalHeight > 0)
            {
                totalHeight += _bottomPadding;
            }
            return totalHeight;
        }

        private void SetupScrollView()
        {
            if (_contentContainer == null) return;

            RectTransform panelRect = GetRectTransform();
            if (panelRect == null) return;

            // Zapisz oryginalny kontener
            _originalContentContainer = _contentContainer;

            float headerHeight = GetHeaderHeight();
            float bottomButtonsHeight = GetBottomButtonsHeight();

            GameObject viewportObj = new GameObject("ScrollViewport");
            viewportObj.transform.SetParent(panelRect, false);
            _scrollViewport = viewportObj.AddComponent<RectTransform>();
            
            _scrollViewport.anchorMin = new Vector2(0f, 0f);
            _scrollViewport.anchorMax = new Vector2(1f, 1f);
            _scrollViewport.offsetMin = new Vector2(0f, bottomButtonsHeight);
            _scrollViewport.offsetMax = new Vector2(-20f, -headerHeight);
            
            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 1f);
            viewportImage.raycastTarget = true;
            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            GameObject scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(panelRect, false);
            RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.offsetMin = new Vector2(-20f, bottomButtonsHeight);
            scrollbarRect.offsetMax = new Vector2(0f, -headerHeight);
            scrollbarRect.sizeDelta = new Vector2(20f, 0f);
            
            _verticalScrollbar = scrollbarObj.AddComponent<Scrollbar>();
            _verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            GameObject scrollbarBackground = new GameObject("Background");
            scrollbarBackground.transform.SetParent(scrollbarObj.transform, false);
            Image bgImage = scrollbarBackground.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            RectTransform bgRect = scrollbarBackground.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            GameObject scrollbarHandle = new GameObject("Handle");
            scrollbarHandle.transform.SetParent(scrollbarBackground.transform, false);
            Image handleImage = scrollbarHandle.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            RectTransform handleRect = scrollbarHandle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = new Vector2(1f, 1f);
            handleRect.sizeDelta = Vector2.zero;
            handleRect.anchoredPosition = Vector2.zero;
            
            _verticalScrollbar.targetGraphic = handleImage;
            _verticalScrollbar.handleRect = handleRect;

            GameObject contentObj = new GameObject("ScrollContent");
            contentObj.transform.SetParent(_scrollViewport, false);
            _scrollContent = contentObj.AddComponent<RectTransform>();
            
            _scrollContent.anchorMin = new Vector2(0.5f, 1f);
            _scrollContent.anchorMax = new Vector2(0.5f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.anchoredPosition = Vector2.zero;
            
            if (_scrollViewport != null && panelRect != null)
            {
                float panelWidth = panelRect.rect.width > 0 ? panelRect.rect.width : panelRect.sizeDelta.x;
                float viewportWidth = Mathf.Max(panelWidth - 20f, 0f);
                _scrollContent.sizeDelta = new Vector2(viewportWidth, 0f);
            }

            List<Transform> childrenToMove = new List<Transform>();
            foreach (var element in _elements)
            {
                if (element != null && element.transform.parent == _contentContainer)
                {
                    childrenToMove.Add(element.transform);
                }
            }
            
            foreach (var child in childrenToMove)
            {
                child.SetParent(_scrollContent, false);
            }

            _scrollRect = viewportObj.AddComponent<ScrollRect>();
            _scrollRect.content = _scrollContent;
            _scrollRect.viewport = _scrollViewport;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            _scrollRect.elasticity = 0.1f;
            _scrollRect.scrollSensitivity = 20f;
            
            _scrollRect.verticalScrollbar = _verticalScrollbar;
            _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            _scrollSetup = true;
            StartCoroutine(UpdateScrollAfterLayout());
        }

        private void UpdateScrollContentSize()
        {
            if (_scrollContent == null) return;

            float totalHeight = CalculateContentHeight();
            RectTransform viewportRect = _scrollViewport;
            if (viewportRect != null)
            {
                float contentWidth = viewportRect.rect.width > 0 ? viewportRect.rect.width : _scrollContent.sizeDelta.x;
                float viewportHeight = viewportRect.rect.height > 0 ? viewportRect.rect.height : 0f;
                float finalHeight = Mathf.Max(totalHeight, viewportHeight);
                
                _scrollContent.sizeDelta = new Vector2(contentWidth, finalHeight);
                
                if (_scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                }
            }
            else
            {
                _scrollContent.sizeDelta = new Vector2(_scrollContent.sizeDelta.x, totalHeight);
            }
        }

        private void RemoveScrollView()
        {
            if (!_scrollSetup) return;

            if (_scrollContent != null && _originalContentContainer != null)
            {
                List<Transform> childrenToMove = new List<Transform>();
                foreach (Transform child in _scrollContent)
                {
                    childrenToMove.Add(child);
                }
                
                foreach (var child in childrenToMove)
                {
                    child.SetParent(_originalContentContainer, false);
                }
            }

            if (_scrollViewport != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_scrollViewport.gameObject);
                }
                else
                {
                    DestroyImmediate(_scrollViewport.gameObject);
                }
            }

            _scrollRect = null;
            _scrollViewport = null;
            _scrollContent = null;
            _scrollSetup = false;
            
            RepositionElements();
        }

        private void OnDestroy()
        {
            if (_localizeTitleEvent != null)
            {
                _localizeTitleEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }
        }
    }
}