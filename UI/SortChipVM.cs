using PartyQOL.Sorting;
using PartyQOL.UI.Popups;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    public sealed class SortChipVM : ViewModel
    {
        private readonly SortSettingsVM _owner;
        private string _label;
        private bool _ascending = true;
        private bool _showDirection;

        public TroopSortMode Mode { get; }

        public SortChipVM(SortSettingsVM owner, TroopSortMode mode, bool ascending, bool showDirection)
        {
            _owner = owner;
            Mode = mode;
            _ascending = ascending;
            _showDirection = showDirection;
            RefreshLabel();
        }

        [DataSourceProperty]
        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value;
                    OnPropertyChangedWithValue(value, nameof(Label));
                }
            }
        }

        [DataSourceProperty]
        public bool IsAscending
        {
            get => _ascending;
            set
            {
                if (_ascending != value)
                {
                    _ascending = value;
                    OnPropertyChangedWithValue(value, nameof(IsAscending));
                }
            }
        }

        [DataSourceProperty]
        public bool ShowDirection
        {
            get => _showDirection;
            set
            {
                if (_showDirection != value)
                {
                    _showDirection = value;
                    OnPropertyChangedWithValue(value, nameof(ShowDirection));
                }
            }
        }

        public SortKey ToKey() => new SortKey(Mode, IsAscending);

        public void ExecuteToggleOrder()
        {
            if (!ShowDirection)
            {
                return;
            }

            IsAscending = !IsAscending;
            _owner?.OnChipChanged();
        }

        private void RefreshLabel()
        {
            Label = Mode switch
            {
                TroopSortMode.Type => new TextObject("{=pqol_chip_type}Type").ToString(),
                TroopSortMode.Tier => new TextObject("{=pqol_chip_tier}Tier").ToString(),
                TroopSortMode.Level => new TextObject("{=pqol_chip_level}Level").ToString(),
                TroopSortMode.Culture => new TextObject("{=pqol_chip_culture}Culture").ToString(),
                TroopSortMode.Name => new TextObject("{=pqol_chip_name}Name").ToString(),
                TroopSortMode.Count => new TextObject("{=pqol_chip_count}Count").ToString(),
                _ => Mode.ToString()
            };
        }
    }
}
