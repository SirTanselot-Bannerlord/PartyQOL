using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using PartyQOL.Filtering;
using PartyQOL.Sorting;
using PartyQOL.UI.Popups;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;

namespace PartyQOL.UI
{
    [ViewModelMixin]
    public sealed class PartyVMMixin : BaseViewModelMixin<PartyVM>
    {
        private static readonly string[] SettingsIconCategories = { "ui_mplobby" };

        private static bool _reopening;
        private static bool _preserveFilterOnNextOpen;
        private static bool _reopenQueued;
        private static PartySwitchSide _queuedSide;
        private static MobileParty _queuedLeft;
        private static MobileParty _queuedRight;

        private readonly List<SpriteCategory> _loadedCategories = new List<SpriteCategory>();
        private SelectorVM<PartySelectItemVM> _leftPartySelector;
        private SelectorVM<PartySelectItemVM> _rightPartySelector;
        private SelectorVM<SortPresetItemVM> _sortPresetSelector;
        private SelectorVM<FilterPresetItemVM> _filterPresetSelector;
        private HintViewModel _sortSettingsHint;
        private HintViewModel _filterSettingsHint;
        private bool _suppressLeftSelection;
        private bool _suppressRightSelection;
        private bool _acceptPartySelection;
        private bool _suppressSortSelect;
        private bool _suppressFilterSelect;
        private bool _isLeftPartySelectorVisible;
        private bool _isRightPartySelectorVisible;
        private bool _isSortAscending;
        private bool _isPartyQolFilterActive;
        private bool _isMainPartyTroopsLblFiltered;
        private bool _isOtherPartyTroopsLblFiltered;

        public static PartyVMMixin Current { get; private set; }

        public PartyVM Party => ViewModel;

        /// <summary>Side switches reopen the screen. Keep the live filter; a fresh open still clears it.</summary>
        internal static void PreserveFilterOnNextOpen()
        {
            _preserveFilterOnNextOpen = true;
        }

        public PartyVMMixin(PartyVM vm)
            : base(vm)
        {
            Current = this;
            _sortSettingsHint = new HintViewModel(new TextObject("{=pqol_sort_btn_hint}Customize troop sort order"));
            _filterSettingsHint = new HintViewModel(new TextObject("{=pqol_filter_btn_hint}Customize troop filters"));
            LoadSettingsIconAtlas();
            PartyQolSettings.EnsureLoaded();
            SortPresets.EnsureLoadedPublic();
            FilterPresets.EnsureLoadedPublic();
            // Fresh open starts with no filter. A side switch sets the preserve flag first.
            if (_preserveFilterOnNextOpen)
            {
                _preserveFilterOnNextOpen = false;
            }
            else
            {
                FilterPresets.ClearActive();
            }
            BuildPartySelectors();
            RefreshSortFilterSelectors();
            ApplyCurrentSort();
            SyncFilterActiveFlag();
            TroopFilterApplicator.ApplyPartyScreen(vm);
        }

        /// <summary>
        /// Side switches swap rosters on the open screen, one tick after the dropdown click.
        /// Popping the party state flashes the map (often black) and the new screen then
        /// dies in native UI.
        /// </summary>
        internal static void TickQueuedReopen()
        {
            if (_reopening)
            {
                return;
            }

            if (_reopenQueued)
            {
                _reopenQueued = false;
                MobileParty left = _queuedLeft;
                MobileParty right = _queuedRight;
                PartySwitchSide side = _queuedSide;
                _queuedLeft = null;
                _queuedRight = null;
                _reopening = true;
                try
                {
                    ApplyQueuedSwitch(left, right, side);
                }
                catch (Exception ex)
                {
                    PartyQolLog.Write("switch failed", ex);
                }
                finally
                {
                    _reopening = false;
                }

                return;
            }

            if (Current != null)
            {
                Current._acceptPartySelection = true;
            }
        }

        public override void OnFinalize()
        {
            PopupHost.CloseAll();
            foreach (SpriteCategory category in _loadedCategories)
            {
                category.Unload();
            }

            _loadedCategories.Clear();
            if (Current == this)
            {
                Current = null;
            }

            base.OnFinalize();
        }

        [DataSourceProperty]
        public SelectorVM<PartySelectItemVM> LeftPartySelector
        {
            get => _leftPartySelector;
            set
            {
                if (_leftPartySelector != value)
                {
                    _leftPartySelector = value;
                    OnPropertyChangedWithValue(value, nameof(LeftPartySelector));
                }
            }
        }

        [DataSourceProperty]
        public SelectorVM<PartySelectItemVM> RightPartySelector
        {
            get => _rightPartySelector;
            set
            {
                if (_rightPartySelector != value)
                {
                    _rightPartySelector = value;
                    OnPropertyChangedWithValue(value, nameof(RightPartySelector));
                }
            }
        }

        [DataSourceProperty]
        public SelectorVM<SortPresetItemVM> SortPresetSelector
        {
            get => _sortPresetSelector;
            set
            {
                if (_sortPresetSelector != value)
                {
                    _sortPresetSelector = value;
                    OnPropertyChangedWithValue(value, nameof(SortPresetSelector));
                }
            }
        }

        [DataSourceProperty]
        public SelectorVM<FilterPresetItemVM> FilterPresetSelector
        {
            get => _filterPresetSelector;
            set
            {
                if (_filterPresetSelector != value)
                {
                    _filterPresetSelector = value;
                    OnPropertyChangedWithValue(value, nameof(FilterPresetSelector));
                }
            }
        }

        [DataSourceProperty]
        public bool IsLeftPartySelectorVisible
        {
            get => _isLeftPartySelectorVisible;
            set
            {
                if (_isLeftPartySelectorVisible != value)
                {
                    _isLeftPartySelectorVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsLeftPartySelectorVisible));
                }
            }
        }

        [DataSourceProperty]
        public bool IsRightPartySelectorVisible
        {
            get => _isRightPartySelectorVisible;
            set
            {
                if (_isRightPartySelectorVisible != value)
                {
                    _isRightPartySelectorVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsRightPartySelectorVisible));
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel SortSettingsHint
        {
            get => _sortSettingsHint;
            set
            {
                if (_sortSettingsHint != value)
                {
                    _sortSettingsHint = value;
                    OnPropertyChangedWithValue(value, nameof(SortSettingsHint));
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel FilterSettingsHint
        {
            get => _filterSettingsHint;
            set
            {
                if (_filterSettingsHint != value)
                {
                    _filterSettingsHint = value;
                    OnPropertyChangedWithValue(value, nameof(FilterSettingsHint));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSortAscending
        {
            get => _isSortAscending;
            set
            {
                if (_isSortAscending != value)
                {
                    _isSortAscending = value;
                    OnPropertyChangedWithValue(value, nameof(IsSortAscending));
                }
            }
        }

        [DataSourceProperty]
        public bool IsPartyQolFilterActive
        {
            get => _isPartyQolFilterActive;
            set
            {
                if (_isPartyQolFilterActive != value)
                {
                    _isPartyQolFilterActive = value;
                    OnPropertyChangedWithValue(value, nameof(IsPartyQolFilterActive));
                }
            }
        }

        [DataSourceProperty]
        public bool IsMainPartyTroopsLblFiltered
        {
            get => _isMainPartyTroopsLblFiltered;
            set
            {
                if (_isMainPartyTroopsLblFiltered != value)
                {
                    _isMainPartyTroopsLblFiltered = value;
                    OnPropertyChangedWithValue(value, nameof(IsMainPartyTroopsLblFiltered));
                }
            }
        }

        [DataSourceProperty]
        public bool IsOtherPartyTroopsLblFiltered
        {
            get => _isOtherPartyTroopsLblFiltered;
            set
            {
                if (_isOtherPartyTroopsLblFiltered != value)
                {
                    _isOtherPartyTroopsLblFiltered = value;
                    OnPropertyChangedWithValue(value, nameof(IsOtherPartyTroopsLblFiltered));
                }
            }
        }

        public override void OnRefresh()
        {
            base.OnRefresh();
            IsPartyQolFilterActive = FilterState.HasActive;
            // Re-apply filter visuals after vanilla refresh (labels, composition, row hide).
            if (ViewModel != null)
            {
                TroopFilterApplicator.ApplyPartyScreen(ViewModel);
            }
        }

        internal void SyncFilterActiveFlag(bool mainTroopsChanged = false, bool otherTroopsChanged = false)
        {
            IsPartyQolFilterActive = FilterState.HasActive;
            // Red only on the side whose headcount actually dropped — never mutate shared brushes.
            IsMainPartyTroopsLblFiltered = FilterState.HasActive && mainTroopsChanged;
            IsOtherPartyTroopsLblFiltered = FilterState.HasActive && otherTroopsChanged;
        }

        [DataSourceMethod]
        public void ExecuteOpenSortSettings()
        {
            SortSettingsVM.Open(ViewModel);
        }

        [DataSourceMethod]
        public void ExecuteOpenFilterSettings()
        {
            FilterSettingsVM.Open(ViewModel);
        }

        [DataSourceMethod]
        public void ExecuteToggleSortOrder()
        {
            if (!SortPresets.HasActiveSaved || TroopSortState.Keys.Count == 0)
            {
                return;
            }

            var keys = TroopSortState.Keys.Select(k => k).ToList();
            SortKey first = keys[0];
            keys[0] = first.WithAscending(!first.Ascending);
            TroopSortState.SetKeys(keys);
            if (SortPresets.TryGet(SortPresets.ActiveName, out SortPreset preset))
            {
                preset.Keys = keys;
            }

            IsSortAscending = keys[0].Ascending;
            TroopRosterSorter.SortPartyScreen(ViewModel, keys);
            TroopFilterApplicator.ApplyPartyScreen(ViewModel);
        }

        internal void RefreshSortFilterSelectors()
        {
            RebuildSortSelector();
            RebuildFilterSelector();
            SyncSortDirectionFromState();
        }

        private void SyncSortDirectionFromState()
        {
            IsSortAscending = TroopSortState.Keys.Count > 0 && TroopSortState.Keys[0].Ascending;
        }

        private void RebuildSortSelector()
        {
            SortPresets.EnsureLoadedPublic();
            var items = new List<SortPresetItemVM>();
            int selected = 0;
            string active = SortPresets.ActiveName;
            if (SortPresets.All.Count == 0)
            {
                items.Add(new SortPresetItemVM(new TextObject("{=pqol_sort_none}Sort...").ToString()));
            }
            else
            {
                for (int i = 0; i < SortPresets.All.Count; i++)
                {
                    SortPreset preset = SortPresets.All[i];
                    items.Add(new SortPresetItemVM(preset.Name));
                    if (string.Equals(preset.Name, active, StringComparison.OrdinalIgnoreCase))
                    {
                        selected = i;
                    }
                }
            }

            _suppressSortSelect = true;
            var selector = new SelectorVM<SortPresetItemVM>(-1, OnSortPresetChanged);
            selector.Refresh(items, selected, OnSortPresetChanged);
            SortPresetSelector = selector;
            _suppressSortSelect = false;
        }

        private void RebuildFilterSelector()
        {
            FilterPresets.EnsureLoadedPublic();
            var items = new List<FilterPresetItemVM>
            {
                new FilterPresetItemVM(new TextObject("{=pqol_filter_none}No filter").ToString(), true)
            };
            int selected = 0;
            string active = FilterPresets.ActiveName;
            for (int i = 0; i < FilterPresets.All.Count; i++)
            {
                FilterPreset preset = FilterPresets.All[i];
                items.Add(new FilterPresetItemVM(preset.Name));
                if (string.Equals(preset.Name, active, StringComparison.OrdinalIgnoreCase))
                {
                    selected = i + 1;
                }
            }

            _suppressFilterSelect = true;
            var selector = new SelectorVM<FilterPresetItemVM>(-1, OnFilterPresetChanged);
            selector.Refresh(items, selected, OnFilterPresetChanged);
            FilterPresetSelector = selector;
            _suppressFilterSelect = false;
        }

        private void OnSortPresetChanged(SelectorVM<SortPresetItemVM> selector)
        {
            if (_suppressSortSelect || selector?.SelectedItem == null)
            {
                return;
            }

            string name = selector.SelectedItem.PresetName;
            if (string.IsNullOrEmpty(name) || !SortPresets.Activate(name))
            {
                return;
            }

            TroopRosterSorter.SortPartyScreen(ViewModel);
            TroopFilterApplicator.ApplyPartyScreen(ViewModel);
        }

        private void OnFilterPresetChanged(SelectorVM<FilterPresetItemVM> selector)
        {
            if (_suppressFilterSelect || selector?.SelectedItem == null)
            {
                return;
            }

            FilterPresetItemVM item = selector.SelectedItem;
            if (item.IsNone)
            {
                FilterPresets.ClearActive();
            }
            else if (!FilterPresets.Activate(item.PresetName))
            {
                return;
            }

            TroopFilterApplicator.ApplyPartyScreen(ViewModel);
        }

        private void BuildPartySelectors()
        {
            PartyVM vm = ViewModel;
            if (vm?.PartyScreenLogic == null)
            {
                IsLeftPartySelectorVisible = false;
                IsRightPartySelectorVisible = false;
                return;
            }

            MobileParty right = vm.PartyScreenLogic.RightOwnerParty?.MobileParty;
            MobileParty left = vm.PartyScreenLogic.LeftOwnerParty?.MobileParty;

            // Stale companion on either side (owned war party that left vicinity): reset that side.
            if (left != null
                && AbsorbPartyService.IsManageableOwnedParty(left)
                && !AbsorbPartyService.IsInPlayerVicinity(left))
            {
                TryReopen(null, AbsorbPartyService.IsTransferSideParty(right) ? right : MobileParty.MainParty, PartySwitchSide.Both);
                return;
            }

            if (right != null
                && AbsorbPartyService.IsManageableOwnedParty(right)
                && !AbsorbPartyService.IsInPlayerVicinity(right))
            {
                TryReopen(AbsorbPartyService.IsTransferSideParty(left) ? left : null, MobileParty.MainParty, PartySwitchSide.Both);
                return;
            }

            bool customizable = AbsorbPartyService.IsTransferSideParty(right)
                                && (left == null || AbsorbPartyService.IsTransferSideParty(left));
            IsLeftPartySelectorVisible = customizable;
            IsRightPartySelectorVisible = customizable;
            if (!customizable)
            {
                return;
            }

            BuildLeftSelector(left, right);
            BuildRightSelector(left, right);
        }

        private void BuildLeftSelector(MobileParty currentLeft, MobileParty currentRight)
        {
            var items = new List<PartySelectItemVM>
            {
                new PartySelectItemVM(AbsorbPartyService.GetTransferPoolLabel(null), null)
            };

            int selected = 0;
            MobileParty[] parties = AbsorbPartyService.GetTransferSideChoices(exclude: currentRight, includeMainParty: true);
            for (int i = 0; i < parties.Length; i++)
            {
                MobileParty party = parties[i];
                items.Add(new PartySelectItemVM(AbsorbPartyService.GetTransferPoolLabel(party), party));
                if (party == currentLeft)
                {
                    selected = i + 1;
                }
            }

            _suppressLeftSelection = true;
            var selector = new SelectorVM<PartySelectItemVM>(-1, OnLeftPartyChanged);
            selector.Refresh(items, selected, OnLeftPartyChanged);
            LeftPartySelector = selector;
            _suppressLeftSelection = false;
        }

        private void BuildRightSelector(MobileParty currentLeft, MobileParty currentRight)
        {
            var items = new List<PartySelectItemVM>();
            int selected = 0;
            MobileParty[] parties = AbsorbPartyService.GetTransferSideChoices(exclude: currentLeft, includeMainParty: true);
            for (int i = 0; i < parties.Length; i++)
            {
                MobileParty party = parties[i];
                items.Add(new PartySelectItemVM(AbsorbPartyService.GetTransferPoolLabel(party), party));
                if (party == currentRight)
                {
                    selected = i;
                }
            }

            if (items.Count == 0)
            {
                IsRightPartySelectorVisible = false;
                return;
            }

            _suppressRightSelection = true;
            var selector = new SelectorVM<PartySelectItemVM>(-1, OnRightPartyChanged);
            selector.Refresh(items, selected, OnRightPartyChanged);
            RightPartySelector = selector;
            _suppressRightSelection = false;
        }

        private void OnLeftPartyChanged(SelectorVM<PartySelectItemVM> selector)
        {
            if (!_acceptPartySelection || _suppressLeftSelection || _reopening || _reopenQueued || selector?.SelectedItem == null)
            {
                return;
            }

            PartyVM vm = ViewModel;
            MobileParty right = vm?.PartyScreenLogic?.RightOwnerParty?.MobileParty ?? MobileParty.MainParty;
            MobileParty currentLeft = vm?.PartyScreenLogic?.LeftOwnerParty?.MobileParty;
            PartySelectItemVM item = selector.SelectedItem;

            if (item.IsDismiss && currentLeft == null)
            {
                return;
            }

            if (!item.IsDismiss && item.Party == currentLeft)
            {
                return;
            }

            TryReopen(item.Party, right, PartySwitchSide.Left);
        }

        private void OnRightPartyChanged(SelectorVM<PartySelectItemVM> selector)
        {
            if (!_acceptPartySelection || _suppressRightSelection || _reopening || _reopenQueued || selector?.SelectedItem == null)
            {
                return;
            }

            PartyVM vm = ViewModel;
            MobileParty left = vm?.PartyScreenLogic?.LeftOwnerParty?.MobileParty;
            MobileParty currentRight = vm?.PartyScreenLogic?.RightOwnerParty?.MobileParty;
            MobileParty nextRight = selector.SelectedItem.Party;
            if (nextRight == null || nextRight == currentRight)
            {
                return;
            }

            // Keep left if still valid opposite the new right; otherwise dismiss.
            // Garrison: re-resolve (ensure) — empty garrisons can be culled on cancel-reopen.
            MobileParty nextLeft;
            if (left != null && left.IsGarrison)
            {
                nextLeft = AbsorbPartyService.GetManageableGarrisonAtPlayerLocation();
            }
            else if (left != null && left != nextRight)
            {
                nextLeft = left;
            }
            else
            {
                nextLeft = null;
            }

            TryReopen(nextLeft, nextRight, PartySwitchSide.Right);
        }

        private void TryReopen(MobileParty leftParty, MobileParty rightParty, PartySwitchSide side)
        {
            if (_reopening || _reopenQueued)
            {
                return;
            }

            _queuedLeft = leftParty;
            _queuedRight = rightParty;
            _queuedSide = side;
            _reopenQueued = true;
        }

        private static void ApplyQueuedSwitch(MobileParty left, MobileParty right, PartySwitchSide side)
        {
            PartyVM vm = Current?.Party;
            if (vm?.PartyScreenLogic == null || Helpers.PartyScreenHelper.GetActivePartyState() == null)
            {
                PartyQolLog.Write("switch opened a new screen (no live party VM)");
                PairPartyScreen.Open(left, right);
                return;
            }

            if (!PartyScreenRetarget.Apply(vm, left, right))
            {
                return;
            }

            MobileParty nowLeft = vm.PartyScreenLogic.LeftOwnerParty?.MobileParty;
            MobileParty nowRight = vm.PartyScreenLogic.RightOwnerParty?.MobileParty;
            if (side != PartySwitchSide.Left)
            {
                Current.BuildLeftSelector(nowLeft, nowRight);
            }

            if (side != PartySwitchSide.Right)
            {
                Current.BuildRightSelector(nowLeft, nowRight);
            }

            Current.ApplyCurrentSort();
            TroopFilterApplicator.ApplyPartyScreen(vm);
        }

        private enum PartySwitchSide
        {
            Left,
            Right,
            Both
        }

        private void ApplyCurrentSort()
        {
            PartyVM vm = ViewModel;
            if (vm?.PartyScreenLogic == null || !SortPresets.HasActiveSaved)
            {
                return;
            }

            TroopRosterSorter.SortPartyScreen(vm);
        }

        private void LoadSettingsIconAtlas()
        {
            foreach (string name in SettingsIconCategories)
            {
                SpriteCategory category = UIResourceManager.GetSpriteCategory(name);
                if (category != null && !category.IsLoaded)
                {
                    category.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
                    _loadedCategories.Add(category);
                }
            }
        }
    }
}
