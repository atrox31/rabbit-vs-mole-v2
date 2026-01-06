using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Interface.Element
{
    /// <summary>
    /// Single row view for GUITable.
    /// Supports click (select) and UI navigation highlight (ISelectHandler).
    /// </summary>
    public class GUITableRow : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Row References")]
        [SerializeField] private Button _button;
        // Visible when the row is "selected" (confirmed by click/submit).
        [SerializeField] private Image _selectionHighlight;
        // Visible when the row is only "highlighted" (focused by navigation).
        [SerializeField] private Image _highlight;

        [SerializeField] private TextMeshProUGUI _gameModeText;
        [SerializeField] private TextMeshProUGUI _mutatorsText;
        [SerializeField] private TextMeshProUGUI _hostNameText;
        [SerializeField] private TextMeshProUGUI _pingText;

        private GUITable _owner;
        private int _index;
        private GUITable.RowData _data;

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            // Don't keep template wiring its own listeners (owner will set per instance).
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClickInternal);
                _button.onClick.AddListener(OnClickInternal);
            }
        }

        internal void SetData(GUITable owner, int index, GUITable.RowData data, string mutatorsYesText, string mutatorsNoText)
        {
            _owner = owner;
            _index = index;
            _data = data;

            if (_gameModeText != null) _gameModeText.text = data.GameMode ?? string.Empty;
            if (_mutatorsText != null) _mutatorsText.text = data.HasMutators ? (mutatorsYesText ?? "TAK") : (mutatorsNoText ?? "NIE");
            if (_hostNameText != null) _hostNameText.text = data.HostName ?? string.Empty;
            if (_pingText != null) _pingText.text = data.Ping < 0 ? "-" : data.Ping.ToString();
        }

        public void SetSelected(bool selected)
        {
            if (_selectionHighlight != null)
            {
                _selectionHighlight.enabled = selected;
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlight != null)
            {
                _highlight.enabled = highlighted;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner?.NotifyRowClicked(_index);
        }

        public void OnSelect(BaseEventData eventData)
        {
            // Only highlight on navigation — do not "select" the row just by moving focus.
            _owner?.NotifyRowHighlighted(_index);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _owner?.NotifyRowUnhighlighted(_index);
        }

        private void OnClickInternal()
        {
            _owner?.NotifyRowClicked(_index);
        }
    }
}


