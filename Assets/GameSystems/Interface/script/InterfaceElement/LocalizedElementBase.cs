using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Interface.Element
{
    /// <summary>
    /// Base class for UI elements that support localization
    /// Provides common functionality for setting up and managing LocalizeStringEvent components
    /// </summary>
    public abstract class LocalizedElementBase : InterfaceElement
    {
        protected TextMeshProUGUI _labelText;
        protected LocalizeStringEvent _localizeStringEvent;
        protected LocalizedString _localizedText;

        protected override void Setup()
        {
            base.Setup();
            SetupLocalization();
        }

        /// <summary>
        /// Sets up LocalizeStringEvent component for the label text
        /// </summary>
        protected virtual void SetupLocalization()
        {
            if (_labelText == null)
                _labelText = GetComponentInChildren<TextMeshProUGUI>();

            if (_localizeStringEvent == null && _labelText != null)
            {
                _localizeStringEvent = _labelText.GetComponent<LocalizeStringEvent>();
                if (_localizeStringEvent == null)
                {
                    _localizeStringEvent = _labelText.gameObject.AddComponent<LocalizeStringEvent>();
                }

                if (_localizeStringEvent != null)
                {
                    _localizeStringEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
                    _localizeStringEvent.OnUpdateString.AddListener(OnLocalizedStringUpdate);
                }
            }
        }

        /// <summary>
        /// Called when localized string is updated
        /// </summary>
        protected virtual void OnLocalizedStringUpdate(string localizedString)
        {
            if (_labelText != null)
            {
                _labelText.text = localizedString;
            }
        }

        /// <summary>
        /// Sets plain text, disabling localization
        /// </summary>
        public virtual void SetText(string text)
        {
            if (_labelText != null)
            {
                if (_localizeStringEvent != null)
                {
                    _localizeStringEvent.StringReference = null;
                }
                _labelText.text = text;
            }
        }

        /// <summary>
        /// Sets localized text
        /// </summary>
        public virtual void SetLocalizedText(LocalizedString localizedText)
        {
            _localizedText = localizedText;
            if (_labelText != null && _localizeStringEvent != null)
            {
                _localizeStringEvent.StringReference = localizedText;
                _localizeStringEvent.RefreshString();
            }
        }

        /// <summary>
        /// Applies plain text to label, disabling localization
        /// </summary>
        protected void ApplyText(string text)
        {
            if (_labelText != null)
            {
                if (_localizeStringEvent != null)
                {
                    _localizeStringEvent.StringReference = null;
                }
                _labelText.text = text;
            }
        }

        /// <summary>
        /// Applies localized text to label
        /// </summary>
        protected void ApplyLocalizedText(LocalizedString localizedText)
        {
            _localizedText = localizedText;
            if (_labelText != null)
            {
                if (_localizeStringEvent != null)
                {
                    _localizeStringEvent.StringReference = localizedText;
                    _localizeStringEvent.RefreshString();
                }
                else
                {
                    localizedText.GetLocalizedStringAsync().Completed += (op) =>
                    {
                        if (op.IsDone && op.Result != null && _labelText != null)
                        {
                            _labelText.text = op.Result;
                        }
                    };
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (_localizeStringEvent != null)
            {
                _localizeStringEvent.OnUpdateString.RemoveListener(OnLocalizedStringUpdate);
            }
        }
    }
}

