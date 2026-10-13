using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using PartyQOL.Upgrades;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    [ViewModelMixin]
    public sealed class PartyUpgradeTroopVMMixin : BaseViewModelMixin<PartyUpgradeTroopVM>
    {
        private string _autoUpgradeText;

        public PartyUpgradeTroopVMMixin(PartyUpgradeTroopVM vm)
            : base(vm)
        {
            _autoUpgradeText = new TextObject("{=pqol_auto_up}Auto Upgrade").ToString();
        }

        [DataSourceProperty]
        public string AutoUpgradeText
        {
            get => _autoUpgradeText;
            set
            {
                if (_autoUpgradeText != value)
                {
                    _autoUpgradeText = value;
                    OnPropertyChangedWithValue(value, nameof(AutoUpgradeText));
                }
            }
        }

        [DataSourceMethod]
        public void ExecuteAutoUpgrade()
        {
            PartyUpgradeTroopVM popup = ViewModel;
            if (popup == null || !popup.IsUpgradePopUp || !popup.IsOpen)
            {
                return;
            }

            PartyVM partyVm = Traverse.Create(popup).Field("_partyVM").GetValue<PartyVM>();
            if (partyVm == null)
            {
                return;
            }

            bool freeOnly = !AutoUpgradeHelper.IsControlDown();
            AutoUpgradeHelper.Run(partyVm, freeOnly);
        }
    }
}
