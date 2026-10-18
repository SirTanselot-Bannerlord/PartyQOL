using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    public sealed class FilterPresetItemVM : SelectorItemVM
    {
        public string PresetName { get; }

        public bool IsNone { get; }

        public FilterPresetItemVM(string presetName, bool isNone = false)
            : base(new TextObject(presetName ?? string.Empty))
        {
            PresetName = presetName ?? string.Empty;
            IsNone = isNone;
        }
    }
}
