using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    public sealed class SortPresetItemVM : SelectorItemVM
    {
        public string PresetName { get; }

        public SortPresetItemVM(string presetName)
            : base(new TextObject(presetName ?? string.Empty))
        {
            PresetName = presetName ?? string.Empty;
        }
    }
}
