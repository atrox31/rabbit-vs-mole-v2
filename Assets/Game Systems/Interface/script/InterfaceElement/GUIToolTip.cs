using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

namespace Interface.Element
{
    public class GUIToolTip : LocalizedElementBase
    {
        [Header("ToolTip References")]
        [SerializeField] private TMPro.TextMeshProUGUI _labelTextOverride;
        
        protected override void Setup()
        {
            base.Setup();
            if (_labelTextOverride != null)
            {
                _labelText = _labelTextOverride;
            }
            DisableRaycastBlocking();
        }
        
        /// <summary>
        /// Disables raycast blocking on all UI elements so mouse events pass through
        /// </summary>
        public void DisableRaycastBlocking()
        {
            Image[] allImages = GetComponentsInChildren<Image>(true);
            foreach (var image in allImages)
            {
                if (image != null)
                {
                    image.raycastTarget = false;
                }
            }
            
            TMPro.TextMeshProUGUI[] allTexts = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text != null)
                {
                    text.raycastTarget = false;
                }
            }
            
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public void Initialize(string label)
        {
            Setup();
            if (_localizeStringEvent != null)
            {
                _localizeStringEvent.StringReference.TableEntryReference = label;
                _localizeStringEvent.RefreshString();
            }
            else if (_labelText != null)
            {
                _labelText.text = label;
            }
        }

        public void Initialize(LocalizedString localizedString)
        {
            ApplyLocalizedText(localizedString);
            Setup();
        }
    }
}