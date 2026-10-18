using PartyQOL.UI.Popups;
using TaleWorlds.Library;

namespace PartyQOL.UI
{
    public sealed class FilterProfileItemVM : ViewModel
    {
        private readonly FilterSettingsVM _owner;
        private string _name;
        private bool _isSelected;
        private bool _isDraft;

        public FilterProfileItemVM(FilterSettingsVM owner, string name, bool isDraft)
        {
            _owner = owner;
            _name = name ?? string.Empty;
            _isDraft = isDraft;
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, nameof(Name));
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

        [DataSourceProperty]
        public bool IsDraft
        {
            get => _isDraft;
            set
            {
                if (_isDraft != value)
                {
                    _isDraft = value;
                    OnPropertyChangedWithValue(value, nameof(IsDraft));
                }
            }
        }

        public void ExecuteSelect()
        {
            _owner?.SelectProfile(this);
        }
    }
}
