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
    /// Profiles | Sorting | Available. Edits stay in the popup until Save; Cancel restores the prior party order.
    /// </summary>
    public sealed class SortSettingsVM : PopupVM
    {
        private readonly PartyVM _partyVm;
        private readonly List<SortKey> _openSnapshotKeys;
        private readonly string _openSnapshotActiveName;
        private MBBindingList<SortProfileItemVM> _profiles;
        private MBBindingList<SortChipVM> _activeChips;
        private MBBindingList<SortChipVM> _availableChips;
        private string _editingSavedName = string.Empty;
        private bool _canDelete;
        private string _titleText;
        private string _helpText;
        private string _profilesTitle;
        private string _activeTitle;
        private string _availableTitle;
        private string _cancelText;
        private string _saveText;
        private string _saveAndExitText;
        private string _deleteText;
        private string _duplicateText;
        private string _saveAsText;

        private SortSettingsVM(PartyVM partyVm)
        {
            _partyVm = partyVm;
            SortPresets.EnsureLoadedPublic();
            _openSnapshotKeys = TroopSortState.Keys.ToList();
            _openSnapshotActiveName = SortPresets.ActiveName;

            _titleText = new TextObject("{=pqol_sort_title}Troop Sort").ToString();
            _helpText = new TextObject("{=pqol_sort_help4}Edits apply to the party only when you Save. New Custom is a draft until saved.").ToString();
            _profilesTitle = new TextObject("{=pqol_sort_profiles}Orders").ToString();
            _activeTitle = new TextObject("{=pqol_sort_active}Sorting").ToString();
            _availableTitle = new TextObject("{=pqol_sort_available}Available").ToString();
            _cancelText = new TextObject("{=pqol_sort_cancel}Cancel").ToString();
            _saveText = new TextObject("{=pqol_sort_save}Save").ToString();
            _saveAsText = new TextObject("{=pqol_sort_saveas}Save As").ToString();
            _saveAndExitText = new TextObject("{=pqol_sort_save_exit}Save and Exit").ToString();
            _deleteText = new TextObject("{=pqol_sort_delete}Delete").ToString();
            _duplicateText = new TextObject("{=pqol_sort_duplicate}Duplicate").ToString();

            // New Custom always starts from the starter template, not the party’s current order.
            BeginDraft(new List<SortKey>(TroopSortState.DefaultKeys));
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
        [DataSourceProperty] public string ActiveTitle { get => _activeTitle; set { if (_activeTitle != value) { _activeTitle = value; OnPropertyChangedWithValue(value, nameof(ActiveTitle)); } } }
        [DataSourceProperty] public string AvailableTitle { get => _availableTitle; set { if (_availableTitle != value) { _availableTitle = value; OnPropertyChangedWithValue(value, nameof(AvailableTitle)); } } }
        [DataSourceProperty] public string CancelText { get => _cancelText; set { if (_cancelText != value) { _cancelText = value; OnPropertyChangedWithValue(value, nameof(CancelText)); } } }
        [DataSourceProperty] public string SaveText { get => _saveText; set { if (_saveText != value) { _saveText = value; OnPropertyChangedWithValue(value, nameof(SaveText)); } } }
        [DataSourceProperty] public string SaveAsText { get => _saveAsText; set { if (_saveAsText != value) { _saveAsText = value; OnPropertyChangedWithValue(value, nameof(SaveAsText)); } } }
        [DataSourceProperty] public string SaveAndExitText { get => _saveAndExitText; set { if (_saveAndExitText != value) { _saveAndExitText = value; OnPropertyChangedWithValue(value, nameof(SaveAndExitText)); } } }
        [DataSourceProperty] public string DeleteText { get => _deleteText; set { if (_deleteText != value) { _deleteText = value; OnPropertyChangedWithValue(value, nameof(DeleteText)); } } }
        [DataSourceProperty] public string DuplicateText { get => _duplicateText; set { if (_duplicateText != value) { _duplicateText = value; OnPropertyChangedWithValue(value, nameof(DuplicateText)); } } }

        [DataSourceProperty]
        public MBBindingList<SortProfileItemVM> Profiles
        {
            get => _profiles;
            set
            {
                if (_profiles != value)
                {
                    _profiles = value;
                    OnPropertyChangedWithValue(value, nameof(Profiles));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<SortChipVM> ActiveChips
        {
            get => _activeChips;
            set
            {
                if (_activeChips != value)
                {
                    _activeChips = value;
                    OnPropertyChangedWithValue(value, nameof(ActiveChips));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<SortChipVM> AvailableChips
        {
            get => _availableChips;
            set
            {
                if (_availableChips != value)
                {
                    _availableChips = value;
                    OnPropertyChangedWithValue(value, nameof(AvailableChips));
                }
            }
        }

        [DataSourceProperty]
        public bool CanDelete
        {
            get => _canDelete;
            set
            {
                if (_canDelete != value)
                {
                    _canDelete = value;
                    OnPropertyChangedWithValue(value, nameof(CanDelete));
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
            MBBindingList<SortChipVM> source = ActiveChips.Contains(dragged) ? ActiveChips : AvailableChips;
            if (source == null || target == null)
            {
                return;
            }

            if (source == ActiveChips && source.Count <= 1 && target == AvailableChips)
            {
                return;
            }

            int from = source.IndexOf(dragged);
            if (from < 0)
            {
                return;
            }

            source.RemoveAt(from);
            if (source == target && from < index)
            {
                index--;
            }

            index = Math.Max(0, Math.Min(index, target.Count));
            dragged.ShowDirection = toActive;
            if (!toActive)
            {
                dragged.IsAscending = true;
            }

            target.Insert(index, dragged);
            RefreshActionFlags();
        }

        public void ExecuteSave()
        {
            TryPersist(closeAfter: false);
        }

        public void ExecuteSaveAs()
        {
            PromptSaveAs(CollectActiveKeys(), SortPresets.NextFreeName(), closeAfter: false);
        }

        public void ExecuteSaveAndExit()
        {
            if (TryPersist(closeAfter: true))
            {
                Close();
            }
        }

        public void ExecuteDuplicate()
        {
            List<SortKey> keys = CollectActiveKeys();
            string suggested = SortPresets.NextFreeName();
            if (!EditingDraft && !string.IsNullOrEmpty(_editingSavedName))
            {
                suggested = TruncateCopyName(_editingSavedName);
            }

            PromptSaveAs(keys, suggested, closeAfter: false);
        }

        public void ExecuteDelete()
        {
            if (EditingDraft)
            {
                return;
            }

            string name = _editingSavedName;
            if (!SortPresets.TryDelete(name, out string error))
            {
                InformationManager.DisplayMessage(new InformationMessage(error));
                return;
            }

            PartySortControllerPatches.RefreshPresetItems(_partyVm);
            BeginDraft(new List<SortKey>(TroopSortState.DefaultKeys));
            if (_partyVm?.PartyScreenLogic != null && SortPresets.HasActiveSaved)
            {
                TroopRosterSorter.SortPartyScreen(_partyVm);
            }
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
                    BeginDraft(new List<SortKey>(TroopSortState.DefaultKeys));
                }
                else
                {
                    RebuildProfiles();
                }

                return;
            }

            if (!SortPresets.TryGet(item.Name, out SortPreset preset))
            {
                return;
            }

            // Load for editing only — do not activate on the party screen until Save.
            _editingSavedName = preset.Name;
            RebuildChips(preset.Keys);
            RebuildProfiles();
        }

        internal void OnChipChanged()
        {
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
            if (!SortPresets.TrySaveAs(name, keys, out saved, out error))
            {
                return false;
            }

            _editingSavedName = saved;
            RebuildChips(keys);
            RebuildProfiles();
            ApplyToParty(keys);
            PartySortControllerPatches.RefreshPresetItems(_partyVm);
            return true;
        }

        private void BeginDraft(IReadOnlyList<SortKey> seed)
        {
            _editingSavedName = string.Empty;
            RebuildChips(seed);
            RebuildProfiles();
        }

        private void RebuildProfiles()
        {
            var list = new MBBindingList<SortProfileItemVM>();
            list.Add(new SortProfileItemVM(this, SortPresets.NewCustomName, isDraft: true)
            {
                IsSelected = EditingDraft
            });

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
                active.Add(new SortChipVM(this, key.Mode, key.Ascending, showDirection: true));
                used.Add(key.Mode);
            }

            if (active.Count == 0)
            {
                active.Add(new SortChipVM(this, TroopSortMode.Type, ascending: false, showDirection: true));
                used.Add(TroopSortMode.Type);
            }

            var available = new MBBindingList<SortChipVM>();
            foreach (TroopSortMode mode in TroopSortState.AllModes())
            {
                if (!used.Contains(mode))
                {
                    available.Add(new SortChipVM(this, mode, ascending: true, showDirection: false));
                }
            }

            ActiveChips = active;
            AvailableChips = available;
        }

        private List<SortKey> CollectActiveKeys()
        {
            return ActiveChips?.Select(c => c.ToKey()).ToList()
                   ?? new List<SortKey>(TroopSortState.DefaultKeys);
        }

        private void RefreshActionFlags()
        {
            CanDelete = !EditingDraft;
        }

        private void ApplyToParty(IReadOnlyList<SortKey> keys)
        {
            TroopSortState.SetKeys(keys);
            if (_partyVm?.PartyScreenLogic != null)
            {
                TroopRosterSorter.SortPartyScreen(_partyVm, keys);
            }

            PartySortControllerPatches.SelectActivePreset(_partyVm);
        }

        private void RestoreSnapshot()
        {
            TroopSortState.SetKeys(_openSnapshotKeys);
            if (!string.IsNullOrEmpty(_openSnapshotActiveName) && SortPresets.Activate(_openSnapshotActiveName))
            {
                PartySortControllerPatches.SelectActivePreset(_partyVm);
                if (_partyVm?.PartyScreenLogic != null)
                {
                    TroopRosterSorter.SortPartyScreen(_partyVm);
                }
            }
            else
            {
                SortPresets.ClearActive();
                PartySortControllerPatches.RefreshPresetItems(_partyVm);
            }
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
