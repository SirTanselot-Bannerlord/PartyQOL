using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace PartyQOL
{
    /// <summary>
    /// Opens the party screen for an arbitrary left/right pair (or dismiss on the left).
    /// v1 UI keeps the right side on MainParty; this helper already accepts any right party.
    /// </summary>
    public static class PairPartyScreen
    {
        public static void Open(MobileParty leftParty, MobileParty rightParty)
        {
            if (rightParty == null)
            {
                rightParty = MobileParty.MainParty;
            }

            if (leftParty == null)
            {
                OpenDismiss(rightParty);
                return;
            }

            // Vicinity gate: never attach a far party to the left roster.
            if (!AbsorbPartyService.IsSelectableForPartyScreen(leftParty))
            {
                OpenDismiss(rightParty);
                return;
            }

            PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
            partyState.IsDonating = false;
            partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;

            PartyScreenLogicInitializationData init = new PartyScreenLogicInitializationData
            {
                LeftOwnerParty = leftParty.Party,
                RightOwnerParty = rightParty.Party,
                LeftMemberRoster = leftParty.MemberRoster,
                LeftPrisonerRoster = leftParty.PrisonRoster,
                RightMemberRoster = rightParty.MemberRoster,
                RightPrisonerRoster = rightParty.PrisonRoster,
                LeftLeaderHero = leftParty.LeaderHero,
                RightLeaderHero = rightParty.LeaderHero,
                LeftPartyMembersSizeLimit = leftParty.Party.PartySizeLimit,
                LeftPartyPrisonersSizeLimit = leftParty.Party.PrisonerSizeLimit,
                RightPartyMembersSizeLimit = rightParty.Party.PartySizeLimit,
                RightPartyPrisonersSizeLimit = rightParty.Party.PrisonerSizeLimit,
                LeftPartyName = leftParty.Name,
                RightPartyName = rightParty.Name,
                Header = new TextObject("{=uQgNPJnc}Manage Troops"),
                TroopTransferableDelegate = PartyScreenHelper.ClanManageTroopAndPrisonerTransferableDelegate,
                PartyPresentationDoneButtonDelegate = DoneHandler,
                PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal,
                IsDismissMode = false,
                IsTroopUpgradesDisabled = true,
                MemberTransferState = PartyScreenLogic.TransferState.Transferable,
                PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
                AccompanyingTransferState = PartyScreenLogic.TransferState.Transferable
            };

            PartyScreenLogic logic = new PartyScreenLogic();
            logic.Initialize(init);
            partyState.PartyScreenLogic = logic;
            Game.Current.GameStateManager.PushState(partyState);
        }

        public static void OpenDismiss(MobileParty rightParty = null)
        {
            if (rightParty == null)
            {
                rightParty = MobileParty.MainParty;
            }

            PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
            partyState.IsDonating = false;
            partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;

            PartyScreenLogicInitializationData init = new PartyScreenLogicInitializationData
            {
                LeftOwnerParty = null,
                RightOwnerParty = rightParty.Party,
                LeftMemberRoster = TroopRoster.CreateDummyTroopRoster(),
                LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
                RightMemberRoster = rightParty.MemberRoster,
                RightPrisonerRoster = rightParty.PrisonRoster,
                LeftLeaderHero = null,
                RightLeaderHero = rightParty.LeaderHero,
                LeftPartyMembersSizeLimit = 0,
                LeftPartyPrisonersSizeLimit = 0,
                RightPartyMembersSizeLimit = rightParty.Party.PartySizeLimit,
                RightPartyPrisonersSizeLimit = rightParty.Party.PrisonerSizeLimit,
                LeftPartyName = null,
                RightPartyName = rightParty.Name,
                TroopTransferableDelegate = PartyScreenHelper.TroopTransferableDelegate,
                PartyPresentationDoneButtonDelegate = DoneHandler,
                PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal,
                IsDismissMode = true,
                IsTroopUpgradesDisabled = false,
                MemberTransferState = PartyScreenLogic.TransferState.Transferable,
                PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
                AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable
            };

            PartyScreenLogic logic = new PartyScreenLogic();
            logic.Initialize(init);
            partyState.PartyScreenLogic = logic;
            Game.Current.GameStateManager.PushState(partyState);
        }

        /// <summary>
        /// Reopen with a new left selection; right stays as provided (v1: MainParty).
        /// </summary>
        public static void ReopenWithLeft(MobileParty leftParty, MobileParty rightParty = null)
        {
            if (rightParty == null)
            {
                rightParty = MobileParty.MainParty;
            }

            PartyScreenHelper.CloseScreen(isForced: true, fromCancel: true);
            Open(leftParty, rightParty);
        }

        private static bool DoneHandler(
            TroopRoster leftMemberRoster,
            TroopRoster leftPrisonRoster,
            TroopRoster rightMemberRoster,
            TroopRoster rightPrisonRoster,
            FlattenedTroopRoster takenPrisonerRoster,
            FlattenedTroopRoster releasedPrisonerRoster,
            bool isForced,
            PartyBase leftParty = null,
            PartyBase rightParty = null)
        {
            return true;
        }
    }
}
