using UnityEngine;
using UnityEngine.UI;

namespace Interface.Element
{
    /// <summary>
    /// GUI element that serves as a vertical spacer - invisible but takes up space
    /// Used to create spacing between other GUI elements in panels
    /// </summary>
    public class GUISpacer : InterfaceElement
    {
        [Header("Spacer Settings")]
        [SerializeField] private float _spacingHeight = 20f;

        private LayoutElement _layoutElement;

        protected override void Setup()
        {
            base.Setup();
            
            // Ensure we have a RectTransform
            RectTransform rectTransform = GetRectTransform();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // Add or get LayoutElement component
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            // Configure LayoutElement to control height
            _layoutElement.preferredHeight = _spacingHeight;
            _layoutElement.flexibleHeight = 0f;
            _layoutElement.minHeight = _spacingHeight;

            // Make element invisible but still take up space
            SetAlpha(0f);
        }

        /// <summary>
        /// Initialize spacer with custom height
        /// </summary>
        /// <param name="height">Height of the spacer in pixels</param>
        public void Initialize(float height)
        {
            _spacingHeight = height;
            Setup();
        }

        /// <summary>
        /// Set the spacing height dynamically
        /// </summary>
        /// <param name="height">New height value</param>
        public void SetHeight(float height)
        {
            _spacingHeight = Mathf.Max(0f, height);
            if (_layoutElement != null)
            {
                _layoutElement.preferredHeight = _spacingHeight;
                _layoutElement.minHeight = _spacingHeight;
            }
            
            // Update RectTransform sizeDelta if needed
            RectTransform rectTransform = GetRectTransform();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _spacingHeight);
            }
        }

        /// <summary>
        /// Get the current spacing height
        /// </summary>
        public float GetHeight()
        {
            return _spacingHeight;
        }

        public override void SetAlpha(float alpha)
        {
            // Always keep spacer invisible (alpha = 0) regardless of calls
            // This ensures it's never visible but still takes up space
            base.SetAlpha(0f);
        }

        protected override void CollectUIComponents()
        {
            // Override to prevent collecting any UI components
            // Spacer should be completely invisible
            _elementImages.Clear();
            _elementTextMeshes.Clear();
        }
    }
}

