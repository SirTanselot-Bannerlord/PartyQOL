using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using PartyQOL.Upgrades;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    [ViewModelMixin]
    public sealed class PartyUpgradeTroopVMMixin : BaseViewModelMixin<PartyUpgradeTroopVM>
    {
        private HintViewModel _upgradeAllHint;

        public PartyUpgradeTroopVMMixin(PartyUpgradeTroopVM vm)
            : base(vm)
        {
            _upgradeAllHint = new HintViewModel(new TextObject(
                "{=pqol_upgrade_all_hint}Upgrades every ready troop that has a single path, or a preferred path. Ctrl + right-click an upgrade icon to set or clear the preferred path.\n\nRBM: Shift-click to only perform 0-cost upgrades."));
        }

        [DataSourceProperty]
        public HintViewModel UpgradeAllHint
        {
            get => _upgradeAllHint;
            set
            {
                if (_upgradeAllHint != value)
                {
                    _upgradeAllHint = value;
                    OnPropertyChangedWithValue(value, nameof(UpgradeAllHint));
                }
            }
        }

        [DataSourceMethod]
        public void ExecuteAutoUpgrade()
        {
            PartyUpgradeTroopVM popup = ViewModel;
            if (popup == null || !popup.IsOpen)
            {
                return;
            }

            PartyVM partyVm = Traverse.Create(popup).Field("_partyVM").GetValue<PartyVM>();
            if (partyVm == null)
            {
                partyVm = PartyVMMixin.Current?.Party;
            }

            if (partyVm == null)
            {
                return;
            }

            AutoUpgradeHelper.Run(partyVm, zeroCostOnly: AutoUpgradeHelper.IsShiftDown());
        }
    }
}
