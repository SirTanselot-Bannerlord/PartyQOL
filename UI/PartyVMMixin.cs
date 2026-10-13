using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
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
        /// <summary>
        /// settings_icon lives in the MP lobby atlas, which campaign never loads. Borrow it while the party screen is open
        /// (same approach CAE uses for ui_partyscreen icons on inventory).
        /// </summary>
        private static readonly string[] SettingsIconCategories = { "ui_mplobby" };

        private static bool _reopening;

        private readonly List<SpriteCategory> _loadedCategories = new List<SpriteCategory>();
        private SelectorVM<PartySelectItemVM> _leftPartySelector;
        private HintViewModel _sortSettingsHint;
        private bool _suppressSelection;
        private bool _isLeftPartySelectorVisible;
        private string _sortSettingsButtonText;

        public static PartyVMMixin Current { get; private set; }

        public PartyVM Party => ViewModel;

        public PartyVMMixin(PartyVM vm)
            : base(vm)
        {
            Current = this;
            _sortSettingsButtonText = new TextObject("{=pqol_sort_btn}Sort").ToString();
            _sortSettingsHint = new HintViewModel(new TextObject("{=pqol_sort_btn_hint}Customize troop sort order"));
            LoadSettingsIconAtlas();
            SortPresets.EnsureLoadedPublic();
            BuildPartySelector();
            PartySortControllerPatches.RefreshPresetItems(vm);
            ApplyCurrentSort();
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
        public string SortSettingsButtonText
        {
            get => _sortSettingsButtonText;
            set
            {
                if (_sortSettingsButtonText != value)
                {
                    _sortSettingsButtonText = value;
                    OnPropertyChangedWithValue(value, nameof(SortSettingsButtonText));
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

        // UIExtenderEx v2 only exposes mixin commands marked [DataSourceMethod].
        [DataSourceMethod]
        public void ExecuteOpenSortSettings()
        {
            SortSettingsVM.Open(ViewModel);
        }

        private void BuildPartySelector()
        {
            PartyVM vm = ViewModel;
            if (vm?.PartyScreenLogic == null)
            {
                IsLeftPartySelectorVisible = false;
                return;
            }

            PartyBase rightOwner = vm.PartyScreenLogic.RightOwnerParty;
            MobileParty right = rightOwner?.MobileParty;
            IsLeftPartySelectorVisible = right == MobileParty.MainParty
                                         || rightOwner == PartyBase.MainParty;
            if (!IsLeftPartySelectorVisible)
            {
                return;
            }

            MobileParty currentLeft = vm.PartyScreenLogic.LeftOwnerParty?.MobileParty;
            if (currentLeft != null && !AbsorbPartyService.IsSelectableForPartyScreen(currentLeft))
            {
                if (!_reopening)
                {
                    _reopening = true;
                    try
                    {
                        PairPartyScreen.ReopenWithLeft(null, right);
                    }
                    finally
                    {
                        _reopening = false;
                    }
                }

                return;
            }

            var items = new List<PartySelectItemVM>
            {
                new PartySelectItemVM(new TextObject("{=pqol_dismiss}Dismiss"), null)
            };

            int selected = 0;
            MobileParty[] parties = AbsorbPartyService.GetSelectablePartiesForPartyScreen();
            for (int i = 0; i < parties.Length; i++)
            {
                MobileParty party = parties[i];
                items.Add(new PartySelectItemVM(party.Name, party));
                if (party == currentLeft)
                {
                    selected = i + 1;
                }
            }

            _suppressSelection = true;
            var selector = new SelectorVM<PartySelectItemVM>(-1, OnLeftPartyChanged);
            selector.Refresh(items, selected, OnLeftPartyChanged);
            LeftPartySelector = selector;
            _suppressSelection = false;
        }

        private void OnLeftPartyChanged(SelectorVM<PartySelectItemVM> selector)
        {
            if (_suppressSelection || _reopening || selector?.SelectedItem == null)
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

            _reopening = true;
            try
            {
                PairPartyScreen.ReopenWithLeft(item.Party, right);
            }
            finally
            {
                _reopening = false;
            }
        }

        private void ApplyCurrentSort()
        {
            PartyVM vm = ViewModel;
            if (vm?.PartyScreenLogic == null)
            {
                return;
            }

            // Only re-apply multi-key sort when a PartyQOL preset (TroopSortType.Custom) is selected.
            PartySortControllerVM controller = vm.MainPartySortController;
            if (controller?.SortOptions?.SelectedItem?.SortType == PartyScreenLogic.TroopSortType.Custom)
            {
                TroopRosterSorter.SortPartyScreen(vm);
            }
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
