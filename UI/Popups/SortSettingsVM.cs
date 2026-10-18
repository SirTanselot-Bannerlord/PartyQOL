using System;
using System.Collections.Generic;
using System.Linq;
using PartyQOL.Sorting;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.UI.Popups
{
    /// <summary>
    /// Profiles | Criteria (active / available) | Details. Edits apply on Save only.
    /// </summary>
    public sealed class SortSettingsVM : PopupVM
    {
        private readonly PartyVM _partyVm;
        private readonly List<SortKey> _openSnapshotKeys;
        private readonly List<string> _openSnapshotTypeOrder;
        private readonly List<string> _openSnapshotCultureOrder;
        private string _openSnapshotActiveName;
        private MBBindingList<SortProfileItemVM> _profiles;
        private MBBindingList<SortChipVM> _activeChips;
        private MBBindingList<SortChipVM> _availableChips;
        private MBBindingList<OrderChipVM> _detailOrderChips;
        private SortChipVM _selectedChip;
        private List<string> _typeOrder = SortOrderDefaults.CopyTypeOrder();
        private List<string> _cultureOrder = new List<string>();
        private string _editingSavedName = string.Empty;
        private bool _canDelete;
        private bool _canRename;
        private bool _canRestore;
        private float _deleteButtonAlpha = 1f;
        private float _renameButtonAlpha = 1f;
        private float _restoreButtonAlpha = 1f;
        private bool _hasSelectedCriterion;
        private bool _showOrderDetails;
        private bool _showDirectionDetails;
        private bool _selectedIsAscending = true;
        private string _titleText;
        private string _helpText;
        private string _profilesTitle;
        private string _criteriaTitle;
        private string _detailsTitle;
        private string _activeSectionTitle;
        private string _availableSectionTitle;
        private string _orderTitle;
        private string _cancelText;
        private string _saveText;
        private string _saveAndExitText;
        private string _deleteText;
        private string _duplicateText;
        private string _saveAsText;
        private string _ascendingText;
        private string _descendingText;
        private string _newText;
        private string _renameText;
        private string _restoreText;

        private SortSettingsVM(PartyVM partyVm)
        {
            _partyVm = partyVm;
            SortPresets.EnsureLoadedPublic();
            _openSnapshotKeys = TroopSortState.Keys.ToList();
            _openSnapshotTypeOrder = TroopSortState.TypeOrder.ToList();
            _openSnapshotCultureOrder = TroopSortState.CultureOrder.ToList();
            _openSnapshotActiveName = SortPresets.ActiveName;

            _titleText = new TextObject("{=pqol_sort_title}Troop Sort").ToString();
            _helpText = new TextObject("{=pqol_sort_help6}Drag criteria between Sorting and Available, and reorder Type or Culture lists in Details.").ToString();
            _profilesTitle = new TextObject("{=pqol_sort_profiles}Orders").ToString();
            _newText = new TextObject("{=pqol_new}New").ToString();
            _renameText = new TextObject("{=pqol_rename}Rename").ToString();
            _criteriaTitle = new TextObject("{=pqol_sort_criteria}Criteria").ToString();
            _detailsTitle = new TextObject("{=pqol_sort_details}Details").ToString();
            _activeSectionTitle = new TextObject("{=pqol_sort_active}Sorting").ToString();
            _availableSectionTitle = new TextObject("{=pqol_sort_available}Available").ToString();
            _orderTitle = new TextObject("{=pqol_sort_order_desc}Order (Desc = top first)").ToString();
            _cancelText = new TextObject("{=pqol_sort_cancel}Cancel").ToString();
            _saveText = new TextObject("{=pqol_sort_save}Save").ToString();
            _saveAsText = new TextObject("{=pqol_sort_saveas}Save As").ToString();
            _saveAndExitText = new TextObject("{=pqol_sort_save_exit}Save and Exit").ToString();
            _deleteText = new TextObject("{=pqol_sort_delete}Delete").ToString();
            _duplicateText = new TextObject("{=pqol_sort_duplicate}Duplicate").ToString();
            _restoreText = new TextObject("{=pqol_restore}Restore").ToString();
            _ascendingText = new TextObject("{=pqol_sort_asc}Ascending").ToString();
            _descendingText = new TextObject("{=pqol_sort_desc}Descending").ToString();

            if (SortPresets.HasActiveSaved && SortPresets.TryGet(SortPresets.ActiveName, out SortPreset active))
            {
                _editingSavedName = active.Name;
                _typeOrder = SortOrderDefaults.NormalizeTypeOrder(active.TypeOrder);
                _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(active.CultureOrder);
                RebuildChips(active.Keys);
                RebuildProfiles();
            }
            else
            {
                BeginDraft(new List<SortKey>(TroopSortState.DefaultKeys), SortOrderDefaults.CopyTypeOrder(), new List<string>());
            }
        }

        private bool EditingDraft => string.IsNullOrEmpty(_editingSavedName);

        public static void Open(PartyVM partyVm)
        {
            if (partyVm == null || PopupHost.IsAnyOpen)
            {
                return;
            }

            PopupHost.Show("PartyQOLSortSettings", new SortSettingsVM(partyVm));
        }

        [DataSourceProperty] public string TitleText { get => _titleText; set { if (_titleText != value) { _titleText = value; OnPropertyChangedWithValue(value, nameof(TitleText)); } } }
        [DataSourceProperty] public string HelpText { get => _helpText; set { if (_helpText != value) { _helpText = value; OnPropertyChangedWithValue(value, nameof(HelpText)); } } }
        [DataSourceProperty] public string ProfilesTitle { get => _profilesTitle; set { if (_profilesTitle != value) { _profilesTitle = value; OnPropertyChangedWithValue(value, nameof(ProfilesTitle)); } } }
        [DataSourceProperty] public string CriteriaTitle { get => _criteriaTitle; set { if (_criteriaTitle != value) { _criteriaTitle = value; OnPropertyChangedWithValue(value, nameof(CriteriaTitle)); } } }
        [DataSourceProperty] public string DetailsTitle { get => _detailsTitle; set { if (_detailsTitle != value) { _detailsTitle = value; OnPropertyChangedWithValue(value, nameof(DetailsTitle)); } } }
        [DataSourceProperty] public string ActiveSectionTitle { get => _activeSectionTitle; set { if (_activeSectionTitle != value) { _activeSectionTitle = value; OnPropertyChangedWithValue(value, nameof(ActiveSectionTitle)); } } }
        [DataSourceProperty] public string AvailableSectionTitle { get => _availableSectionTitle; set { if (_availableSectionTitle != value) { _availableSectionTitle = value; OnPropertyChangedWithValue(value, nameof(AvailableSectionTitle)); } } }
        [DataSourceProperty] public string OrderTitle { get => _orderTitle; set { if (_orderTitle != value) { _orderTitle = value; OnPropertyChangedWithValue(value, nameof(OrderTitle)); } } }
        [DataSourceProperty] public string CancelText { get => _cancelText; set { if (_cancelText != value) { _cancelText = value; OnPropertyChangedWithValue(value, nameof(CancelText)); } } }
        [DataSourceProperty] public string SaveText { get => _saveText; set { if (_saveText != value) { _saveText = value; OnPropertyChangedWithValue(value, nameof(SaveText)); } } }
        [DataSourceProperty] public string SaveAsText { get => _saveAsText; set { if (_saveAsText != value) { _saveAsText = value; OnPropertyChangedWithValue(value, nameof(SaveAsText)); } } }
        [DataSourceProperty] public string SaveAndExitText { get => _saveAndExitText; set { if (_saveAndExitText != value) { _saveAndExitText = value; OnPropertyChangedWithValue(value, nameof(SaveAndExitText)); } } }
        [DataSourceProperty] public string DeleteText { get => _deleteText; set { if (_deleteText != value) { _deleteText = value; OnPropertyChangedWithValue(value, nameof(DeleteText)); } } }
        [DataSourceProperty] public string DuplicateText { get => _duplicateText; set { if (_duplicateText != value) { _duplicateText = value; OnPropertyChangedWithValue(value, nameof(DuplicateText)); } } }
        [DataSourceProperty] public string AscendingText { get => _ascendingText; set { if (_ascendingText != value) { _ascendingText = value; OnPropertyChangedWithValue(value, nameof(AscendingText)); } } }
        [DataSourceProperty] public string DescendingText { get => _descendingText; set { if (_descendingText != value) { _descendingText = value; OnPropertyChangedWithValue(value, nameof(DescendingText)); } } }
        [DataSourceProperty] public string NewText { get => _newText; set { if (_newText != value) { _newText = value; OnPropertyChangedWithValue(value, nameof(NewText)); } } }
        [DataSourceProperty] public string RenameText { get => _renameText; set { if (_renameText != value) { _renameText = value; OnPropertyChangedWithValue(value, nameof(RenameText)); } } }
        [DataSourceProperty] public string RestoreText { get => _restoreText; set { if (_restoreText != value) { _restoreText = value; OnPropertyChangedWithValue(value, nameof(RestoreText)); } } }

        [DataSourceProperty]
        public MBBindingList<SortProfileItemVM> Profiles
        {
            get => _profiles;
            set { if (_profiles != value) { _profiles = value; OnPropertyChangedWithValue(value, nameof(Profiles)); } }
        }

        [DataSourceProperty]
        public MBBindingList<SortChipVM> ActiveChips
        {
            get => _activeChips;
            set { if (_activeChips != value) { _activeChips = value; OnPropertyChangedWithValue(value, nameof(ActiveChips)); } }
        }

        [DataSourceProperty]
        public MBBindingList<SortChipVM> AvailableChips
        {
            get => _availableChips;
            set { if (_availableChips != value) { _availableChips = value; OnPropertyChangedWithValue(value, nameof(AvailableChips)); } }
        }

        [DataSourceProperty]
        public MBBindingList<OrderChipVM> DetailOrderChips
        {
            get => _detailOrderChips;
            set { if (_detailOrderChips != value) { _detailOrderChips = value; OnPropertyChangedWithValue(value, nameof(DetailOrderChips)); } }
        }

        [DataSourceProperty]
        public bool CanDelete
        {
            get => _canDelete;
            set { if (_canDelete != value) { _canDelete = value; OnPropertyChangedWithValue(value, nameof(CanDelete)); } }
        }

        [DataSourceProperty]
        public bool CanRename
        {
            get => _canRename;
            set { if (_canRename != value) { _canRename = value; OnPropertyChangedWithValue(value, nameof(CanRename)); } }
        }

        [DataSourceProperty]
        public bool CanRestore
        {
            get => _canRestore;
            set { if (_canRestore != value) { _canRestore = value; OnPropertyChangedWithValue(value, nameof(CanRestore)); } }
        }

        [DataSourceProperty]
        public float DeleteButtonAlpha
        {
            get => _deleteButtonAlpha;
            set { if (Math.Abs(_deleteButtonAlpha - value) > 0.001f) { _deleteButtonAlpha = value; OnPropertyChangedWithValue(value, nameof(DeleteButtonAlpha)); } }
        }

        [DataSourceProperty]
        public float RenameButtonAlpha
        {
            get => _renameButtonAlpha;
            set { if (Math.Abs(_renameButtonAlpha - value) > 0.001f) { _renameButtonAlpha = value; OnPropertyChangedWithValue(value, nameof(RenameButtonAlpha)); } }
        }

        [DataSourceProperty]
        public float RestoreButtonAlpha
        {
            get => _restoreButtonAlpha;
            set { if (Math.Abs(_restoreButtonAlpha - value) > 0.001f) { _restoreButtonAlpha = value; OnPropertyChangedWithValue(value, nameof(RestoreButtonAlpha)); } }
        }

        [DataSourceProperty]
        public bool HasSelectedCriterion
        {
            get => _hasSelectedCriterion;
            set { if (_hasSelectedCriterion != value) { _hasSelectedCriterion = value; OnPropertyChangedWithValue(value, nameof(HasSelectedCriterion)); } }
        }

        [DataSourceProperty]
        public bool ShowOrderDetails
        {
            get => _showOrderDetails;
            set { if (_showOrderDetails != value) { _showOrderDetails = value; OnPropertyChangedWithValue(value, nameof(ShowOrderDetails)); } }
        }

        [DataSourceProperty]
        public bool ShowDirectionDetails
        {
            get => _showDirectionDetails;
            set { if (_showDirectionDetails != value) { _showDirectionDetails = value; OnPropertyChangedWithValue(value, nameof(ShowDirectionDetails)); } }
        }

        [DataSourceProperty]
        public bool SelectedIsAscending
        {
            get => _selectedIsAscending;
            set
            {
                if (_selectedIsAscending != value)
                {
                    _selectedIsAscending = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedIsAscending));
                    if (_selectedChip != null && _selectedChip.IsActive)
                    {
                        _selectedChip.IsAscending = value;
                        OnChipChanged();
                    }
                }
            }
        }

        public void ExecuteDrop(SortChipVM dragged, int index, string tag)
        {
            if (dragged == null || ActiveChips == null || AvailableChips == null)
            {
                return;
            }

            bool toActive = string.Equals(tag, "active", StringComparison.OrdinalIgnoreCase);
            MBBindingList<SortChipVM> target = toActive ? ActiveChips : AvailableChips;
            MBBindingList<SortChipVM> source = FindChipList(dragged);
            if (source == null || target == null)
            {
                return;
            }

            if (source == ActiveChips && source.Count <= 1 && target == AvailableChips)
            {
                return;
            }

            SortChipVM chip = FindChipByMode(dragged.Mode) ?? dragged;
            int from = source.IndexOf(chip);
            if (from < 0)
            {
                from = source.IndexOf(dragged);
            }

            if (from < 0)
            {
                return;
            }

            chip = source[from];
            source.RemoveAt(from);
            if (source == target && index > from)
            {
                index--;
            }

            if (index < 0)
            {
                index = target.Count;
            }

            index = Math.Max(0, Math.Min(index, target.Count));
            chip.IsActive = toActive;
            chip.ShowDirection = toActive;
            if (!toActive)
            {
                chip.IsAscending = true;
            }

            target.Insert(index, chip);
            // Do not promote Available→Active here — SelectCriterion's click path does that.
            SelectCriterion(chip, promoteFromAvailable: false);
            RefreshActionFlags();
        }

        public void ExecuteNew()
        {
            BeginDraft(new List<SortKey>(TroopSortState.DefaultKeys), SortOrderDefaults.CopyTypeOrder(), new List<string>());
        }

        public void ExecuteOrderDrop(OrderChipVM dragged, int index, string tag)
        {
            if (dragged == null || DetailOrderChips == null || !DetailOrderChips.Contains(dragged))
            {
                return;
            }

            int from = DetailOrderChips.IndexOf(dragged);
            if (from < 0)
            {
                return;
            }

            DetailOrderChips.RemoveAt(from);
            if (from < index)
            {
                index--;
            }

            index = Math.Max(0, Math.Min(index, DetailOrderChips.Count));
            DetailOrderChips.Insert(index, dragged);
            PersistDetailOrderFromChips();
        }

        public void ExecuteToggleSelectedDirection()
        {
            if (_selectedChip == null || !_selectedChip.IsActive)
            {
                return;
            }

            SelectedIsAscending = !SelectedIsAscending;
        }

        public void ExecuteSave() => TryPersist(closeAfter: false);

        public void ExecuteSaveAs() => PromptSaveAs(CollectActiveKeys(), SortPresets.NextFreeName(), closeAfter: false);

        public void ExecuteSaveAndExit()
        {
            if (TryPersist(closeAfter: true))
            {
                Close();
            }
        }

        public void ExecuteDuplicate()
        {
            string suggested = SortPresets.NextFreeName();
            if (!EditingDraft && !string.IsNullOrEmpty(_editingSavedName))
            {
                suggested = TruncateCopyName(_editingSavedName);
            }

            PromptSaveAs(CollectActiveKeys(), suggested, closeAfter: false);
        }

        public void ExecuteRename()
        {
            if (!CanRename || string.IsNullOrEmpty(_editingSavedName))
            {
                return;
            }

            string current = _editingSavedName;
            InformationManager.ShowTextInquiry(new TextInquiryData(
                new TextObject("{=pqol_rename_title}Rename").ToString(),
                new TextObject("{=pqol_rename_sort_body}New name for this sort order:").ToString(),
                true,
                true,
                new TextObject("{=pqol_rename}Rename").ToString(),
                new TextObject("{=pqol_sort_cancel}Cancel").ToString(),
                text =>
                {
                    if (!SortPresets.TryRename(current, text, out string saved, out string error))
                    {
                        InformationManager.DisplayMessage(new InformationMessage(error));
                        return;
                    }

                    if (string.Equals(_openSnapshotActiveName, current, StringComparison.OrdinalIgnoreCase))
                    {
                        _openSnapshotActiveName = saved;
                    }

                    _editingSavedName = saved;
                    RebuildProfiles();
                    PartyVMMixin.Current?.RefreshSortFilterSelectors();
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=pqol_renamed}Renamed to {NAME}")
                            .SetTextVariable("NAME", saved).ToString()));
                },
                () => { },
                false,
                null,
                "",
                current));
        }

        public void ExecuteDelete()
        {
            if (!CanDelete || EditingDraft)
            {
                return;
            }

            string name = _editingSavedName;
            if (!SortPresets.TryDelete(name, out string error))
            {
                InformationManager.DisplayMessage(new InformationMessage(error));
                return;
            }

            PartyVMMixin.Current?.RefreshSortFilterSelectors();
            BeginDraft(new List<SortKey>(TroopSortState.DefaultKeys), SortOrderDefaults.CopyTypeOrder(), new List<string>());
            if (_partyVm?.PartyScreenLogic != null && SortPresets.HasActiveSaved)
            {
                TroopRosterSorter.SortPartyScreen(_partyVm);
            }
        }

        public void ExecuteRestore()
        {
            if (!CanRestore || string.IsNullOrEmpty(_editingSavedName))
            {
                return;
            }

            SortPreset factory = SortPresets.CreateFactoryPreset(_editingSavedName);
            _typeOrder = SortOrderDefaults.NormalizeTypeOrder(factory.TypeOrder);
            _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(factory.CultureOrder);
            RebuildChips(factory.Keys);
            RebuildProfiles();
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=pqol_restored}Restored factory defaults.").ToString()));
        }

        public override void ExecuteCancel()
        {
            RestoreSnapshot();
            Close();
        }

        internal void SelectProfile(SortProfileItemVM item)
        {
            if (item == null)
            {
                return;
            }

            if (item.IsDraft)
            {
                if (!EditingDraft)
                {
                    BeginDraft(new List<SortKey>(TroopSortState.DefaultKeys), SortOrderDefaults.CopyTypeOrder(), new List<string>());
                }

                return;
            }

            if (!SortPresets.TryGet(item.Name, out SortPreset preset))
            {
                return;
            }

            _editingSavedName = preset.Name;
            _typeOrder = SortOrderDefaults.NormalizeTypeOrder(preset.TypeOrder);
            _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(preset.CultureOrder);
            RebuildChips(preset.Keys);
            RebuildProfiles();
        }

        private MBBindingList<SortChipVM> FindChipList(SortChipVM chip)
        {
            if (chip == null)
            {
                return null;
            }

            if (ActiveChips != null && (ActiveChips.Contains(chip) || ActiveChips.Any(c => c.Mode == chip.Mode)))
            {
                return ActiveChips;
            }

            if (AvailableChips != null && (AvailableChips.Contains(chip) || AvailableChips.Any(c => c.Mode == chip.Mode)))
            {
                return AvailableChips;
            }

            return null;
        }

        private SortChipVM FindChipByMode(TroopSortMode mode)
        {
            return ActiveChips?.FirstOrDefault(c => c.Mode == mode)
                   ?? AvailableChips?.FirstOrDefault(c => c.Mode == mode);
        }

        internal void SelectCriterion(SortChipVM chip, bool promoteFromAvailable = false)
        {
            if (chip == null)
            {
                return;
            }

            // Click only selects. Move Available↔Sorting via drag-drop (ExecuteDrop).
            if (promoteFromAvailable
                && !chip.IsActive
                && AvailableChips != null
                && AvailableChips.Contains(chip))
            {
                AvailableChips.Remove(chip);
                chip.IsActive = true;
                chip.ShowDirection = true;
                ActiveChips.Add(chip);
            }

            _selectedChip = chip;
            foreach (SortChipVM c in ActiveChips.Concat(AvailableChips ?? Enumerable.Empty<SortChipVM>()))
            {
                c.IsSelected = ReferenceEquals(c, chip) || c.Mode == chip.Mode;
            }

            HasSelectedCriterion = true;
            ShowDirectionDetails = false;
            _selectedIsAscending = chip.IsAscending;
            OnPropertyChanged(nameof(SelectedIsAscending));
            ShowOrderDetails = chip.IsActive && (chip.Mode == TroopSortMode.Type || chip.Mode == TroopSortMode.Culture);
            RebuildDetailOrder();
            RefreshActionFlags();
        }

        internal void OnChipChanged()
        {
            if (_selectedChip != null)
            {
                SelectedIsAscending = _selectedChip.IsAscending;
            }

            RefreshActionFlags();
        }

        private bool TryPersist(bool closeAfter)
        {
            List<SortKey> keys = CollectActiveKeys();
            if (EditingDraft)
            {
                PromptSaveAs(keys, SortPresets.NextFreeName(), closeAfter);
                return false;
            }

            if (!CommitSave(_editingSavedName, keys, out _, out string err))
            {
                InformationManager.DisplayMessage(new InformationMessage(err));
                return false;
            }

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=pqol_sort_saved}Sort order saved.").ToString()));
            return true;
        }

        private void PromptSaveAs(List<SortKey> keys, string suggested, bool closeAfter)
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                new TextObject("{=pqol_sort_saveas_title}Save sort order").ToString(),
                new TextObject("{=pqol_sort_saveas_body}Name for this sort order:").ToString(),
                true,
                true,
                new TextObject("{=pqol_sort_save}Save").ToString(),
                new TextObject("{=pqol_sort_cancel}Cancel").ToString(),
                text =>
                {
                    if (!CommitSave(text, keys, out string saved, out string error))
                    {
                        InformationManager.DisplayMessage(new InformationMessage(error));
                        return;
                    }

                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=pqol_sort_saved_as}Saved as {NAME}")
                            .SetTextVariable("NAME", saved).ToString()));
                    if (closeAfter)
                    {
                        Close();
                    }
                },
                () => { },
                false,
                null,
                "",
                suggested));
        }

        private bool CommitSave(string name, List<SortKey> keys, out string saved, out string error)
        {
            PersistDetailOrderFromChips();
            if (!SortPresets.TrySaveAs(name, keys, _typeOrder, _cultureOrder, out saved, out error))
            {
                return false;
            }

            _editingSavedName = saved;
            RebuildChips(keys);
            RebuildProfiles();
            // Live party order only if this preset is already selected on the party bar.
            if (string.Equals(SortPresets.ActiveName, saved, StringComparison.OrdinalIgnoreCase))
            {
                ApplyToParty(keys);
            }

            PartyVMMixin.Current?.RefreshSortFilterSelectors();
            return true;
        }

        private void BeginDraft(IReadOnlyList<SortKey> seed, IReadOnlyList<string> typeOrder, IReadOnlyList<string> cultureOrder)
        {
            _editingSavedName = string.Empty;
            _typeOrder = SortOrderDefaults.NormalizeTypeOrder(typeOrder);
            _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(cultureOrder);
            RebuildChips(seed);
            RebuildProfiles();
        }

        private void RebuildProfiles()
        {
            var list = new MBBindingList<SortProfileItemVM>();
            if (EditingDraft)
            {
                list.Add(new SortProfileItemVM(this, SortPresets.NewCustomName, isDraft: true)
                {
                    IsSelected = true
                });
            }

            foreach (SortPreset preset in SortPresets.All)
            {
                list.Add(new SortProfileItemVM(this, preset.Name, isDraft: false)
                {
                    IsSelected = !EditingDraft
                                 && string.Equals(preset.Name, _editingSavedName, StringComparison.OrdinalIgnoreCase)
                });
            }

            Profiles = list;
            RefreshActionFlags();
        }

        private void RebuildChips(IReadOnlyList<SortKey> activeKeys)
        {
            var active = new MBBindingList<SortChipVM>();
            var used = new HashSet<TroopSortMode>();
            foreach (SortKey key in activeKeys ?? Array.Empty<SortKey>())
            {
                active.Add(new SortChipVM(this, key.Mode, key.Ascending, showDirection: true, isActive: true));
                used.Add(key.Mode);
            }

            if (active.Count == 0)
            {
                active.Add(new SortChipVM(this, TroopSortMode.Type, ascending: false, showDirection: true, isActive: true));
                used.Add(TroopSortMode.Type);
            }

            var available = new MBBindingList<SortChipVM>();
            foreach (TroopSortMode mode in TroopSortState.AllModes())
            {
                if (!used.Contains(mode))
                {
                    // Type/Culture order lists use Desc = top first; other modes default Asc.
                    bool ascending = mode != TroopSortMode.Type && mode != TroopSortMode.Culture;
                    available.Add(new SortChipVM(this, mode, ascending, showDirection: false, isActive: false));
                }
            }

            ActiveChips = active;
            AvailableChips = available;
            SelectCriterion(active[0]);
        }

        private void RebuildDetailOrder()
        {
            var list = new MBBindingList<OrderChipVM>();
            if (_selectedChip == null || !_selectedChip.IsActive)
            {
                DetailOrderChips = list;
                return;
            }

            if (_selectedChip.Mode == TroopSortMode.Type)
            {
                foreach (string id in _typeOrder)
                {
                    list.Add(new OrderChipVM(this, id, id));
                }
            }
            else if (_selectedChip.Mode == TroopSortMode.Culture)
            {
                EnsureCultureOrderSeeded();
                foreach (string id in _cultureOrder)
                {
                    list.Add(new OrderChipVM(this, id, id));
                }
            }

            DetailOrderChips = list;
        }

        private void EnsureCultureOrderSeeded()
        {
            // Always merge the full campaign culture list (not only cultures currently in party).
            _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(_cultureOrder);
            if (_partyVm == null)
            {
                return;
            }

            var ids = new HashSet<string>(_cultureOrder, StringComparer.OrdinalIgnoreCase);
            CollectCultures(_partyVm.MainPartyTroops, ids);
            CollectCultures(_partyVm.MainPartyPrisoners, ids);
            CollectCultures(_partyVm.OtherPartyTroops, ids);
            CollectCultures(_partyVm.OtherPartyPrisoners, ids);
            _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(ids);
        }

        private static void CollectCultures(MBBindingList<PartyCharacterVM> list, HashSet<string> ids)
        {
            if (list == null)
            {
                return;
            }

            foreach (PartyCharacterVM item in list)
            {
                string id = item?.Character?.Culture?.StringId;
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }
        }

        private void PersistDetailOrderFromChips()
        {
            if (_selectedChip == null || DetailOrderChips == null)
            {
                return;
            }

            List<string> ids = DetailOrderChips.Select(c => c.Id).ToList();
            if (_selectedChip.Mode == TroopSortMode.Type)
            {
                _typeOrder = SortOrderDefaults.NormalizeTypeOrder(ids);
            }
            else if (_selectedChip.Mode == TroopSortMode.Culture)
            {
                _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(ids);
            }
        }

        private List<SortKey> CollectActiveKeys()
        {
            return ActiveChips?.Select(c => c.ToKey()).ToList()
                   ?? new List<SortKey>(TroopSortState.DefaultKeys);
        }

        private void RefreshActionFlags()
        {
            bool customSaved = !EditingDraft && !SortPresets.IsFactoryName(_editingSavedName);
            CanDelete = customSaved;
            CanRename = customSaved;
            CanRestore = !EditingDraft && SortPresets.IsFactoryName(_editingSavedName);
            DeleteButtonAlpha = CanDelete ? 1f : 0.4f;
            RenameButtonAlpha = CanRename ? 1f : 0.4f;
            RestoreButtonAlpha = CanRestore ? 1f : 0.4f;
        }

        private void ApplyToParty(IReadOnlyList<SortKey> keys)
        {
            TroopSortState.SetAll(keys, _typeOrder, _cultureOrder);
            if (_partyVm?.PartyScreenLogic != null)
            {
                TroopRosterSorter.SortPartyScreen(_partyVm, keys);
            }

            PartyVMMixin.Current?.RefreshSortFilterSelectors();
        }

        private void RestoreSnapshot()
        {
            TroopSortState.SetAll(_openSnapshotKeys, _openSnapshotTypeOrder, _openSnapshotCultureOrder);
            if (!string.IsNullOrEmpty(_openSnapshotActiveName) && SortPresets.Activate(_openSnapshotActiveName))
            {
                if (_partyVm?.PartyScreenLogic != null)
                {
                    TroopRosterSorter.SortPartyScreen(_partyVm);
                }
            }
            else
            {
                SortPresets.ClearActive();
            }

            PartyVMMixin.Current?.RefreshSortFilterSelectors();
        }

        private static string TruncateCopyName(string source)
        {
            string stem = source + " Copy";
            if (stem.Length <= SortPresets.MaxNameLength)
            {
                return SortPresets.All.Any(p => string.Equals(p.Name, stem, StringComparison.OrdinalIgnoreCase))
                    ? SortPresets.NextFreeName()
                    : stem;
            }

            return SortPresets.NextFreeName();
        }
    }
}
