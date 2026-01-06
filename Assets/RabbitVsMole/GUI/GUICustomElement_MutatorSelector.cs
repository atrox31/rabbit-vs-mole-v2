using System;
using System.Collections.Generic;
using System.Linq;
using Interface.Element;
using RabbitVsMole.GameData.Mutator;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace RabbitVsMole
{
    /// <summary>
    /// Panel used to pick multiple mutators. Based on GUICustomElement_GameModeSelector.
    /// </summary>
    public class GUICustomElement_MutatorSelector : LocalizedElementBase
    {
        private const int MAX_SELECTED_MUTATORS = 8;

        public readonly struct InitArgs
        {
            public readonly List<MutatorSO> AvailableMutators;
            public readonly List<MutatorSO> PreselectedMutators;
            public readonly List<MutatorSO> LockedMutators;

            public InitArgs(List<MutatorSO> availableMutators, List<MutatorSO> preselectedMutators = null, List<MutatorSO> lockedMutators = null)
            {
                AvailableMutators = availableMutators ?? new List<MutatorSO>();
                PreselectedMutators = preselectedMutators ?? new List<MutatorSO>();
                LockedMutators = lockedMutators ?? new List<MutatorSO>();
            }

            public InitArgs(MutatorSO[] availableMutators, MutatorSO[] preselectedMutators = null, MutatorSO[] lockedMutators = null)
            {
                AvailableMutators = availableMutators != null ? new List<MutatorSO>(availableMutators) : new List<MutatorSO>();
                PreselectedMutators = preselectedMutators != null ? new List<MutatorSO>(preselectedMutators) : new List<MutatorSO>();
                LockedMutators = lockedMutators != null ? new List<MutatorSO>(lockedMutators) : new List<MutatorSO>();
            }
        }

        [Header("Mutator List References")]
        [SerializeField] private ScrollRect _mutatorScrollView;
        [SerializeField] private RectTransform _listContent;
        [SerializeField] private GameObject _mutatorButtonTemplate;
        [SerializeField] private float _buttonSpacing = 10f;

        [Header("Details")]
        [SerializeField] private Image _mutatorPreviewImage;
        [SerializeField] private TextMeshProUGUI _gameModeDescriptionText;

        [Header("Selected Mutators")]
        [SerializeField] private GameObject _selectedMutatorIconTemplate;
        [SerializeField] private RectTransform _selectedIconsContainer;

        [Header("Actions")]
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _removeButton;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _backButton;

        public event Action<List<MutatorSO>> AcceptClicked;
        public event Action BackClicked;

        private readonly List<MutatorSO> _mutators = new();
        private readonly List<MutatorSO> _selectedMutators = new();
        private readonly HashSet<MutatorSO> _lockedMutators = new();
        private readonly Dictionary<MutatorSO, Button> _listButtons = new();
        private readonly List<Button> _selectedIconButtons = new();

        private MutatorSO _focusedMutator;
        private bool _layoutReady;

        public MutatorSO GetSelectedMutator() => _focusedMutator;

        public override void InitializeWithArgument(object argument)
        {
            List<MutatorSO> incomingMutators = null;
            List<MutatorSO> preselected = null;
            List<MutatorSO> locked = null;

            if (argument is InitArgs args)
            {
                incomingMutators = args.AvailableMutators;
                preselected = args.PreselectedMutators;
                locked = args.LockedMutators;
            }
            else if (argument is List<MutatorSO> list)
            {
                incomingMutators = list;
            }
            else if (argument is MutatorSO[] array)
            {
                incomingMutators = new List<MutatorSO>(array);
            }
            else
            {
                DebugHelper.LogWarning(this, $"MutatorSelector: Invalid argument type {argument?.GetType()}");
            }

            _mutators.Clear();
            if (incomingMutators != null)
                _mutators.AddRange(incomingMutators);

            SetLockedMutators(locked);
            ApplyPreselected(preselected);
            TryRefreshUI();
        }

        protected override void Setup()
        {
            base.Setup();

            ResolveListContent();
            HideTemplates();
            SetupListLayout();
            SetupSelectedIconsLayout();
            WireActionButtons();
            FixCustomElementLayout();
            ClearDetails();

            _layoutReady = true;
            TryRefreshUI();
        }

        private void ResolveListContent()
        {
            if (_mutatorScrollView != null && _listContent == null)
            {
                Transform contentTransform = _mutatorScrollView.transform.Find("Viewport/Content");
                if (contentTransform != null)
                {
                    _listContent = contentTransform.GetComponent<RectTransform>();
                }
            }
        }

        private void HideTemplates()
        {
            if (_mutatorButtonTemplate != null)
            {
                _mutatorButtonTemplate.SetActive(false);
                _mutatorButtonTemplate.transform.position = new Vector3(10000, 10000, 0);
            }

            if (_selectedMutatorIconTemplate != null)
            {
                _selectedMutatorIconTemplate.SetActive(false);
                _selectedMutatorIconTemplate.transform.position = new Vector3(10000, 10000, 0);
            }
        }

        private void SetupListLayout()
        {
            if (_listContent == null)
                return;

            VerticalLayoutGroup layoutGroup = _listContent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.spacing = _buttonSpacing;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            ContentSizeFitter sizeFitter = _listContent.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = _listContent.gameObject.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void SetupSelectedIconsLayout()
        {
            if (_selectedIconsContainer == null && _selectedMutatorIconTemplate != null)
            {
                _selectedIconsContainer = _selectedMutatorIconTemplate.transform.parent as RectTransform;
            }

            if (_selectedIconsContainer != null)
            {
                HorizontalLayoutGroup layoutGroup = _selectedIconsContainer.GetComponent<HorizontalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = _selectedIconsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                }
                layoutGroup.spacing = _buttonSpacing;
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;

                ContentSizeFitter sizeFitter = _selectedIconsContainer.GetComponent<ContentSizeFitter>();
                if (sizeFitter == null)
                {
                    sizeFitter = _selectedIconsContainer.gameObject.AddComponent<ContentSizeFitter>();
                }
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void WireActionButtons()
        {
            if (_addButton != null) _addButton.onClick.AddListener(OnAddClicked);
            if (_removeButton != null) _removeButton.onClick.AddListener(OnRemoveClicked);
            if (_acceptButton != null) _acceptButton.onClick.AddListener(OnAcceptClickedInternal);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClickedInternal);
        }

        private void UnwireActionButtons()
        {
            if (_addButton != null) _addButton.onClick.RemoveListener(OnAddClicked);
            if (_removeButton != null) _removeButton.onClick.RemoveListener(OnRemoveClicked);
            if (_acceptButton != null) _acceptButton.onClick.RemoveListener(OnAcceptClickedInternal);
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClickedInternal);
        }

        private void ApplyPreselected(IEnumerable<MutatorSO> preselected)
        {
            _selectedMutators.Clear();
            if (preselected != null)
            {
                foreach (var mutator in preselected)
                {
                    if (mutator == null)
                        continue;
                    if (_selectedMutators.Count >= MAX_SELECTED_MUTATORS)
                        break;
                    if (_selectedMutators.Contains(mutator))
                        continue;
                    if (IsIncompatibleWithSelection(mutator))
                        continue;
                    _selectedMutators.Add(mutator);
                }
            }

            // Ensure locked mutators are always included.
            foreach (var locked in _lockedMutators)
            {
                if (locked == null) continue;
                if (_selectedMutators.Contains(locked)) continue;
                if (_selectedMutators.Count < MAX_SELECTED_MUTATORS)
                {
                    _selectedMutators.Add(locked);
                }
            }
        }

        private void TryRefreshUI()
        {
            if (!_layoutReady)
                return;
            if (_mutatorScrollView == null || _listContent == null || _mutatorButtonTemplate == null)
            {
                DebugHelper.LogWarning(this, "MutatorSelector: Missing list references.");
                return;
            }

            BuildListButtons();
            RefreshSelectedIcons();
            UpdateCompatibilityLocks();
            SelectInitialMutator();
            UpdateActionButtons();
        }

        private void BuildListButtons()
        {
            ClearListButtons();

            foreach (var mutator in _mutators)
            {
                if (mutator == null)
                    continue;

                GameObject buttonObj = Instantiate(_mutatorButtonTemplate, _listContent);
                buttonObj.SetActive(true);

                Button button = buttonObj.GetComponent<Button>() ?? buttonObj.GetComponentInChildren<Button>();
                if (button == null)
                {
                    DebugHelper.LogWarning(this, $"MutatorSelector: Button component missing for mutator {mutator.name}");
                    Destroy(buttonObj);
                    continue;
                }

                MutatorSO captured = mutator;
                button.onClick.AddListener(() => OnListButtonClicked(captured));
                _listButtons[captured] = button;

                TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    LocalizeStringEvent le = label.GetComponent<LocalizeStringEvent>();
                    if (le != null)
                    {
                        le.StringReference = null;
                        le.OnUpdateString.RemoveAllListeners();
                    }
                    label.text = captured.GetLocalizedName() ?? captured.name;
                }
            }
        }

        private void ClearListButtons()
        {
            foreach (var button in _listButtons.Values)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            _listButtons.Clear();
        }

        private void RefreshSelectedIcons()
        {
            foreach (var btn in _selectedIconButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _selectedIconButtons.Clear();

            if (_selectedIconsContainer == null || _selectedMutatorIconTemplate == null)
                return;

            foreach (var mutator in _selectedMutators)
            {
                if (mutator == null)
                    continue;

                GameObject iconObj = Instantiate(_selectedMutatorIconTemplate, _selectedIconsContainer);
                iconObj.SetActive(true);

                Button button = iconObj.GetComponent<Button>() ?? iconObj.GetComponentInChildren<Button>();
                if (button != null)
                {
                    MutatorSO captured = mutator;
                    button.onClick.AddListener(() => OnSelectedIconClicked(captured));
                    _selectedIconButtons.Add(button);
                }

                Image image = iconObj.GetComponent<Image>() ?? iconObj.GetComponentInChildren<Image>();
                if (image != null)
                {
                    image.sprite = mutator.image;
                    image.enabled = mutator.image != null;
                }

                TextMeshProUGUI label = iconObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    LocalizeStringEvent le = label.GetComponent<LocalizeStringEvent>();
                    if (le != null)
                    {
                        le.StringReference = null;
                        le.OnUpdateString.RemoveAllListeners();
                    }
                    label.text = mutator.GetLocalizedName() ?? mutator.name;
                }
            }
        }

        private void SelectInitialMutator()
        {
            if (_focusedMutator != null)
            {
                SelectMutator(_focusedMutator);
                return;
            }

            MutatorSO defaultChoice = _selectedMutators.FirstOrDefault() ?? _mutators.FirstOrDefault();
            SelectMutator(defaultChoice);
        }

        private void OnListButtonClicked(MutatorSO mutator)
        {
            SelectMutator(mutator);
        }

        private void OnSelectedIconClicked(MutatorSO mutator)
        {
            SelectMutator(mutator);
        }

        private void SelectMutator(MutatorSO mutator)
        {
            _focusedMutator = mutator;
            UpdateDetails(mutator);
            UpdateActionButtons();

            if (mutator != null && _listButtons.TryGetValue(mutator, out var button) && button != null)
            {
                button.Select();
            }
        }

        private void UpdateDetails(MutatorSO mutator)
        {
            if (_mutatorPreviewImage != null)
            {
                _mutatorPreviewImage.sprite = mutator?.image;
                _mutatorPreviewImage.enabled = mutator?.image != null;
            }

            if (_gameModeDescriptionText != null)
            {
                LocalizeStringEvent le = _gameModeDescriptionText.GetComponent<LocalizeStringEvent>();
                if (le != null)
                {
                    le.StringReference = null;
                    le.OnUpdateString.RemoveAllListeners();
                }

                if (mutator != null)
                {
                    string name = mutator.GetLocalizedName() ?? mutator.name;
                    string description = mutator.GetLocalizedDescription() ?? string.Empty;
                    _gameModeDescriptionText.text = $"<b>{name}</b>\n{description}";
                }
                else
                {
                    _gameModeDescriptionText.text = string.Empty;
                }
            }
        }

        private void UpdateCompatibilityLocks()
        {
            foreach (var kvp in _listButtons)
            {
                MutatorSO mutator = kvp.Key;
                Button button = kvp.Value;
                if (button == null) continue;

                bool incompatible = IsIncompatibleWithSelection(mutator);
                bool overLimit = _selectedMutators.Count >= MAX_SELECTED_MUTATORS && !_selectedMutators.Contains(mutator);
                button.interactable = !incompatible && !overLimit;
            }
        }

        private bool IsIncompatibleWithSelection(MutatorSO candidate)
        {
            if (candidate == null)
                return false;

            foreach (var selected in _selectedMutators)
            {
                if (selected == null)
                    continue;
                if (AreIncompatible(candidate, selected))
                    return true;
            }

            return false;
        }

        private static bool AreIncompatible(MutatorSO a, MutatorSO b)
        {
            if (a == null || b == null) return false;
            bool aHasList = a.incompatibleWith != null && a.incompatibleWith.Contains(b);
            bool bHasList = b.incompatibleWith != null && b.incompatibleWith.Contains(a);
            return aHasList || bHasList;
        }

        private void OnAddClicked()
        {
            if (_focusedMutator == null)
                return;
            if (_selectedMutators.Contains(_focusedMutator))
                return;
            if (_selectedMutators.Count >= MAX_SELECTED_MUTATORS)
                return;
            if (IsIncompatibleWithSelection(_focusedMutator))
                return;

            TryAddMutator(_focusedMutator);
        }

        private void OnRemoveClicked()
        {
            if (_focusedMutator == null)
                return;

            TryRemoveMutator(_focusedMutator);
        }

        private void OnAcceptClickedInternal()
        {
            AcceptClicked?.Invoke(GetSelectedMutators());
        }

        private void OnBackClickedInternal()
        {
            BackClicked?.Invoke();
        }

        private void UpdateActionButtons()
        {
            bool canAdd = _focusedMutator != null
                && !_selectedMutators.Contains(_focusedMutator)
                && _selectedMutators.Count < MAX_SELECTED_MUTATORS
                && !IsIncompatibleWithSelection(_focusedMutator);

            bool canRemove = _focusedMutator != null
                && _selectedMutators.Contains(_focusedMutator)
                && !_lockedMutators.Contains(_focusedMutator);

            if (_addButton != null) _addButton.interactable = canAdd;
            if (_removeButton != null) _removeButton.interactable = canRemove;
        }

        public List<MutatorSO> GetSelectedMutators()
        {
            return new List<MutatorSO>(_selectedMutators);
        }

        public void ClearSelectedMutators()
        {
            _selectedMutators.Clear();
            RefreshSelectedIcons();
            UpdateCompatibilityLocks();
            UpdateActionButtons();
            ClearDetails();
        }

        /// <summary>
        /// Adds currently focused mutator to the selection (same as clicking Add).
        /// </summary>
        public void AddSelectedMutator()
        {
            TryAddMutator(_focusedMutator);
        }

        /// <summary>
        /// Removes currently focused mutator from the selection (same as clicking Remove).
        /// </summary>
        public void DeleteSelectedMutator()
        {
            TryRemoveMutator(_focusedMutator);
        }

        private bool TryAddMutator(MutatorSO mutator)
        {
            if (mutator == null)
                return false;
            if (_selectedMutators.Contains(mutator))
                return false;
            if (_selectedMutators.Count >= MAX_SELECTED_MUTATORS)
                return false;
            if (IsIncompatibleWithSelection(mutator))
                return false;

            _selectedMutators.Add(mutator);
            RefreshSelectedIcons();
            UpdateCompatibilityLocks();
            UpdateActionButtons();
            return true;
        }

        private bool TryRemoveMutator(MutatorSO mutator)
        {
            if (mutator == null)
                return false;
            if (_lockedMutators.Contains(mutator))
                return false;

            if (_selectedMutators.Remove(mutator))
            {
                RefreshSelectedIcons();
                UpdateCompatibilityLocks();
                UpdateActionButtons();
                return true;
            }

            return false;
        }

        private void ClearDetails()
        {
            if (_mutatorPreviewImage != null)
            {
                _mutatorPreviewImage.sprite = null;
                _mutatorPreviewImage.enabled = false;
            }

            if (_gameModeDescriptionText != null)
            {
                _gameModeDescriptionText.text = string.Empty;
            }
        }

        private void FixCustomElementLayout()
        {
            RectTransform rectTransform = GetRectTransform();
            if (rectTransform != null)
            {
                LayoutElement layoutElement = GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = gameObject.AddComponent<LayoutElement>();
                }

                // Allow parent layouts to size this element; prevent it from overflowing.
                layoutElement.flexibleHeight = 1f;
                layoutElement.minHeight = 0f;
            }
        }

        private void SetLockedMutators(IEnumerable<MutatorSO> locked)
        {
            _lockedMutators.Clear();
            if (locked == null)
                return;

            foreach (var mutator in locked)
            {
                if (mutator != null)
                {
                    _lockedMutators.Add(mutator);
                }
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            UnwireActionButtons();

            foreach (var button in _listButtons.Values)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }

            foreach (var btn in _selectedIconButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveAllListeners();
            }
        }
    }
}

