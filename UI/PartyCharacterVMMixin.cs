using System.Runtime.CompilerServices;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;

namespace PartyQOL.UI
{
    [ViewModelMixin]
    public sealed class PartyCharacterVMMixin : BaseViewModelMixin<PartyCharacterVM>
    {
        private static readonly ConditionalWeakTable<PartyCharacterVM, Holder> Holders =
            new ConditionalWeakTable<PartyCharacterVM, Holder>();

        private bool _isHiddenByPartyQolFilter;

        public PartyCharacterVMMixin(PartyCharacterVM vm)
            : base(vm)
        {
            Holders.Remove(vm);
            Holders.Add(vm, new Holder(this));
        }

        [DataSourceProperty]
        public bool IsHiddenByPartyQolFilter
        {
            get => _isHiddenByPartyQolFilter;
            set
            {
                if (_isHiddenByPartyQolFilter != value)
                {
                    _isHiddenByPartyQolFilter = value;
                    OnPropertyChangedWithValue(value, nameof(IsHiddenByPartyQolFilter));
                }
            }
        }

        internal static void SetFiltered(PartyCharacterVM vm, bool hidden)
        {
            if (vm == null)
            {
                return;
            }

            if (Holders.TryGetValue(vm, out Holder holder))
            {
                holder.Mixin.IsHiddenByPartyQolFilter = hidden;
            }
        }

        private sealed class Holder
        {
            public readonly PartyCharacterVMMixin Mixin;

            public Holder(PartyCharacterVMMixin mixin)
            {
                Mixin = mixin;
            }
        }
    }
}
