using Interface.Element;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System.Collections;
using System.Linq;

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

        [Header("Sound")]
        [SerializeField] private AudioClip _showSound;
        [SerializeField] private AudioClip _hideSound;

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

        // Auto-scroll settings
        private float _autoScrollSpeed = 0f;
        private float _autoScrollDelay = 1f;
        private bool _autoScrollActive = false;
        private bool _autoScrollPaused = false;
        private float _lastScrollValue = 0f;
        private bool _isScrollingDown = true;

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

            AudioManager.PreloadClips(_showSound, _hideSound);
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

        public GameObject GetFirstButton()
        {
            foreach (var element in _elements.Concat(_bottomElements))
            {
                if(element.TryGetComponent(out GUIButton button))
                {
                    return button.gameObject;
                }
            }
            return null;
        }

        public InterfaceElement AddElement(InterfaceElement element, bool isBottomElement = false)
        {
            if (element == null) return null;

            if (isBottomElement)
                            _bottomElements.Add(element);
                        else
                            _elements.Add(element);
            

            element.transform.SetParent(_contentContainer, false);
            
            RectTransform elementRect = element.GetRectTransform();
            if (elementRect != null)
            {
                // Modyfikuj anchory tylko jeśli element na to pozwala
                if (element.AllowAnchorModification)
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
                }
                
                Interface.Element.GUISlider guiSlider = element as Interface.Element.GUISlider;
                if (guiSlider != null)
                {
                    guiSlider.FixSliderLayout();
                }
                
                Interface.Element.GUILabel guiLabel = element as Interface.Element.GUILabel;
                if (guiLabel != null)
                {
                    guiLabel.FixLabelLayout();
                }
                
                // Sprawdź czy element ma metodę FixCustomElementLayout (dla customowych elementów)
                var customElementType = element.GetType();
                var fixMethod = customElementType.GetMethod("FixCustomElementLayout", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (fixMethod != null)
                {
                    fixMethod.Invoke(element, null);
                }
            }
            
            RepositionElements();
            
            if (_enableScrollWhenNeeded && gameObject.activeInHierarchy)
            {
                StartCoroutine(CheckScrollAfterLayout());
            }

            if (isBottomElement)
                return _bottomElements.Last();
            else
                return _elements.Last();
        }

        public void RemoveElement(InterfaceElement element)
        {
            if (element == null) return;

            _elements.Remove(element);
            _bottomElements.Remove(element);
            
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
        
        private IEnumerator ResetScrollAndStartAutoScroll()
        {
            yield return new WaitForEndOfFrame();
            
            // Reset scrollbar to top
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
            
            // Reset auto-scroll state
            _isScrollingDown = true;
            _lastScrollValue = 1f;
            
            // Wait for panel animation to complete before starting auto-scroll
            float animationDelay = _animationDuration > 0f ? _animationDuration : 0.25f;
            yield return new WaitForSecondsRealtime(animationDelay);
            
            // Wait for animation to actually finish
            yield return new WaitUntil(() => !_isAnimating);
            
            // Start auto-scroll if enabled
            if (_autoScrollSpeed > 0f && _enableScrollWhenNeeded)
            {
                yield return new WaitForEndOfFrame();
                StartCoroutine(CheckScrollAndStartAutoScroll());
            }
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
            Interface.Element.GUILabel previousLabel = null;

            foreach (var element in _elements)
            {
                if (element == null) continue;

                RectTransform elementRect = element.GetRectTransform();
                if (elementRect == null) continue;
                
                if (element.transform.parent != containerForMiddleElements)
                {
                    element.transform.SetParent(containerForMiddleElements, false);
                    Interface.Element.GUILabel guiLabel = element as Interface.Element.GUILabel;
                    if (guiLabel != null)
                    {
                        guiLabel.FixLabelLayout();
                    }
                }

                elementRect.anchoredPosition = new Vector2(0, currentY);
                
                Interface.Element.GUILabel currentLabel = element as Interface.Element.GUILabel;
                float spacing = (currentLabel != null && previousLabel != null) ? 0f : _elementSpacing;
                currentY -= (elementRect.sizeDelta.y + spacing);
                
                previousLabel = currentLabel;
            }

            float currentBottomY = _bottomPadding;
            
            // Calculate total width of all bottom elements for horizontal centering
            float totalWidth = 0f;
            List<RectTransform> validBottomElements = new List<RectTransform>();
            
            foreach (var element in _bottomElements)
            {
                if (element == null) continue;
                
                RectTransform elementRect = element.GetRectTransform();
                if (elementRect == null) continue;
                
                if (element.transform.parent != _contentContainer)
                {
                    element.transform.SetParent(_contentContainer, false);
                }
                
                // Ensure bottom elements are centered horizontally
                if (element.AllowAnchorModification)
                {
                    elementRect.anchorMin = new Vector2(0.5f, 0f);
                    elementRect.anchorMax = new Vector2(0.5f, 0f);
                    elementRect.pivot = new Vector2(0.5f, 0f);
                }
                
                validBottomElements.Add(elementRect);
                totalWidth += elementRect.sizeDelta.x;
            }
            
            // Add spacing between elements
            if (validBottomElements.Count > 1)
            {
                totalWidth += _elementSpacing * (validBottomElements.Count - 1);
            }
            
            // Position elements horizontally, centered
            float startX = -totalWidth / 2f;
            float currentX = startX;
            
            foreach (var elementRect in validBottomElements)
            {
                float elementWidth = elementRect.sizeDelta.x;
                currentX += elementWidth / 2f; // Center of element
                
                elementRect.anchoredPosition = new Vector2(currentX, currentBottomY);
                
                currentX += elementWidth / 2f; // Move to end of element
                if (elementRect != validBottomElements[validBottomElements.Count - 1])
                {
                    currentX += _elementSpacing; // Add spacing before next element
                }
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
            
            //StartCoroutine(EnsureButtonsInteractableAfterDelay());
            
            if (_enableScrollWhenNeeded)
            {
                StartCoroutine(CheckScrollAfterLayout());
            }
            
            // Reset scrollbar to top and start auto-scroll if enabled
            StartCoroutine(ResetScrollAndStartAutoScroll());

            AudioManager.PlaySoundUI(_showSound);
        }


        public void HidePanel()
        {
            // Stop auto-scroll when hiding panel
            if (_autoScrollActive)
            {
                _autoScrollActive = false;
                _autoScrollPaused = false;
                if (_verticalScrollbar != null)
                {
                    _verticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
                }
            }
            
            Hide();

            AudioManager.PlaySoundUI(_showSound);
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
            else if (animationStatus == AnimationStatus.Show)
            {
                // Ensure all buttons are interactable after animation completes
                //EnsureButtonsInteractable();
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
            Interface.Element.GUILabel previousLabel = null;
            
            foreach (var element in _elements)
            {
                if (element == null) continue;
                RectTransform elementRect = element.GetRectTransform();
                if (elementRect != null)
                {
                    float elementHeight = elementRect.rect.height > 0 ? elementRect.rect.height : elementRect.sizeDelta.y;
                    
                    Interface.Element.GUILabel currentLabel = element as Interface.Element.GUILabel;
                    float spacing = (currentLabel != null && previousLabel != null) ? 0f : _elementSpacing;
                    totalHeight += elementHeight + spacing;
                    
                    previousLabel = currentLabel;
                }
            }
            
            if (_elements.Count > 0 && previousLabel == null)
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

            _originalContentContainer = _contentContainer;

            float headerHeight = GetHeaderHeight();
            float bottomButtonsHeight = GetBottomButtonsHeight();

            GameObject viewportObj = new GameObject("ScrollViewport");
            viewportObj.transform.SetParent(panelRect, false);
            _scrollViewport = viewportObj.AddComponent<RectTransform>();
            
            _scrollViewport.anchorMin = new Vector2(0f, 0f);
            _scrollViewport.anchorMax = new Vector2(1f, 1f);
            _scrollViewport.offsetMin = new Vector2(0f, bottomButtonsHeight);
            _scrollViewport.offsetMax = new Vector2(0f, -headerHeight);
            
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
                // ScrollRect will automatically adjust viewport width when scrollbar is visible
                // So we use full panel width here
                float viewportWidth = Mathf.Max(panelWidth, 0f);
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

        public void SetAutoScroll(float speed, float delay)
        {
            _autoScrollSpeed = speed;
            _autoScrollDelay = delay;

            // Stop existing auto-scroll if any
            if (_autoScrollActive)
            {
                _autoScrollActive = false;
                _autoScrollPaused = false;
                if (_verticalScrollbar != null)
                {
                    _verticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
                }
            }
            
            // Only proceed if speed > 0
            if (speed <= 0f) return;
            
            // Check if scrollbar is needed
            if (_enableScrollWhenNeeded && gameObject.activeInHierarchy)
            {
                StartCoroutine(CheckScrollAndStartAutoScroll());
            }
            else if (!_enableScrollWhenNeeded)
            {
                // Scroll is disabled, so auto-scroll cannot work
                DebugHelper.LogWarning(this, "Auto-scroll requested but scroll is disabled on this panel.");
            }
        }

        public InterfaceElement GetElementByID(string elementID) =>
            _elements
                .Concat(_bottomElements)
                .Where(x => x?.ElementID != string.Empty)
                .FirstOrDefault(x => x.ElementID == elementID);
        
        private IEnumerator CheckScrollAndStartAutoScroll()
        {
            yield return new WaitForEndOfFrame();
            CheckAndSetupScroll();
            
            yield return new WaitForEndOfFrame();
            if(_autoScrollDelay > 0f)
            {
                //yield return new WaitForSeconds(_autoScrollDelay);
            }

            // Only start auto-scroll if scrollbar is actually needed
            if (_scrollSetup && _scrollRect != null && _verticalScrollbar != null && _autoScrollSpeed > 0f)
            {
                _autoScrollActive = true;
                _autoScrollPaused = false;
                _isScrollingDown = true;
                _lastScrollValue = _scrollRect.verticalNormalizedPosition;
                
                // Subscribe to scrollbar changes to detect manual scrolling
                _verticalScrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
                
                StartCoroutine(AutoScrollCoroutine());
            }
        }

        private void OnScrollbarValueChanged(float value)
        {
            if (!_autoScrollActive || _autoScrollPaused) return;
            
            float currentValue = _scrollRect.verticalNormalizedPosition;
            float expectedValue = _lastScrollValue;
            
            // Calculate expected next value based on auto-scroll direction
            float direction = _isScrollingDown ? -1f : 1f;
            float expectedNextValue = expectedValue + (direction * _autoScrollSpeed * Time.unscaledDeltaTime);
            expectedNextValue = Mathf.Clamp01(expectedNextValue);
            
            // If the actual value differs significantly from expected, user is manually scrolling
            float delta = Mathf.Abs(currentValue - expectedNextValue);
            if (delta > 0.02f)
            {
                _autoScrollPaused = true;
                _autoScrollActive = false;
                if (_verticalScrollbar != null)
                {
                    _verticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
                }
            }
            
            _lastScrollValue = currentValue;
        }

        private IEnumerator AutoScrollCoroutine()
        {
            while (_autoScrollActive && !_autoScrollPaused && _scrollRect != null)
            {
                if (_scrollRect.verticalNormalizedPosition <= 0f)
                {
                    // Reached bottom, scroll back up
                    _isScrollingDown = false; 
                    if (_autoScrollDelay > 0f)
                    {
                        yield return new WaitForSecondsRealtime(_autoScrollDelay);
                    }
                }
                else if (_scrollRect.verticalNormalizedPosition >= 1f)
                {
                    // Reached top, scroll back down
                    _isScrollingDown = true; 
                    if (_autoScrollDelay > 0f)
                    {
                        yield return new WaitForSecondsRealtime(_autoScrollDelay);
                    }
                }
                
                float direction = _isScrollingDown ? -1f : 1f;
                float newValue = _scrollRect.verticalNormalizedPosition + (direction * _autoScrollSpeed * Time.unscaledDeltaTime);
                newValue = Mathf.Clamp01(newValue);
                
                _scrollRect.verticalNormalizedPosition = newValue;
                _lastScrollValue = newValue;
                
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_localizeTitleEvent != null)
            {
                _localizeTitleEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }
            
            if (_verticalScrollbar != null)
            {
                _verticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
            }
        }
    }
}