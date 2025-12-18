using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace Interface.Element
{
    public class GUILabel : LocalizedElementBase
    {
        [Header("Label References")]
        [SerializeField] private TextMeshProUGUI _labelTextOverride;

        protected override void Setup()
        {
            base.Setup();
            if (_labelTextOverride != null)
            {
                _labelText = _labelTextOverride;
            }
        }

        public void Initialize(string text)
        {
            ApplyText(text);
            Setup();
        }

        public void Initialize(LocalizedString localizedText)
        {
            ApplyLocalizedText(localizedText);
            Setup();
        }

        /// <summary>
        /// Fixes RectTransform settings after adding to panel
        /// Stretches label to panel width
        /// </summary>
        public void FixLabelLayout()
        {
            RectTransform labelRect = GetRectTransform();
            if (labelRect != null)
            {
                float pivotY = labelRect.pivot.y;
                float sizeY = labelRect.sizeDelta.y;
                
                labelRect.anchorMin = new Vector2(0f, pivotY);
                labelRect.anchorMax = new Vector2(1f, pivotY);
                labelRect.pivot = new Vector2(0.5f, pivotY);
                labelRect.sizeDelta = new Vector2(0f, sizeY);
            }
        }
    }
}