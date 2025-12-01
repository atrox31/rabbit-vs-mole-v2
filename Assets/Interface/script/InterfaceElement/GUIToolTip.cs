using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Interface.Element
{
    public class GUIToolTip : InterfaceElement
    {
        [Header("ToolTip References")]
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private LocalizeStringEvent _localizeLabelEvent;
        
        private LocalizedString _localizedLabel;
        
        protected override void Setup()
        {
            base.Setup();

            // Setup LocalizeStringEvent if available
            if (_localizeLabelEvent == null && _labelText != null)
            {
                _localizeLabelEvent = _labelText.GetComponent<LocalizeStringEvent>();
                if (_localizeLabelEvent == null)
                {
                    _localizeLabelEvent = _labelText.gameObject.AddComponent<LocalizeStringEvent>();
                }

                // Subscribe to update event to ensure it works
                if (_localizeLabelEvent != null)
                {
                    _localizeLabelEvent.OnUpdateString.AddListener(OnLocalizedStringUpdate);
                }
            }
            
            // Disable raycast blocking so tooltip doesn't interfere with mouse events
            DisableRaycastBlocking();
        }
        
        /// <summary>
        /// Disables raycast blocking on all UI elements in the tooltip so mouse events pass through
        /// </summary>
        public void DisableRaycastBlocking()
        {
            // Disable raycastTarget on all Images
            Image[] allImages = GetComponentsInChildren<Image>(true);
            foreach (var image in allImages)
            {
                if (image != null)
                {
                    image.raycastTarget = false;
                }
            }
            
            // Disable raycastTarget on all TextMeshProUGUI
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text != null)
                {
                    text.raycastTarget = false;
                }
            }
            
            // Add CanvasGroup to block raycasts at the root level
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void OnLocalizedStringUpdate(string localizedString)
        {
            if (_labelText != null)
            {
                _labelText.text = localizedString;
            }
        }

        public void Initialize(string label)
        {
            Setup();
            if (_localizeLabelEvent != null)
            {
                _localizeLabelEvent.StringReference.TableEntryReference = label;
                _localizeLabelEvent.RefreshString();
            }
            else if (_labelText != null)
            {
                _labelText.text = label;
            }
        }

        private void OnDestroy()
        {
            if (_localizeLabelEvent != null)
            {
                _localizeLabelEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }
        }

        public void Initialize(LocalizedString localizedString)
        {
            _localizedLabel = localizedString;
            Setup();
            if (_localizeLabelEvent != null)
            {
                _localizeLabelEvent.StringReference = localizedString;
                _localizeLabelEvent.RefreshString();
            }
            else if (_labelText != null)
            {
                _labelText.text = localizedString.GetLocalizedString();
            }
        }
    }
}