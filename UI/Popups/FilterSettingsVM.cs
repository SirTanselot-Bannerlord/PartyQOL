using System;
using System.Collections.Generic;
using System.Linq;
using PartyQOL.Filtering;
using PartyQOL.Sorting;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.UI.Popups
{
    // parity-ok: wounded/healthy mode is filter-only
    public sealed class FilterSettingsVM : PopupVM
    {
        private readonly PartyVM _partyVm;
        private string _openSnapshotActiveName;
        private FilterPreset _draft;
        private string _editingSavedName = string.Empty;
        private FilterChipVM _selectedChip;
        private MBBindingList<FilterProfileItemVM> _profiles;
        private MBBindingList<FilterChipVM> _criteriaChips;
        private MBBindingList<FilterToggleChipVM> _detailToggles;
        private bool _canDelete;
        private bool _canRename;
        private bool _canRestore;
        private float _deleteButtonAlpha = 1f;
        private float _renameButtonAlpha = 1f;
        private float _restoreButtonAlpha = 1f;
        private bool _hasSelectedCriterion;
        private bool _showToggleDetails;
        private bool _showRangeDetails;
        private bool _showConditionDetails;
        private bool _conditionIsOff = true;
        private bool _conditionIsWounded;
        private bool _conditionIsHealthy;
        private bool _criterionEnabled;
        private int _rangeMin;
        private int _rangeMax;
        private float _lowSlider;
        private float _highSlider = PartyQOL.UI.Widgets.PartyQolRangeSliderWidget.Resolution;
        private string _rangeMinText = "0";
        private string _rangeMaxText = "6";
        private bool _syncingRange;
        private string _titleText;
        private string _helpText;
        private string _profilesTitle;
        private string _criteriaTitle;
        private string _detailsTitle;
        private string _enabledText;
        private string _rangeText;
        private string _cancelText;
        private string _saveText;
        private string _saveAsText;
        private string _saveAndExitText;
        private string _deleteText;
        private string _duplicateText;
        private string _newText;
        private string _renameText;
        private string _restoreText;

        private FilterSettingsVM(PartyVM partyVm)
        {
            _partyVm = partyVm;
            FilterPresets.EnsureLoadedPublic();
            SeedCulturesFromParty();
            _openSnapshotActiveName = FilterPresets.ActiveName;

            _titleText = new TextObject("{=pqol_filter_title}Troop Filter").ToString();
            _helpText = new TextObject("{=pqol_filter_help3}Save stores the preset. Choose it on the party bar to apply. Use New for a draft until saved.").ToString();
            _profilesTitle = new TextObject("{=pqol_filter_profiles}Filters").ToString();
            _newText = new TextObject("{=pqol_new}New").ToString();
            _renameText = new TextObject("{=pqol_rename}Rename").ToString();
            _restoreText = new TextObject("{=pqol_restore}Restore").ToString();
            _criteriaTitle = new TextObject("{=pqol_filter_criteria}Criteria").ToString();
            _detailsTitle = new TextObject("{=pqol_filter_details}Details").ToString();
            _enabledText = new TextObject("{=pqol_filter_enabled}Enabled").ToString();
            _rangeText = new TextObject("{=pqol_filter_range}Range").ToString();
            _cancelText = new TextObject("{=pqol_sort_cancel}Cancel").ToString();
            _saveText = new TextObject("{=pqol_sort_save}Save").ToString();
            _saveAsText = new TextObject("{=pqol_sort_saveas}Save As").ToString();
            _saveAndExitText = new TextObject("{=pqol_sort_save_exit}Save and Exit").ToString();
            _deleteText = new TextObject("{=pqol_sort_delete}Delete").ToString();
            _duplicateText = new TextObject("{=pqol_sort_duplicate}Duplicate").ToString();

            if (FilterPresets.HasActiveSaved && FilterPresets.TryGet(FilterPresets.ActiveName, out FilterPreset active))
            {
                _editingSavedName = active.Name;
                _draft = active.Clone();
                SeedDraftCultures();
                RebuildCriteria();
                RebuildProfiles();
            }
            else
            {
                BeginDraft(FilterPresets.CreateDraft());
            }
        }

        private bool EditingDraft => string.IsNullOrEmpty(_editingSavedName);

        public static void Open(PartyVM partyVm)
        {
            if (partyVm == null || PopupHost.IsAnyOpen)
            {
                return;
            }

            PopupHost.Show("PartyQOLFilterSettings", new FilterSettingsVM(partyVm));
        }

        [DataSourceProperty] public string TitleText { get => _titleText; set { if (_titleText != value) { _titleText = value; OnPropertyChangedWithValue(value, nameof(TitleText)); } } }
        [DataSourceProperty] public string HelpText { get => _helpText; set { if (_helpText != value) { _helpText = value; OnPropertyChangedWithValue(value, nameof(HelpText)); } } }
        [DataSourceProperty] public string ProfilesTitle { get => _profilesTitle; set { if (_profilesTitle != value) { _profilesTitle = value; OnPropertyChangedWithValue(value, nameof(ProfilesTitle)); } } }
        [DataSourceProperty] public string CriteriaTitle { get => _criteriaTitle; set { if (_criteriaTitle != value) { _criteriaTitle = value; OnPropertyChangedWithValue(value, nameof(CriteriaTitle)); } } }
        [DataSourceProperty] public string DetailsTitle { get => _detailsTitle; set { if (_detailsTitle != value) { _detailsTitle = value; OnPropertyChangedWithValue(value, nameof(DetailsTitle)); } } }
        [DataSourceProperty] public string EnabledText { get => _enabledText; set { if (_enabledText != value) { _enabledText = value; OnPropertyChangedWithValue(value, nameof(EnabledText)); } } }
        [DataSourceProperty] public string RangeText { get => _rangeText; set { if (_rangeText != value) { _rangeText = value; OnPropertyChangedWithValue(value, nameof(RangeText)); } } }
        [DataSourceProperty] public string CancelText { get => _cancelText; set { if (_cancelText != value) { _cancelText = value; OnPropertyChangedWithValue(value, nameof(CancelText)); } } }
        [DataSourceProperty] public string SaveText { get => _saveText; set { if (_saveText != value) { _saveText = value; OnPropertyChangedWithValue(value, nameof(SaveText)); } } }
        [DataSourceProperty] public string SaveAsText { get => _saveAsText; set { if (_saveAsText != value) { _saveAsText = value; OnPropertyChangedWithValue(value, nameof(SaveAsText)); } } }
        [DataSourceProperty] public string SaveAndExitText { get => _saveAndExitText; set { if (_saveAndExitText != value) { _saveAndExitText = value; OnPropertyChangedWithValue(value, nameof(SaveAndExitText)); } } }
        [DataSourceProperty] public string DeleteText { get => _deleteText; set { if (_deleteText != value) { _deleteText = value; OnPropertyChangedWithValue(value, nameof(DeleteText)); } } }
        [DataSourceProperty] public string DuplicateText { get => _duplicateText; set { if (_duplicateText != value) { _duplicateText = value; OnPropertyChangedWithValue(value, nameof(DuplicateText)); } } }
        [DataSourceProperty] public string NewText { get => _newText; set { if (_newText != value) { _newText = value; OnPropertyChangedWithValue(value, nameof(NewText)); } } }
        [DataSourceProperty] public string RenameText { get => _renameText; set { if (_renameText != value) { _renameText = value; OnPropertyChangedWithValue(value, nameof(RenameText)); } } }
        [DataSourceProperty] public string RestoreText { get => _restoreText; set { if (_restoreText != value) { _restoreText = value; OnPropertyChangedWithValue(value, nameof(RestoreText)); } } }

        [DataSourceProperty]
        public MBBindingList<FilterProfileItemVM> Profiles
        {
            get => _profiles;
            set { if (_profiles != value) { _profiles = value; OnPropertyChangedWithValue(value, nameof(Profiles)); } }
        }

        [DataSourceProperty]
        public MBBindingList<FilterChipVM> CriteriaChips
        {
            get => _criteriaChips;
            set { if (_criteriaChips != value) { _criteriaChips = value; OnPropertyChangedWithValue(value, nameof(CriteriaChips)); } }
        }

        [DataSourceProperty]
        public MBBindingList<FilterToggleChipVM> DetailToggles
        {
            get => _detailToggles;
            set { if (_detailToggles != value) { _detailToggles = value; OnPropertyChangedWithValue(value, nameof(DetailToggles)); } }
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
        public bool ShowToggleDetails
        {
            get => _showToggleDetails;
            set { if (_showToggleDetails != value) { _showToggleDetails = value; OnPropertyChangedWithValue(value, nameof(ShowToggleDetails)); } }
        }

        [DataSourceProperty]
        public bool ShowRangeDetails
        {
            get => _showRangeDetails;
            set { if (_showRangeDetails != value) { _showRangeDetails = value; OnPropertyChangedWithValue(value, nameof(ShowRangeDetails)); } }
        }

        [DataSourceProperty]
        public bool ShowConditionDetails
        {
            get => _showConditionDetails;
            set { if (_showConditionDetails != value) { _showConditionDetails = value; OnPropertyChangedWithValue(value, nameof(ShowConditionDetails)); } }
        }

        [DataSourceProperty]
        public bool ConditionIsOff
        {
            get => _conditionIsOff;
            set { if (_conditionIsOff != value) { _conditionIsOff = value; OnPropertyChangedWithValue(value, nameof(ConditionIsOff)); } }
        }

        [DataSourceProperty]
        public bool ConditionIsWounded
        {
            get => _conditionIsWounded;
            set { if (_conditionIsWounded != value) { _conditionIsWounded = value; OnPropertyChangedWithValue(value, nameof(ConditionIsWounded)); } }
        }

        [DataSourceProperty]
        public bool ConditionIsHealthy
        {
            get => _conditionIsHealthy;
            set { if (_conditionIsHealthy != value) { _conditionIsHealthy = value; OnPropertyChangedWithValue(value, nameof(ConditionIsHealthy)); } }
        }

        [DataSourceProperty]
        public bool CriterionEnabled
        {
            get => _criterionEnabled;
            set
            {
                if (_criterionEnabled != value)
                {
                    _criterionEnabled = value;
                    OnPropertyChangedWithValue(value, nameof(CriterionEnabled));
                    ApplyCriterionEnabled(value);
                }
            }
        }

        [DataSourceProperty]
        public int RangeMin
        {
            get => _rangeMin;
            set
            {
                if (_rangeMin != value)
                {
                    _rangeMin = value;
                    OnPropertyChangedWithValue(value, nameof(RangeMin));
                    if (!_syncingRange)
                    {
                        SyncRangeUiFromValues();
                        ApplyRange();
                    }
                }
            }
        }

        [DataSourceProperty]
        public int RangeMax
        {
            get => _rangeMax;
            set
            {
                if (_rangeMax != value)
                {
                    _rangeMax = value;
                    OnPropertyChangedWithValue(value, nameof(RangeMax));
                    if (!_syncingRange)
                    {
                        SyncRangeUiFromValues();
                        ApplyRange();
                    }
                }
            }
        }

        [DataSourceProperty]
        public float LowSlider
        {
            get => _lowSlider;
            set
            {
                if (System.Math.Abs(value - _lowSlider) >= 0.01f)
                {
                    _lowSlider = value;
                    OnPropertyChangedWithValue(value, nameof(LowSlider));
                    if (!_syncingRange)
                    {
                        SetRangeFromSliders();
                    }
                }
            }
        }

        [DataSourceProperty]
        public float HighSlider
        {
            get => _highSlider;
            set
            {
                if (System.Math.Abs(value - _highSlider) >= 0.01f)
                {
                    _highSlider = value;
                    OnPropertyChangedWithValue(value, nameof(HighSlider));
                    if (!_syncingRange)
                    {
                        SetRangeFromSliders();
                    }
                }
            }
        }

        [DataSourceProperty]
        public string RangeMinText
        {
            get => _rangeMinText;
            set
            {
                if (_rangeMinText != value)
                {
                    _rangeMinText = value;
                    OnPropertyChangedWithValue(value, nameof(RangeMinText));
                    if (!_syncingRange && int.TryParse(value, out int parsed))
                    {
                        RangeMin = System.Math.Min(parsed, RangeMax);
                    }
                }
            }
        }

        [DataSourceProperty]
        public string RangeMaxText
        {
            get => _rangeMaxText;
            set
            {
                if (_rangeMaxText != value)
                {
                    _rangeMaxText = value;
                    OnPropertyChangedWithValue(value, nameof(RangeMaxText));
                    if (!_syncingRange && int.TryParse(value, out int parsed))
                    {
                        RangeMax = System.Math.Max(parsed, RangeMin);
                    }
                }
            }
        }

        public void ExecuteToggleCriterionEnabled()
        {
            CriterionEnabled = !CriterionEnabled;
        }

        public void ExecuteRangeScrollUp()
        {
            ShiftRange(1);
        }

        public void ExecuteRangeScrollDown()
        {
            ShiftRange(-1);
        }

        public void ExecuteDetailDrop(FilterToggleChipVM dragged, int index, string tag)
        {
            if (dragged == null || DetailToggles == null || !DetailToggles.Contains(dragged))
            {
                return;
            }

            int from = DetailToggles.IndexOf(dragged);
            if (from < 0)
            {
                return;
            }

            DetailToggles.RemoveAt(from);
            if (from < index)
            {
                index--;
            }

            index = Math.Max(0, Math.Min(index, DetailToggles.Count));
            DetailToggles.Insert(index, dragged);
            PersistTogglesFromDetails();
            RefreshCriteriaSummaries();
        }

        public void ExecuteNew()
        {
            BeginDraft(FilterPresets.CreateDraft());
        }

        public void ExecuteSave() => TryPersist(closeAfter: false);

        public void ExecuteSaveAs() => PromptSaveAs(FilterPresets.NextFreeName(), closeAfter: false);

        public void ExecuteSaveAndExit()
        {
            if (TryPersist(closeAfter: true))
            {
                Close();
            }
        }

        public void ExecuteDuplicate()
        {
            string suggested = FilterPresets.NextFreeName();
            if (!EditingDraft && !string.IsNullOrEmpty(_editingSavedName))
            {
                string stem = _editingSavedName + " Copy";
                suggested = stem.Length <= FilterPresets.MaxNameLength ? stem : FilterPresets.NextFreeName();
            }

            PromptSaveAs(suggested, closeAfter: false);
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
                new TextObject("{=pqol_rename_filter_body}New name for this filter:").ToString(),
                true,
                true,
                new TextObject("{=pqol_rename}Rename").ToString(),
                new TextObject("{=pqol_sort_cancel}Cancel").ToString(),
                text =>
                {
                    if (!FilterPresets.TryRename(current, text, out string saved, out string error))
                    {
                        InformationManager.DisplayMessage(new InformationMessage(error));
                        return;
                    }

                    if (string.Equals(_openSnapshotActiveName, current, StringComparison.OrdinalIgnoreCase))
                    {
                        _openSnapshotActiveName = saved;
                    }

                    _editingSavedName = saved;
                    if (_draft != null)
                    {
                        _draft.Name = saved;
                    }

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

            if (!FilterPresets.TryDelete(_editingSavedName, out string error))
            {
                InformationManager.DisplayMessage(new InformationMessage(error));
                return;
            }

            PartyVMMixin.Current?.RefreshSortFilterSelectors();
            TroopFilterApplicator.ApplyPartyScreen(_partyVm);
            BeginDraft(FilterPresets.CreateDraft());
        }

        public void ExecuteRestore()
        {
            // Reset criteria to factory draft defaults; keep the editing name when on a saved profile.
            string keepName = _editingSavedName;
            FilterPreset fresh = FilterPresets.CreateDraft();
            if (!string.IsNullOrEmpty(keepName))
            {
                fresh.Name = keepName;
                _editingSavedName = keepName;
                _draft = fresh;
                SeedDraftCultures();
                RebuildCriteria();
                RebuildProfiles();
            }
            else
            {
                BeginDraft(fresh);
            }

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=pqol_restored}Restored factory defaults.").ToString()));
        }

        public override void ExecuteCancel()
        {
            RestoreSnapshot();
            Close();
        }

        internal void SelectProfile(FilterProfileItemVM item)
        {
            if (item == null)
            {
                return;
            }

            if (item.IsDraft)
            {
                if (!EditingDraft)
                {
                    BeginDraft(FilterPresets.CreateDraft());
                }

                return;
            }

            if (!FilterPresets.TryGet(item.Name, out FilterPreset preset))
            {
                return;
            }

            _editingSavedName = preset.Name;
            _draft = preset.Clone();
            RebuildCriteria();
            RebuildProfiles();
        }

        internal void SelectCriterion(FilterChipVM chip)
        {
            if (chip == null || _draft == null)
            {
                return;
            }

            _selectedChip = chip;
            foreach (FilterChipVM c in CriteriaChips ?? Enumerable.Empty<FilterChipVM>())
            {
                c.IsSelected = c == chip;
            }

            HasSelectedCriterion = true;
            bool enabled = chip.Kind switch
            {
                FilterCriterionKind.Type => _draft.UseType,
                FilterCriterionKind.Culture => _draft.UseCulture,
                FilterCriterionKind.Tier => _draft.UseTier,
                FilterCriterionKind.MaxTier => _draft.UseMaxTier,
                FilterCriterionKind.Level => _draft.UseLevel,
                FilterCriterionKind.Count => _draft.UseCount,
                FilterCriterionKind.Condition => _draft.UseCondition,
                _ => false
            };
            _criterionEnabled = enabled;
            OnPropertyChanged(nameof(CriterionEnabled));

            ShowToggleDetails = chip.Kind == FilterCriterionKind.Type || chip.Kind == FilterCriterionKind.Culture;
            ShowRangeDetails = chip.Kind == FilterCriterionKind.Tier
                               || chip.Kind == FilterCriterionKind.MaxTier
                               || chip.Kind == FilterCriterionKind.Level
                               || chip.Kind == FilterCriterionKind.Count;
            ShowConditionDetails = chip.Kind == FilterCriterionKind.Condition;
            RefreshConditionFlags();
            RebuildDetails();
        }

        public void ExecuteConditionOff() => SetCondition(ConditionFilterMode.Off);

        public void ExecuteConditionWounded() => SetCondition(ConditionFilterMode.Wounded);

        public void ExecuteConditionHealthy() => SetCondition(ConditionFilterMode.Healthy);

        private void SetCondition(ConditionFilterMode mode)
        {
            if (_draft == null)
            {
                return;
            }

            _draft.ConditionMode = mode;
            bool enable = mode != ConditionFilterMode.Off;
            if (_criterionEnabled != enable)
            {
                CriterionEnabled = enable;
            }
            else
            {
                _draft.UseCondition = enable;
            }

            RefreshConditionFlags();
            RefreshCriteriaSummaries();
        }

        private void RefreshConditionFlags()
        {
            bool on = _draft != null && _draft.UseCondition && _draft.ConditionMode != ConditionFilterMode.Off;
            ConditionIsOff = !on;
            ConditionIsWounded = on && _draft.ConditionMode == ConditionFilterMode.Wounded;
            ConditionIsHealthy = on && _draft.ConditionMode == ConditionFilterMode.Healthy;
        }

        internal void OnDetailChanged()
        {
            PersistTogglesFromDetails();
            RefreshCriteriaSummaries();
        }

        private void BeginDraft(FilterPreset draft)
        {
            _editingSavedName = string.Empty;
            _draft = draft ?? FilterPresets.CreateDraft();
            SeedDraftCultures();
            RebuildCriteria();
            RebuildProfiles();
        }

        private void RebuildProfiles()
        {
            var list = new MBBindingList<FilterProfileItemVM>();
            if (EditingDraft)
            {
                list.Add(new FilterProfileItemVM(this, FilterPresets.NewCustomName, isDraft: true)
                {
                    IsSelected = true
                });
            }

            foreach (FilterPreset preset in FilterPresets.All)
            {
                list.Add(new FilterProfileItemVM(this, preset.Name, isDraft: false)
                {
                    IsSelected = !EditingDraft && string.Equals(preset.Name, _editingSavedName, StringComparison.OrdinalIgnoreCase)
                });
            }

            Profiles = list;
            RefreshActionFlags();
        }

        private void RefreshActionFlags()
        {
            CanDelete = !EditingDraft;
            CanRename = !EditingDraft;
            // Filter Restore always resets criteria to CreateDraft() defaults (including drafts).
            CanRestore = true;
            DeleteButtonAlpha = CanDelete ? 1f : 0.4f;
            RenameButtonAlpha = CanRename ? 1f : 0.4f;
            RestoreButtonAlpha = CanRestore ? 1f : 0.4f;
        }

        private void RebuildCriteria()
        {
            var list = new MBBindingList<FilterChipVM>
            {
                new FilterChipVM(this, FilterCriterionKind.Type, _draft.SummaryFor(FilterCriterionKind.Type)),
                new FilterChipVM(this, FilterCriterionKind.Culture, _draft.SummaryFor(FilterCriterionKind.Culture)),
                new FilterChipVM(this, FilterCriterionKind.Tier, _draft.SummaryFor(FilterCriterionKind.Tier)),
                new FilterChipVM(this, FilterCriterionKind.MaxTier, _draft.SummaryFor(FilterCriterionKind.MaxTier)),
                new FilterChipVM(this, FilterCriterionKind.Level, _draft.SummaryFor(FilterCriterionKind.Level)),
                new FilterChipVM(this, FilterCriterionKind.Count, _draft.SummaryFor(FilterCriterionKind.Count)),
                new FilterChipVM(this, FilterCriterionKind.Condition, _draft.SummaryFor(FilterCriterionKind.Condition))
            };
            CriteriaChips = list;
            SelectCriterion(list[0]);
        }

        private void RefreshCriteriaSummaries()
        {
            if (CriteriaChips == null || _draft == null)
            {
                return;
            }

            foreach (FilterChipVM chip in CriteriaChips)
            {
                chip.SummaryText = _draft.SummaryFor(chip.Kind);
            }
        }

        private void RebuildDetails()
        {
            var toggles = new MBBindingList<FilterToggleChipVM>();
            if (_selectedChip == null || _draft == null)
            {
                DetailToggles = toggles;
                return;
            }

            if (_selectedChip.Kind == FilterCriterionKind.Type)
            {
                foreach (FilterToggleItem item in _draft.Types)
                {
                    toggles.Add(new FilterToggleChipVM(this, item.Id, TypeDisplayName(item.Id), item.Enabled));
                }
            }
            else if (_selectedChip.Kind == FilterCriterionKind.Culture)
            {
                SeedDraftCultures();
                foreach (FilterToggleItem item in _draft.Cultures)
                {
                    toggles.Add(new FilterToggleChipVM(
                        this,
                        item.Id,
                        SortOrderDefaults.CultureDisplayName(item.Id),
                        item.Enabled));
                }
            }
            else if (_selectedChip.Kind == FilterCriterionKind.Tier)
            {
                _syncingRange = true;
                _rangeMin = _draft.TierMin;
                _rangeMax = _draft.TierMax;
                OnPropertyChanged(nameof(RangeMin));
                OnPropertyChanged(nameof(RangeMax));
                SyncRangeUiFromValues();
                _syncingRange = false;
            }
            else if (_selectedChip.Kind == FilterCriterionKind.MaxTier)
            {
                _syncingRange = true;
                _rangeMin = _draft.MaxTierMin;
                _rangeMax = _draft.MaxTierMax;
                OnPropertyChanged(nameof(RangeMin));
                OnPropertyChanged(nameof(RangeMax));
                SyncRangeUiFromValues();
                _syncingRange = false;
            }
            else if (_selectedChip.Kind == FilterCriterionKind.Level)
            {
                _syncingRange = true;
                _rangeMin = _draft.LevelMin;
                _rangeMax = _draft.LevelMax;
                OnPropertyChanged(nameof(RangeMin));
                OnPropertyChanged(nameof(RangeMax));
                SyncRangeUiFromValues();
                _syncingRange = false;
            }
            else if (_selectedChip.Kind == FilterCriterionKind.Count)
            {
                _syncingRange = true;
                _rangeMin = _draft.CountMin;
                _rangeMax = _draft.CountMax;
                OnPropertyChanged(nameof(RangeMin));
                OnPropertyChanged(nameof(RangeMax));
                SyncRangeUiFromValues();
                _syncingRange = false;
            }

            DetailToggles = toggles;
        }

        private void ApplyCriterionEnabled(bool enabled)
        {
            if (_selectedChip == null || _draft == null)
            {
                return;
            }

            switch (_selectedChip.Kind)
            {
                case FilterCriterionKind.Type: _draft.UseType = enabled; break;
                case FilterCriterionKind.Culture: _draft.UseCulture = enabled; break;
                case FilterCriterionKind.Tier: _draft.UseTier = enabled; break;
                case FilterCriterionKind.MaxTier: _draft.UseMaxTier = enabled; break;
                case FilterCriterionKind.Level: _draft.UseLevel = enabled; break;
                case FilterCriterionKind.Count: _draft.UseCount = enabled; break;
                case FilterCriterionKind.Condition:
                    _draft.UseCondition = enabled;
                    if (enabled && _draft.ConditionMode == ConditionFilterMode.Off)
                    {
                        _draft.ConditionMode = ConditionFilterMode.Wounded;
                    }

                    RefreshConditionFlags();
                    break;
            }

            RefreshCriteriaSummaries();
        }

        private void ApplyRange()
        {
            if (_selectedChip == null || _draft == null || !ShowRangeDetails)
            {
                return;
            }

            int min = Math.Min(RangeMin, RangeMax);
            int max = Math.Max(RangeMin, RangeMax);
            if (_selectedChip.Kind == FilterCriterionKind.Tier)
            {
                _draft.TierMin = min;
                _draft.TierMax = max;
            }
            else if (_selectedChip.Kind == FilterCriterionKind.MaxTier)
            {
                _draft.MaxTierMin = min;
                _draft.MaxTierMax = max;
            }
            else if (_selectedChip.Kind == FilterCriterionKind.Level)
            {
                _draft.LevelMin = min;
                _draft.LevelMax = max;
            }
            else if (_selectedChip.Kind == FilterCriterionKind.Count)
            {
                _draft.CountMin = min;
                _draft.CountMax = max;
            }

            RefreshCriteriaSummaries();
        }

        private void PersistTogglesFromDetails()
        {
            if (_selectedChip == null || _draft == null || DetailToggles == null)
            {
                return;
            }

            List<FilterToggleItem> items = DetailToggles
                .Select(t => new FilterToggleItem { Id = t.Id, Enabled = t.IsEnabled })
                .ToList();
            if (_selectedChip.Kind == FilterCriterionKind.Type)
            {
                _draft.Types = items;
            }
            else if (_selectedChip.Kind == FilterCriterionKind.Culture)
            {
                _draft.Cultures = items;
            }
        }

        private int GetRangeFloor() =>
            _selectedChip?.Kind == FilterCriterionKind.Level || _selectedChip?.Kind == FilterCriterionKind.Count
                ? 1
                : 0;

        private int GetRangeCeiling() =>
            _selectedChip?.Kind == FilterCriterionKind.Count
                ? 999
                : _selectedChip?.Kind == FilterCriterionKind.Level
                    ? 99
                    : 6;

        private void SetRangeFromSliders()
        {
            int floor = GetRangeFloor();
            int ceiling = GetRangeCeiling();
            float span = ceiling - floor;
            if (span <= 0f)
            {
                return;
            }

            float res = PartyQOL.UI.Widgets.PartyQolRangeSliderWidget.Resolution;
            int min = floor + (int)System.Math.Round(_lowSlider / res * span);
            int max = floor + (int)System.Math.Round(_highSlider / res * span);
            min = System.Math.Max(floor, System.Math.Min(ceiling, min));
            max = System.Math.Max(min, System.Math.Min(ceiling, max));
            _syncingRange = true;
            _rangeMin = min;
            _rangeMax = max;
            OnPropertyChanged(nameof(RangeMin));
            OnPropertyChanged(nameof(RangeMax));
            _rangeMinText = min.ToString();
            _rangeMaxText = max.ToString();
            OnPropertyChanged(nameof(RangeMinText));
            OnPropertyChanged(nameof(RangeMaxText));
            _syncingRange = false;
            ApplyRange();
        }

        private void SyncRangeUiFromValues()
        {
            int floor = GetRangeFloor();
            int ceiling = GetRangeCeiling();
            float span = ceiling - floor;
            float res = PartyQOL.UI.Widgets.PartyQolRangeSliderWidget.Resolution;
            _lowSlider = span <= 0f ? 0f : (_rangeMin - floor) / span * res;
            _highSlider = span <= 0f ? res : (_rangeMax - floor) / span * res;
            _rangeMinText = _rangeMin.ToString();
            _rangeMaxText = _rangeMax.ToString();
            OnPropertyChanged(nameof(LowSlider));
            OnPropertyChanged(nameof(HighSlider));
            OnPropertyChanged(nameof(RangeMinText));
            OnPropertyChanged(nameof(RangeMaxText));
        }

        private void ShiftRange(int direction)
        {
            int floor = GetRangeFloor();
            int ceiling = GetRangeCeiling();
            int delta = direction;
            int nextMin = System.Math.Max(floor, System.Math.Min(ceiling, RangeMin + delta));
            int nextMax = System.Math.Max(nextMin, System.Math.Min(ceiling, RangeMax + delta));
            if (nextMax - nextMin != RangeMax - RangeMin)
            {
                return;
            }

            RangeMin = nextMin;
            RangeMax = nextMax;
        }

        private bool TryPersist(bool closeAfter)
        {
            if (EditingDraft)
            {
                PromptSaveAs(FilterPresets.NextFreeName(), closeAfter);
                return false;
            }

            if (!CommitSave(_editingSavedName, out _, out string err))
            {
                InformationManager.DisplayMessage(new InformationMessage(err));
                return false;
            }

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=pqol_filter_saved}Filter saved.").ToString()));
            return true;
        }

        private void PromptSaveAs(string suggested, bool closeAfter)
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                new TextObject("{=pqol_filter_saveas_title}Save filter").ToString(),
                new TextObject("{=pqol_filter_saveas_body}Name for this filter:").ToString(),
                true,
                true,
                new TextObject("{=pqol_sort_save}Save").ToString(),
                new TextObject("{=pqol_sort_cancel}Cancel").ToString(),
                text =>
                {
                    if (!CommitSave(text, out string saved, out string error))
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

        private bool CommitSave(string name, out string saved, out string error)
        {
            PersistTogglesFromDetails();
            if (!FilterPresets.TrySaveAs(name, _draft, out saved, out error))
            {
                return false;
            }

            _editingSavedName = saved;
            _draft = FilterPresets.TryGet(saved, out FilterPreset p) ? p.Clone() : _draft;
            RebuildCriteria();
            RebuildProfiles();
            TroopFilterApplicator.ApplyPartyScreen(_partyVm);
            PartyVMMixin.Current?.RefreshSortFilterSelectors();
            return true;
        }

        private void RestoreSnapshot()
        {
            if (!string.IsNullOrEmpty(_openSnapshotActiveName) && FilterPresets.Activate(_openSnapshotActiveName))
            {
                // ok
            }
            else
            {
                FilterPresets.ClearActive();
            }

            TroopFilterApplicator.ApplyPartyScreen(_partyVm);
            PartyVMMixin.Current?.RefreshSortFilterSelectors();
        }

        private void SeedCulturesFromParty()
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectCultures(_partyVm?.MainPartyTroops, ids);
            CollectCultures(_partyVm?.MainPartyPrisoners, ids);
            CollectCultures(_partyVm?.OtherPartyTroops, ids);
            CollectCultures(_partyVm?.OtherPartyPrisoners, ids);
            FilterPresets.MergeCultureIds(ids);
        }

        private void SeedDraftCultures()
        {
            if (_draft == null)
            {
                return;
            }

            // Full campaign culture list, plus any cultures currently in the party screen.
            foreach (FilterToggleItem culture in FilterState.DefaultCultures())
            {
                if (!_draft.Cultures.Exists(c => string.Equals(c.Id, culture.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    _draft.Cultures.Add(culture.Clone());
                }
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectCultures(_partyVm?.MainPartyTroops, ids);
            CollectCultures(_partyVm?.MainPartyPrisoners, ids);
            CollectCultures(_partyVm?.OtherPartyTroops, ids);
            CollectCultures(_partyVm?.OtherPartyPrisoners, ids);
            foreach (string id in ids)
            {
                if (!_draft.Cultures.Exists(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    _draft.Cultures.Add(new FilterToggleItem { Id = id, Enabled = true });
                }
            }

            List<string> orderedIds = SortOrderDefaults.OrderCulturesForUi(_draft.Cultures.Select(c => c.Id));
            var byId = _draft.Cultures.ToDictionary(c => c.Id, c => c, StringComparer.OrdinalIgnoreCase);
            _draft.Cultures = orderedIds
                .Where(byId.ContainsKey)
                .Select(id => byId[id])
                .ToList();
        }

        private static string TypeDisplayName(string id) =>
            id switch
            {
                "Infantry" => "Infantry",
                "Ranged" => "Archers",
                "Cavalry" => "Cavalry",
                "HorseArcher" => "Horse Archers",
                _ => id
            };

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
    }
}
