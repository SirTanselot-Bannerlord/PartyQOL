using PartyQOL.UI.Popups;
using TaleWorlds.Library;

namespace PartyQOL.UI
{
    public sealed class FilterToggleChipVM : ViewModel
    {
        private readonly FilterSettingsVM _owner;
        private string _label;
        private bool _isEnabled;
        private bool _isSelected;

        public string Id { get; }

        public FilterToggleChipVM(FilterSettingsVM owner, string id, string label, bool enabled)
        {
            _owner = owner;
            Id = id;
            _label = label ?? id;
            _isEnabled = enabled;
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
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChangedWithValue(value, nameof(IsEnabled));
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

        public void ExecuteToggle()
        {
            IsEnabled = !IsEnabled;
            _owner?.OnDetailChanged();
        }
    }
}
