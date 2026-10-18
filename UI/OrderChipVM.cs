using PartyQOL.UI.Popups;
using TaleWorlds.Library;

namespace PartyQOL.UI
{
    public sealed class OrderChipVM : ViewModel
    {
        private readonly SortSettingsVM _owner;
        private string _label;
        private bool _isSelected;

        public string Id { get; }

        public OrderChipVM(SortSettingsVM owner, string id, string label)
        {
            _owner = owner;
            Id = id;
            _label = label ?? id;
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
            // Selection is visual only for reorder lists.
        }
    }
}
