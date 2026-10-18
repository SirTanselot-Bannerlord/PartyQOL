using PartyQOL.Filtering;
using PartyQOL.UI.Popups;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    public sealed class FilterChipVM : ViewModel
    {
        private readonly FilterSettingsVM _owner;
        private string _title;
        private string _summaryText;
        private bool _isSelected;

        public FilterCriterionKind Kind { get; }

        public FilterChipVM(FilterSettingsVM owner, FilterCriterionKind kind, string summary)
        {
            _owner = owner;
            Kind = kind;
            _title = kind switch
            {
                FilterCriterionKind.Type => new TextObject("{=pqol_chip_type}Type").ToString(),
                FilterCriterionKind.Culture => new TextObject("{=pqol_chip_culture}Culture").ToString(),
                FilterCriterionKind.Tier => new TextObject("{=pqol_chip_tier}Tier").ToString(),
                FilterCriterionKind.MaxTier => new TextObject("{=pqol_chip_max_tier}Max Tier").ToString(),
                FilterCriterionKind.Level => new TextObject("{=pqol_chip_level}Level").ToString(),
                FilterCriterionKind.Count => new TextObject("{=pqol_chip_count}Count").ToString(),
                FilterCriterionKind.Condition => new TextObject("{=pqol_chip_wounded}Wounded").ToString(),
                _ => kind.ToString()
            };
            _summaryText = summary ?? string.Empty;
        }

        [DataSourceProperty]
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChangedWithValue(value, nameof(Title));
                }
            }
        }

        [DataSourceProperty]
        public string SummaryText
        {
            get => _summaryText;
            set
            {
                if (_summaryText != value)
                {
                    _summaryText = value;
                    OnPropertyChangedWithValue(value, nameof(SummaryText));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                }
            }
        }

        public void ExecuteSelect()
        {
            _owner?.SelectCriterion(this);
        }
    }
}
