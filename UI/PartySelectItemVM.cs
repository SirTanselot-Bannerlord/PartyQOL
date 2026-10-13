using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    public sealed class PartySelectItemVM : SelectorItemVM
    {
        public MobileParty Party { get; }

        public bool IsDismiss => Party == null;

        public PartySelectItemVM(TextObject name, MobileParty party)
            : base(name)
        {
            Party = party;
        }
    }
}
