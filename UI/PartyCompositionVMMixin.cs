using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using PartyQOL.Filtering;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;

namespace PartyQOL.UI
{
    [ViewModelMixin]
    public sealed class PartyCompositionVMMixin : BaseViewModelMixin<PartyCompositionVM>
    {
        private bool _isInfantryCountFiltered;
        private bool _isRangedCountFiltered;
        private bool _isCavalryCountFiltered;
        private bool _isHorseArcherCountFiltered;
        private bool _isInfantryExcluded;
        private bool _isRangedExcluded;
        private bool _isCavalryExcluded;
        private bool _isHorseArcherExcluded;

        public PartyCompositionVMMixin(PartyCompositionVM vm)
            : base(vm)
        {
            PartyCompositionFilterPatches.Register(vm, this);
            SetCountColors(false, false, false, false);
        }

        [DataSourceProperty]
        public bool IsInfantryCountFiltered
        {
            get => _isInfantryCountFiltered;
            set
            {
                if (_isInfantryCountFiltered != value)
                {
                    _isInfantryCountFiltered = value;
                    OnPropertyChangedWithValue(value, nameof(IsInfantryCountFiltered));
                }
            }
        }

        [DataSourceProperty]
        public bool IsRangedCountFiltered
        {
            get => _isRangedCountFiltered;
            set
            {
                if (_isRangedCountFiltered != value)
                {
                    _isRangedCountFiltered = value;
                    OnPropertyChangedWithValue(value, nameof(IsRangedCountFiltered));
                }
            }
        }

        [DataSourceProperty]
        public bool IsCavalryCountFiltered
        {
            get => _isCavalryCountFiltered;
            set
            {
                if (_isCavalryCountFiltered != value)
                {
                    _isCavalryCountFiltered = value;
                    OnPropertyChangedWithValue(value, nameof(IsCavalryCountFiltered));
                }
            }
        }

        [DataSourceProperty]
        public bool IsHorseArcherCountFiltered
        {
            get => _isHorseArcherCountFiltered;
            set
            {
                if (_isHorseArcherCountFiltered != value)
                {
                    _isHorseArcherCountFiltered = value;
                    OnPropertyChangedWithValue(value, nameof(IsHorseArcherCountFiltered));
                }
            }
        }

        [DataSourceProperty]
        public bool IsInfantryExcluded
        {
            get => _isInfantryExcluded;
            set
            {
                if (_isInfantryExcluded != value)
                {
                    _isInfantryExcluded = value;
                    OnPropertyChangedWithValue(value, nameof(IsInfantryExcluded));
                }
            }
        }

        [DataSourceProperty]
        public bool IsRangedExcluded
        {
            get => _isRangedExcluded;
            set
            {
                if (_isRangedExcluded != value)
                {
                    _isRangedExcluded = value;
                    OnPropertyChangedWithValue(value, nameof(IsRangedExcluded));
                }
            }
        }

        [DataSourceProperty]
        public bool IsCavalryExcluded
        {
            get => _isCavalryExcluded;
            set
            {
                if (_isCavalryExcluded != value)
                {
                    _isCavalryExcluded = value;
                    OnPropertyChangedWithValue(value, nameof(IsCavalryExcluded));
                }
            }
        }

        [DataSourceProperty]
        public bool IsHorseArcherExcluded
        {
            get => _isHorseArcherExcluded;
            set
            {
                if (_isHorseArcherExcluded != value)
                {
                    _isHorseArcherExcluded = value;
                    OnPropertyChangedWithValue(value, nameof(IsHorseArcherExcluded));
                }
            }
        }

        [DataSourceMethod]
        public void ExecuteToggleInfantry() => ApplyFormation(FilterState.ToggleFormation, "Infantry");

        [DataSourceMethod]
        public void ExecuteToggleRanged() => ApplyFormation(FilterState.ToggleFormation, "Ranged");

        [DataSourceMethod]
        public void ExecuteToggleCavalry() => ApplyFormation(FilterState.ToggleFormation, "Cavalry");

        [DataSourceMethod]
        public void ExecuteToggleHorseArcher() => ApplyFormation(FilterState.ToggleFormation, "HorseArcher");

        [DataSourceMethod]
        public void ExecuteIsolateInfantry() => ApplyFormation(FilterState.IsolateFormation, "Infantry");

        [DataSourceMethod]
        public void ExecuteIsolateRanged() => ApplyFormation(FilterState.IsolateFormation, "Ranged");

        [DataSourceMethod]
        public void ExecuteIsolateCavalry() => ApplyFormation(FilterState.IsolateFormation, "Cavalry");

        [DataSourceMethod]
        public void ExecuteIsolateHorseArcher() => ApplyFormation(FilterState.IsolateFormation, "HorseArcher");

        internal void SetExcluded(bool infantry, bool ranged, bool cavalry, bool horseArcher)
        {
            _isInfantryExcluded = infantry;
            _isRangedExcluded = ranged;
            _isCavalryExcluded = cavalry;
            _isHorseArcherExcluded = horseArcher;
            OnPropertyChangedWithValue(infantry, nameof(IsInfantryExcluded));
            OnPropertyChangedWithValue(ranged, nameof(IsRangedExcluded));
            OnPropertyChangedWithValue(cavalry, nameof(IsCavalryExcluded));
            OnPropertyChangedWithValue(horseArcher, nameof(IsHorseArcherExcluded));
        }

        private static void ApplyFormation(System.Action<string> mutate, string typeId)
        {
            mutate(typeId);
            if (PartyVMMixin.Current?.Party != null)
            {
                TroopFilterApplicator.ApplyPartyScreen(PartyVMMixin.Current.Party);
            }
        }

        internal void SetCountColors(bool infChanged, bool rngChanged, bool cavChanged, bool haChanged)
        {
            // Always notify — Gauntlet may miss a no-op set when re-applying the same filter.
            _isInfantryCountFiltered = infChanged;
            _isRangedCountFiltered = rngChanged;
            _isCavalryCountFiltered = cavChanged;
            _isHorseArcherCountFiltered = haChanged;
            OnPropertyChangedWithValue(infChanged, nameof(IsInfantryCountFiltered));
            OnPropertyChangedWithValue(rngChanged, nameof(IsRangedCountFiltered));
            OnPropertyChangedWithValue(cavChanged, nameof(IsCavalryCountFiltered));
            OnPropertyChangedWithValue(haChanged, nameof(IsHorseArcherCountFiltered));
        }
    }
}
