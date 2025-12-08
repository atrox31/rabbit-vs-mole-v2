using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using Interface;

namespace Interface.Element
{
    public class GUIButton : LocalizedElementBase
    {
        [Header("Button References")]
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private TextMeshProUGUI _buttonTextOverride;

        private Action _onClickAction;
        
        [Header("Tooltip Settings")]
        private bool _isDisabled = false;
        private LocalizedString _tooltipText;
        private GUIToolTip _tooltipInstance;
        private MainMenuManager _menuManager;

        protected override void Setup()
        {
            base.Setup();

            if (_button == null)
                _button = GetComponent<UnityEngine.UI.Button>();

            if (_buttonTextOverride != null)
            {
                _labelText = _buttonTextOverride;
            }

            if (_button != null)
            {
                // Respect disabled state - don't override if already set
                _button.interactable = !_isDisabled;
                
                // Ensure button's Image can receive raycasts even when disabled
                // This allows EventTrigger to work for disabled buttons (for tooltips)
                UnityEngine.UI.Image buttonImage = _button.GetComponent<UnityEngine.UI.Image>();
                if (buttonImage != null)
                {
                    buttonImage.raycastTarget = true;
                }
                
                if (_onClickAction != null)
                {
                    _button.onClick.RemoveAllListeners();
                    _button.onClick.AddListener(() => _onClickAction?.Invoke());
                }
                
                SetupHoverEvents();
            }
        }

        private void SetupHoverEvents()
        {
            // For disabled buttons, we need to use the Image component directly for EventTrigger
            // because disabled buttons don't receive pointer events
            GameObject targetObject = _button.gameObject;
            UnityEngine.UI.Image targetImage = _button.GetComponent<UnityEngine.UI.Image>();
            
            // If button is disabled and has tooltip, ensure Image can receive raycasts
            if (_isDisabled && _tooltipText != null && !_tooltipText.IsEmpty)
            {
                if (targetImage == null)
                {
                    // Add Image component if it doesn't exist (for raycast detection)
                    targetImage = targetObject.AddComponent<UnityEngine.UI.Image>();
                    targetImage.color = new Color(1, 1, 1, 0); // Transparent
                }
                targetImage.raycastTarget = true;
            }
            
            EventTrigger trigger = targetObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = targetObject.AddComponent<EventTrigger>();
            }

            // Remove existing entries for pointer enter/exit
            trigger.triggers.RemoveAll(t => t.eventID == EventTriggerType.PointerEnter || t.eventID == EventTriggerType.PointerExit);

            // Pointer Enter
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data); });
            trigger.triggers.Add(pointerEnter);

            // Pointer Exit
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnPointerExit((PointerEventData)data); });
            trigger.triggers.Add(pointerExit);
        }

        private void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDisabled && _tooltipText != null && !_tooltipText.IsEmpty)
            {
                ShowTooltip();
            }
        }

        private void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (_tooltipInstance != null)
            {
                HideTooltip();
            }

            if (_menuManager == null)
            {
                _menuManager = FindFirstObjectByType<MainMenuManager>();
            }

            if (_menuManager == null || _tooltipText == null || _tooltipText.IsEmpty)
            {
                return;
            }

            var tooltipElement = _menuManager.CreateToolTip(_tooltipText);
            _tooltipInstance = tooltipElement as GUIToolTip;
            if (_tooltipInstance != null)
            {
                // Position tooltip to cover the button area
                RectTransform buttonRect = GetRectTransform();
                RectTransform tooltipRect = _tooltipInstance.GetRectTransform();
                
                if (buttonRect != null && tooltipRect != null)
                {
                    tooltipRect.SetParent(buttonRect.parent, false);
                    tooltipRect.anchorMin = buttonRect.anchorMin;
                    tooltipRect.anchorMax = buttonRect.anchorMax;
                    tooltipRect.anchoredPosition = buttonRect.anchoredPosition;
                    tooltipRect.sizeDelta = buttonRect.sizeDelta;
                    tooltipRect.SetAsLastSibling(); // Show on top
                }
                
                // Ensure tooltip doesn't block raycasts (should be done in Setup, but ensure it here too)
                _tooltipInstance.DisableRaycastBlocking();
                
                _tooltipInstance.Show();
            }
        }

        private void HideTooltip()
        {
            if (_tooltipInstance != null)
            {
                _tooltipInstance.Hide();
                // Destroy tooltip after hiding animation completes (animation duration is typically 0.25f)
                if (_tooltipInstance.gameObject != null)
                {
                    Destroy(_tooltipInstance.gameObject, 0.35f);
                }
                _tooltipInstance = null;
            }
        }

        public void Initialize(string text, Action onClick)
        {
            _onClickAction = onClick;
            ApplyText(text);
            Setup();
        }

        public void Initialize(LocalizedString localizedText, Action onClick)
        {
            _onClickAction = onClick;
            ApplyLocalizedText(localizedText);
            Setup();
        }

        public void SetDisabled(bool disabled, LocalizedString tooltipText = null)
        {
            _isDisabled = disabled;
            _tooltipText = tooltipText;
            
            if (_button != null)
            {
                _button.interactable = !disabled;
                
                // Ensure Image can receive raycasts for disabled buttons (for tooltips)
                if (disabled)
                {
                    UnityEngine.UI.Image buttonImage = _button.GetComponent<UnityEngine.UI.Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.raycastTarget = true;
                    }
                }
            }
        }

        public void SetDisabled(bool disabled, string tooltipText)
        {
            LocalizedString localizedTooltip = null;
            if (!string.IsNullOrEmpty(tooltipText))
            {
                localizedTooltip = new LocalizedString();
                localizedTooltip.TableReference = "Interface";
                localizedTooltip.TableEntryReference = tooltipText;
            }
            SetDisabled(disabled, localizedTooltip);
        }

        private new void OnDestroy()
        {
            HideTooltip();
            base.OnDestroy();
        }
    }
}