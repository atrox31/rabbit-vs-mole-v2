using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Interface.Element
{
    public class GUIButton : InterfaceElement
    {
        [Header("Button References")]
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private LocalizeStringEvent _localizeStringEvent;

        private Action _onClickAction;
        private LocalizedString _localizedText;
        
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

            if (_button != null)
            {
                if (_onClickAction != null)
                {
                    _button.onClick.RemoveAllListeners();
                    _button.onClick.AddListener(() => _onClickAction?.Invoke());
                }
                
                // Setup hover events for tooltip
                SetupHoverEvents();
            }

            // Setup LocalizeStringEvent if available
            if (_localizeStringEvent == null && _buttonText != null)
            {
                _localizeStringEvent = _buttonText.GetComponent<LocalizeStringEvent>();
                if (_localizeStringEvent == null)
                {
                    _localizeStringEvent = _buttonText.gameObject.AddComponent<LocalizeStringEvent>();
                }
                
                // Ensure the target is set - LocalizeStringEvent should auto-find TextMeshProUGUI
                // but we can also subscribe to the update event to ensure it works
                if (_localizeStringEvent != null)
                {
                    _localizeStringEvent.OnUpdateString.AddListener(OnLocalizedStringUpdate);
                }
            }
        }

        private void SetupHoverEvents()
        {
            EventTrigger trigger = _button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = _button.gameObject.AddComponent<EventTrigger>();
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

        private void OnLocalizedStringUpdate(string localizedString)
        {
            if (_buttonText != null)
            {
                _buttonText.text = localizedString;
            }
        }

        public void Initialize(string text, Action onClick)
        {
            _onClickAction = onClick;
            
            if (_buttonText != null)
            {
                // Try to use localization if available, otherwise use plain text
                if (_localizeStringEvent != null && _localizedText != null)
                {
                    _localizeStringEvent.StringReference = _localizedText;
                }
                else
                {
                    _buttonText.text = text;
                }
            }

            Setup();
        }

        public void Initialize(LocalizedString localizedText, Action onClick)
        {
            _localizedText = localizedText;
            _onClickAction = onClick;
            
            // Setup first to ensure LocalizeStringEvent is available
            Setup();
            
            if (_buttonText != null)
            {
                if (_localizeStringEvent != null)
                {
                    _localizeStringEvent.StringReference = localizedText;
                    _localizeStringEvent.RefreshString();
                }
                else
                {
                    // Fallback to async loading if LocalizeStringEvent not available
                    localizedText.GetLocalizedStringAsync().Completed += (op) =>
                    {
                        if (op.IsDone && op.Result != null && _buttonText != null)
                        {
                            _buttonText.text = op.Result;
                        }
                    };
                }
            }
        }


        public void SetText(string text)
        {
            if (_buttonText != null)
            {
                if (_localizeStringEvent != null)
                {
                    _localizeStringEvent.StringReference = null;
                }
                _buttonText.text = text;
            }
        }

        public void SetLocalizedText(LocalizedString localizedText)
        {
            _localizedText = localizedText;
            if (_buttonText != null && _localizeStringEvent != null)
            {
                _localizeStringEvent.StringReference = localizedText;
                _localizeStringEvent.RefreshString();
            }
        }

        public void SetDisabled(bool disabled, LocalizedString tooltipText = null)
        {
            _isDisabled = disabled;
            _tooltipText = tooltipText;
            
            if (_button != null)
            {
                _button.interactable = !disabled;
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

        private void OnDestroy()
        {
            HideTooltip();
            
            if (_localizeStringEvent != null)
            {
                _localizeStringEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }
        }
    }
}