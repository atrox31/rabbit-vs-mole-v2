using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Interface.Element
{
    /// <summary>
    /// Simple table element (no filtering/sorting). Displays rows and allows selecting a row.
    /// Intended to be used as a custom element via PanelBuilder.AddCustomElement(prefab, args).
    /// </summary>
    public class GUITable : InterfaceElement
    {
        [Serializable]
        public struct RowData
        {
            public string GameMode;
            public bool HasMutators;
            public string HostName;
            public int Ping;

            /// <summary>
            /// Connection address selected by user (for next steps).
            /// Can be IP, hostname, or any connection key (e.g. Steam lobby id string).
            /// </summary>
            public string Ip;
        }

        public readonly struct InitArgs
        {
            public readonly IReadOnlyList<RowData> Rows;
            public readonly LocalizedString HeaderGameMode;
            public readonly LocalizedString HeaderMutators;
            public readonly LocalizedString HeaderHostName;
            public readonly LocalizedString HeaderPing;

            public readonly LocalizedString YesText;
            public readonly LocalizedString NoText;

            public InitArgs(
                IReadOnlyList<RowData> rows,
                LocalizedString headerGameMode = null,
                LocalizedString headerMutators = null,
                LocalizedString headerHostName = null,
                LocalizedString headerPing = null,
                LocalizedString yesText = null,
                LocalizedString noText = null)
            {
                Rows = rows;
                HeaderGameMode = headerGameMode;
                HeaderMutators = headerMutators;
                HeaderHostName = headerHostName;
                HeaderPing = headerPing;
                YesText = yesText;
                NoText = noText;
            }
        }

        [Header("Table References")]
        [SerializeField] private TextMeshProUGUI _headerGameMode;
        [SerializeField] private TextMeshProUGUI _headerMutators;
        [SerializeField] private TextMeshProUGUI _headerHostName;
        [SerializeField] private TextMeshProUGUI _headerPing;

        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _scrollContent;

        [SerializeField] private Button _refreshButton;

        [Header("Row Template")]
        [SerializeField] private GUITableRow _rowTemplate;
        [SerializeField] private bool _hideTemplateRow = true;

        [Header("Layout Settings")]
        [SerializeField] private bool _allowAnchorModificationOverride = false;
        [SerializeField] private float _fallbackHeight = 420f;

        [Header("Text Settings")]
        [SerializeField] private LocalizedString _localizedHeaderGameMode = new LocalizedString("Interface", "interface_table_gamemode");
        [SerializeField] private LocalizedString _localizedHeaderMutators = new LocalizedString("Interface", "interface_table_mutators");
        [SerializeField] private LocalizedString _localizedHeaderHostName = new LocalizedString("Interface", "interface_table_hostname");
        [SerializeField] private LocalizedString _localizedHeaderPing = new LocalizedString("Interface", "interface_table_ping");

        [SerializeField] private LocalizedString _localizedYesText = new LocalizedString("Interface", "interface_table_yes");
        [SerializeField] private LocalizedString _localizedNoText = new LocalizedString("Interface", "interface_table_no");

        [SerializeField] private string _mutatorsYesTextFallback = "TAK";
        [SerializeField] private string _mutatorsNoTextFallback = "NIE";

        private string _mutatorsYesTextResolved = "TAK";
        private string _mutatorsNoTextResolved = "NIE";

        private LocalizeStringEvent _locHeaderGameMode;
        private LocalizeStringEvent _locHeaderMutators;
        private LocalizeStringEvent _locHeaderHostName;
        private LocalizeStringEvent _locHeaderPing;

        private readonly List<GUITableRow> _rows = new();
        private List<RowData> _data = new();
        private int _selectedIndex = -1;
        private int _highlightedIndex = -1;

        public override bool AllowAnchorModification => _allowAnchorModificationOverride;

        public int SelectedIndex => _selectedIndex;
        public int HighlightedIndex => _highlightedIndex;
        public bool HasSelection => _selectedIndex >= 0 && _selectedIndex < _data.Count;
        public string SelectedIp => HasSelection ? (_data[_selectedIndex].Ip ?? string.Empty) : string.Empty;

        public event Action<RowData> OnRowSelected;
        public event Action OnRefreshRequested;

        // When SetRows is called while the element is inactive, queue a rebuild until it is enabled.
        private bool _rebuildQueuedOnEnable = false;

        protected override void Setup()
        {
            base.Setup();

            if (_scrollRect == null)
            {
                _scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (_scrollContent == null && _scrollRect != null)
            {
                _scrollContent = _scrollRect.content;
            }

            if (_rowTemplate == null)
            {
                _rowTemplate = GetComponentInChildren<GUITableRow>(true);
            }

            if (_hideTemplateRow)
            {
                HideTemplate();
            }

            EnsureScrollContentHasLayout();
            SetupHeaderLocalization();
            RefreshYesNoLocalizedStrings();
            SetupRefreshButton();
            FixCustomElementLayout();
        }

        private void SetupRefreshButton()
        {
            if (_refreshButton == null)
                return;

            _refreshButton.onClick.RemoveListener(HandleRefreshClicked);
            _refreshButton.onClick.AddListener(HandleRefreshClicked);
        }

        private void HandleRefreshClicked()
        {
            OnRefreshRequested?.Invoke();
        }

        private void OnEnable()
        {
            // When panel becomes visible, trigger any pending rebuild (or at least one rebuild).
            if (!isActiveAndEnabled) return;

            if (_rebuildQueuedOnEnable)
            {
                _rebuildQueuedOnEnable = false;
                StartCoroutine(RebuildLayoutNextFrame());
            }
            else
            {
                StartCoroutine(RebuildLayoutNextFrame());
            }
        }

        public override void InitializeWithArgument(object argument)
        {
            if (argument is InitArgs args)
            {
                // allow overrides, but default to serialized values if null
                SetHeaderLocalized(
                    args.HeaderGameMode ?? _localizedHeaderGameMode,
                    args.HeaderMutators ?? _localizedHeaderMutators,
                    args.HeaderHostName ?? _localizedHeaderHostName,
                    args.HeaderPing ?? _localizedHeaderPing);

                SetYesNoLocalized(
                    args.YesText ?? _localizedYesText,
                    args.NoText ?? _localizedNoText);

                SetRows(args.Rows);
                return;
            }

            if (argument is IReadOnlyList<RowData> rows)
            {
                SetRows(rows);
                return;
            }

            if (argument is List<RowData> rowsList)
            {
                SetRows(rowsList);
                return;
            }

            if (argument != null)
            {
                DebugHelper.LogWarning(this, $"GUITable: Invalid argument type: {argument.GetType()}");
            }
        }

        /// <summary>Sets plain header texts (disables localization for those headers).</summary>
        public void SetHeaders(string gameMode, string mutators, string hostName, string ping)
        {
            ClearHeaderLocalization();
            if (_headerGameMode != null) _headerGameMode.text = gameMode ?? string.Empty;
            if (_headerMutators != null) _headerMutators.text = mutators ?? string.Empty;
            if (_headerHostName != null) _headerHostName.text = hostName ?? string.Empty;
            if (_headerPing != null) _headerPing.text = ping ?? string.Empty;
        }

        public void SetHeaderLocalized(LocalizedString gameMode, LocalizedString mutators, LocalizedString hostName, LocalizedString ping)
        {
            _localizedHeaderGameMode = gameMode;
            _localizedHeaderMutators = mutators;
            _localizedHeaderHostName = hostName;
            _localizedHeaderPing = ping;
            SetupHeaderLocalization();
        }

        public void SetYesNoLocalized(LocalizedString yesText, LocalizedString noText)
        {
            _localizedYesText = yesText;
            _localizedNoText = noText;
            RefreshYesNoLocalizedStrings();
        }

        /// <summary>
        /// Fallback only (if localization is not yet resolved or keys are missing).
        /// </summary>
        public void SetYesNoFallback(string yesText, string noText)
        {
            _mutatorsYesTextFallback = string.IsNullOrWhiteSpace(yesText) ? "TAK" : yesText;
            _mutatorsNoTextFallback = string.IsNullOrWhiteSpace(noText) ? "NIE" : noText;
            // If we don't have resolved values yet, use fallback immediately.
            if (string.IsNullOrWhiteSpace(_mutatorsYesTextResolved)) _mutatorsYesTextResolved = _mutatorsYesTextFallback;
            if (string.IsNullOrWhiteSpace(_mutatorsNoTextResolved)) _mutatorsNoTextResolved = _mutatorsNoTextFallback;
            RefreshMutatorsColumnOnRows();
        }

        public void SetRows(IReadOnlyList<RowData> rows, bool keepSelectionIfPossible = false)
        {
            _data = rows != null ? new List<RowData>(rows) : new List<RowData>();

            int oldSelected = _selectedIndex;
            string oldSelectedIp = HasSelection ? SelectedIp : string.Empty;

            ClearRows();
            CreateRows();

            if (keepSelectionIfPossible)
            {
                // Prefer exact index if it still exists
                if (oldSelected >= 0 && oldSelected < _data.Count)
                {
                    SelectRow(oldSelected, notify: false);
                }
                else if (!string.IsNullOrEmpty(oldSelectedIp))
                {
                    int found = _data.FindIndex(r => string.Equals(r.Ip, oldSelectedIp, StringComparison.Ordinal));
                    if (found >= 0) SelectRow(found, notify: false);
                }
            }

            if (!HasSelection)
            {
                UpdateSelectionVisuals();
            }
        }

        public void ClearSelection(bool notify = false)
        {
            _selectedIndex = -1;
            UpdateSelectionVisuals();

            if (notify)
            {
                OnRowSelected?.Invoke(default);
            }
        }

        public bool TryGetSelectedRow(out RowData selected)
        {
            if (HasSelection)
            {
                selected = _data[_selectedIndex];
                return true;
            }
            selected = default;
            return false;
        }

        public void SelectRow(int index, bool notify = true)
        {
            if (index < 0 || index >= _data.Count)
            {
                _selectedIndex = -1;
                UpdateSelectionVisuals();
                return;
            }

            if (_selectedIndex == index)
            {
                // still refresh visuals (e.g. template rebuild)
                UpdateSelectionVisuals();
                if (notify)
                {
                    OnRowSelected?.Invoke(_data[_selectedIndex]);
                }
                return;
            }

            _selectedIndex = index;
            UpdateSelectionVisuals();

            if (notify)
            {
                OnRowSelected?.Invoke(_data[_selectedIndex]);
            }
        }

        internal void NotifyRowClicked(int index)
        {
            SelectRow(index, notify: true);
        }

        internal void NotifyRowHighlighted(int index)
        {
            if (index < 0 || index >= _data.Count)
            {
                _highlightedIndex = -1;
                UpdateSelectionVisuals();
                return;
            }
            if (_highlightedIndex == index) return;
            _highlightedIndex = index;
            UpdateSelectionVisuals();
        }

        internal void NotifyRowUnhighlighted(int index)
        {
            if (_highlightedIndex == index)
            {
                _highlightedIndex = -1;
                UpdateSelectionVisuals();
            }
        }

        public override void FixCustomElementLayout()
        {
            RectTransform rt = GetRectTransform();
            if (rt == null) return;

            // If prefab forgot to set height, enforce a sane fallback.
            if (rt.sizeDelta.y <= 0.01f && rt.rect.height <= 0.01f)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Max(10f, _fallbackHeight));
            }

            // If prefab forgot to set width, try to match parent container width.
            if (rt.sizeDelta.x <= 0.01f)
            {
                if (transform.parent is RectTransform parentRt)
                {
                    float w = parentRt.rect.width > 0.01f ? parentRt.rect.width : parentRt.sizeDelta.x;
                    if (w > 0.01f)
                    {
                        rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
                    }
                }
            }
        }

        private void HideTemplate()
        {
            if (_rowTemplate == null) return;

            _rowTemplate.gameObject.SetActive(false);
            // Keep template outside layout if it lives under content
            if (_scrollContent != null && _rowTemplate.transform.parent == _scrollContent)
            {
                _rowTemplate.transform.SetParent(transform, false);
            }
        }

        private void EnsureScrollContentHasLayout()
        {
            if (_scrollContent == null) return;

            var layoutGroup = _scrollContent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = _scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.spacing = 6f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var fitter = _scrollContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = _scrollContent.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                if (row == null) continue;

                if (Application.isPlaying)
                {
                    Destroy(row.gameObject);
                }
                else
                {
                    DestroyImmediate(row.gameObject);
                }
            }
            _rows.Clear();
        }

        private void CreateRows()
        {
            if (_rowTemplate == null)
            {
                Debug.LogError("GUITable: Row template is not assigned!");
                return;
            }
            if (_scrollContent == null)
            {
                Debug.LogError("GUITable: Scroll content is not assigned!");
                return;
            }

            for (int i = 0; i < _data.Count; i++)
            {
                var instance = Instantiate(_rowTemplate, _scrollContent);
                instance.gameObject.SetActive(true);
                instance.SetData(this, i, _data[i], _mutatorsYesTextResolved, _mutatorsNoTextResolved);

                // Ensure the layout system sees a stable row height.
                // Without this, VerticalLayoutGroup may treat rows as height=0 and only apply spacing (overlap).
                var rowRT = instance.transform as RectTransform;
                float rowHeight = 50f;
                if (rowRT != null)
                {
                    float h = rowRT.rect.height > 0.01f ? rowRT.rect.height : rowRT.sizeDelta.y;
                    if (h > 0.01f) rowHeight = h;
                }

                var layoutElement = instance.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = instance.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.ignoreLayout = false;
                layoutElement.minHeight = rowHeight;
                layoutElement.preferredHeight = rowHeight;
                layoutElement.flexibleHeight = 0f;

                _rows.Add(instance);
            }

            // Force layout rebuild so rows don't overlap (especially when panel is inactive during creation).
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }

            // One more rebuild next frame to handle cases where panel was inactive during creation.
            if (isActiveAndEnabled)
            {
                StartCoroutine(RebuildLayoutNextFrame());
            }
            else
            {
                // Queue rebuild for when the element becomes active to avoid StartCoroutine errors on inactive objects.
                _rebuildQueuedOnEnable = true;
            }
        }

        private IEnumerator RebuildLayoutNextFrame()
        {
            if (_scrollContent == null) yield break;
            yield return null; // next frame
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
            Canvas.ForceUpdateCanvases();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null) continue;
                _rows[i].SetSelected(i == _selectedIndex);
                _rows[i].SetHighlighted(i == _highlightedIndex);
            }
        }

        private void SetupHeaderLocalization()
        {
            EnsureHeaderLocalization(_headerGameMode, ref _locHeaderGameMode, _localizedHeaderGameMode);
            EnsureHeaderLocalization(_headerMutators, ref _locHeaderMutators, _localizedHeaderMutators);
            EnsureHeaderLocalization(_headerHostName, ref _locHeaderHostName, _localizedHeaderHostName);
            EnsureHeaderLocalization(_headerPing, ref _locHeaderPing, _localizedHeaderPing);
        }

        private void EnsureHeaderLocalization(TextMeshProUGUI text, ref LocalizeStringEvent localizeEvent, LocalizedString reference)
        {
            if (text == null) return;
            if (localizeEvent == null)
            {
                localizeEvent = text.GetComponent<LocalizeStringEvent>();
                if (localizeEvent == null)
                {
                    localizeEvent = text.gameObject.AddComponent<LocalizeStringEvent>();
                }
            }

            if (localizeEvent == null) return;

            localizeEvent.OnUpdateString.RemoveAllListeners();
            localizeEvent.OnUpdateString.AddListener(s =>
            {
                if (text != null) text.text = s ?? string.Empty;
            });

            localizeEvent.StringReference = reference;
            localizeEvent.RefreshString();
        }

        private void ClearHeaderLocalization()
        {
            ClearHeaderLocalization(_headerGameMode, ref _locHeaderGameMode);
            ClearHeaderLocalization(_headerMutators, ref _locHeaderMutators);
            ClearHeaderLocalization(_headerHostName, ref _locHeaderHostName);
            ClearHeaderLocalization(_headerPing, ref _locHeaderPing);
        }

        private void ClearHeaderLocalization(TextMeshProUGUI text, ref LocalizeStringEvent localizeEvent)
        {
            if (localizeEvent == null) return;
            localizeEvent.OnUpdateString.RemoveAllListeners();
            localizeEvent.StringReference = null;
            // keep component, but it will do nothing
            localizeEvent = null;
        }

        private void RefreshYesNoLocalizedStrings()
        {
            // Always start from fallbacks, then override when async resolves.
            _mutatorsYesTextResolved = string.IsNullOrWhiteSpace(_mutatorsYesTextFallback) ? "TAK" : _mutatorsYesTextFallback;
            _mutatorsNoTextResolved = string.IsNullOrWhiteSpace(_mutatorsNoTextFallback) ? "NIE" : _mutatorsNoTextFallback;

            // Resolve YES
            if (_localizedYesText != null && !_localizedYesText.IsEmpty)
            {
                var op = _localizedYesText.GetLocalizedStringAsync();
                op.Completed += handle =>
                {
                    if (handle.IsDone && !string.IsNullOrEmpty(handle.Result))
                    {
                        _mutatorsYesTextResolved = handle.Result;
                        RefreshMutatorsColumnOnRows();
                    }
                };
            }

            // Resolve NO
            if (_localizedNoText != null && !_localizedNoText.IsEmpty)
            {
                var op = _localizedNoText.GetLocalizedStringAsync();
                op.Completed += handle =>
                {
                    if (handle.IsDone && !string.IsNullOrEmpty(handle.Result))
                    {
                        _mutatorsNoTextResolved = handle.Result;
                        RefreshMutatorsColumnOnRows();
                    }
                };
            }

            RefreshMutatorsColumnOnRows();
        }

        private void RefreshMutatorsColumnOnRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null) continue;
                if (i < 0 || i >= _data.Count) continue;
                _rows[i].SetData(this, i, _data[i], _mutatorsYesTextResolved, _mutatorsNoTextResolved);
            }
        }
    }
}


