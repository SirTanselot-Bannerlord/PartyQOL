using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace PartyQOL
{
    public sealed class AbsorbPartyDialogueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddPlayerLine(
                "partyqol_absorb_party",
                "hero_main_options",
                "partyqol_absorb_confirm",
                "{=pqol_abs_ask}I want your party to join mine — absorb everyone and everything into my party.",
                AbsorbCondition,
                null,
                110,
                AbsorbClickableCondition);

            starter.AddDialogLine(
                "partyqol_absorb_confirm_line",
                "partyqol_absorb_confirm",
                "partyqol_absorb_options",
                "{=pqol_abs_confirm}You want my whole party — troops, prisoners, and baggage — folded into yours?",
                null,
                null);

            starter.AddPlayerLine(
                "partyqol_absorb_yes",
                "partyqol_absorb_options",
                "close_window",
                "{=pqol_abs_yes}Yes. Join my party.",
                null,
                AbsorbConsequence);

            starter.AddPlayerLine(
                "partyqol_absorb_no",
                "partyqol_absorb_options",
                "lord_pretalk",
                "{=pqol_abs_no}Never mind.",
                null,
                null);
        }

        private static bool AbsorbCondition()
        {
            Hero hero = Hero.OneToOneConversationHero;
            MobileParty party = hero?.PartyBelongedTo;
            if (hero == null || party == null || party.LeaderHero != hero)
            {
                return false;
            }

            return AbsorbPartyService.CanAbsorb(party, out _)
                   || (AbsorbPartyService.IsManageableOwnedParty(party) && party.Army != null);
        }

        private static bool AbsorbClickableCondition(out TextObject explanation)
        {
            MobileParty party = Hero.OneToOneConversationHero?.PartyBelongedTo;
            if (AbsorbPartyService.CanAbsorb(party, out explanation))
            {
                return true;
            }

            if (explanation == null || explanation == TextObject.GetEmpty())
            {
                explanation = new TextObject("{=pqol_abs_blocked}You cannot absorb this party right now.");
            }

            return false;
        }

        private static void AbsorbConsequence()
        {
            MobileParty party = Hero.OneToOneConversationHero?.PartyBelongedTo;
            AbsorbPartyService.TryAbsorbIntoMainParty(party);
        }
    }
}
