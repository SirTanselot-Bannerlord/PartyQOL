using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace PartyQOL
{
    /// <summary>
    /// Opens the party screen for an arbitrary left/right transfer-pool pair
    /// (or dismiss on the left). Garrison sides bind settlement dungeon prisoners
    /// into that side's prisoner list.
    /// </summary>
    public static class PairPartyScreen
    {
        public static void Open(MobileParty leftParty, MobileParty rightParty)
        {
            if (!TryCreateLogic(leftParty, rightParty, out PartyScreenLogic logic))
            {
                return;
            }

            Push(logic);
        }

        /// <summary>
        /// Builds the transfer logic for a pair without touching the screen stack.
        /// Side switches install this onto the already-open party screen.
        /// </summary>
        public static bool TryCreateLogic(MobileParty leftParty, MobileParty rightParty, out PartyScreenLogic logic)
        {
            logic = null;
            if (Game.Current?.GameStateManager == null)
            {
                return false;
            }

            if (rightParty == null || !AbsorbPartyService.IsTransferSideParty(rightParty))
            {
                rightParty = MobileParty.MainParty;
            }

            if (leftParty != null && leftParty.IsGarrison)
            {
                leftParty = AbsorbPartyService.GetManageableGarrisonAtPlayerLocation();
            }

            if (leftParty == null || !AbsorbPartyService.IsTransferSideParty(leftParty) || leftParty == rightParty)
            {
                logic = CreateDismissLogic(rightParty);
                return logic != null;
            }

            ResolveSideRosters(
                leftParty,
                out TroopRoster leftMembers,
                out TroopRoster leftPrisoners,
                out int leftMemberLimit,
                out int leftPrisonerLimit,
                out TextObject leftName);

            ResolveSideRosters(
                rightParty,
                out TroopRoster rightMembers,
                out TroopRoster rightPrisoners,
                out int rightMemberLimit,
                out int rightPrisonerLimit,
                out TextObject rightName);

            bool involvesMain = leftParty == MobileParty.MainParty || rightParty == MobileParty.MainParty;
            PartyScreenLogicInitializationData init = new PartyScreenLogicInitializationData
            {
                LeftOwnerParty = leftParty.Party,
                RightOwnerParty = rightParty.Party,
                LeftMemberRoster = leftMembers,
                LeftPrisonerRoster = leftPrisoners,
                RightMemberRoster = rightMembers,
                RightPrisonerRoster = rightPrisoners,
                LeftLeaderHero = leftParty.LeaderHero,
                RightLeaderHero = rightParty.LeaderHero,
                LeftPartyMembersSizeLimit = leftMemberLimit,
                LeftPartyPrisonersSizeLimit = leftPrisonerLimit,
                RightPartyMembersSizeLimit = rightMemberLimit,
                RightPartyPrisonersSizeLimit = rightPrisonerLimit,
                LeftPartyName = leftName,
                RightPartyName = rightName,
                Header = new TextObject("{=uQgNPJnc}Manage Troops"),
                TroopTransferableDelegate = PartyScreenHelper.ClanManageTroopAndPrisonerTransferableDelegate,
                PartyPresentationDoneButtonDelegate = DoneHandler,
                PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal,
                IsDismissMode = false,
                IsTroopUpgradesDisabled = !involvesMain,
                MemberTransferState = PartyScreenLogic.TransferState.Transferable,
                PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
                AccompanyingTransferState = PartyScreenLogic.TransferState.Transferable
            };

            logic = new PartyScreenLogic();
            logic.Initialize(init);
            return true;
        }

        private static void Push(PartyScreenLogic logic)
        {
            PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
            partyState.IsDonating = false;
            partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
            partyState.PartyScreenLogic = logic;
            Game.Current.GameStateManager.PushState(partyState);
        }

        /// <summary>
        /// Garrison members + town dungeon prisoners share one prisoner column.
        /// </summary>
        private static void ResolveSideRosters(
            MobileParty party,
            out TroopRoster members,
            out TroopRoster prisoners,
            out int memberLimit,
            out int prisonerLimit,
            out TextObject name)
        {
            members = party.MemberRoster;
            // PartySizeLimit caches the last roster change and keeps the at-sea multiplier after docking.
            memberLimit = LiveMemberLimit(party.Party);
            prisoners = party.PrisonRoster;
            prisonerLimit = party.Party.PrisonerSizeLimit;
            name = AbsorbPartyService.GetTransferPoolLabel(party);

            if (!party.IsGarrison)
            {
                // Non-garrison: prefer the party's own name over the pool label helper's TextObject.
                name = party.Name ?? name;
                return;
            }

            Settlement settlement = party.CurrentSettlement
                                    ?? MobileParty.MainParty?.CurrentSettlement;
            PartyBase settlementParty = settlement?.Party;
            if (settlementParty?.PrisonRoster == null)
            {
                return;
            }

            prisoners = settlementParty.PrisonRoster;
            prisonerLimit = settlementParty.PrisonerSizeLimit;
        }

        /// <summary>
        /// Current member cap, including or excluding the at-sea ship bonus as of this moment.
        /// Same truncation the game uses when it caches <see cref="PartyBase.PartySizeLimit"/>.
        /// </summary>
        internal static int LiveMemberLimit(PartyBase party)
        {
            if (party == null)
            {
                return 0;
            }

            return (int)party.PartySizeLimitExplainer.ResultNumber;
        }

        public static void OpenDismiss(MobileParty rightParty = null)
        {
            PartyScreenLogic logic = CreateDismissLogic(rightParty);
            if (logic != null)
            {
                Push(logic);
            }
        }

        private static PartyScreenLogic CreateDismissLogic(MobileParty rightParty)
        {
            if (rightParty == null || !AbsorbPartyService.IsTransferSideParty(rightParty))
            {
                rightParty = MobileParty.MainParty;
            }

            if (rightParty == null)
            {
                return null;
            }

            ResolveSideRosters(
                rightParty,
                out TroopRoster rightMembers,
                out TroopRoster rightPrisoners,
                out int rightMemberLimit,
                out int rightPrisonerLimit,
                out TextObject rightName);

            PartyScreenLogicInitializationData init = new PartyScreenLogicInitializationData
            {
                LeftOwnerParty = null,
                RightOwnerParty = rightParty.Party,
                LeftMemberRoster = TroopRoster.CreateDummyTroopRoster(),
                LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
                RightMemberRoster = rightMembers,
                RightPrisonerRoster = rightPrisoners,
                LeftLeaderHero = null,
                RightLeaderHero = rightParty.LeaderHero,
                LeftPartyMembersSizeLimit = 0,
                LeftPartyPrisonersSizeLimit = 0,
                RightPartyMembersSizeLimit = rightMemberLimit,
                RightPartyPrisonersSizeLimit = rightPrisonerLimit,
                LeftPartyName = null,
                RightPartyName = rightName,
                TroopTransferableDelegate = PartyScreenHelper.TroopTransferableDelegate,
                PartyPresentationDoneButtonDelegate = DoneHandler,
                PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal,
                IsDismissMode = true,
                IsTroopUpgradesDisabled = rightParty != MobileParty.MainParty,
                MemberTransferState = PartyScreenLogic.TransferState.Transferable,
                PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
                AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable
            };

            PartyScreenLogic logic = new PartyScreenLogic();
            logic.Initialize(init);
            return logic;
        }

        public static void Reopen(MobileParty leftParty, MobileParty rightParty)
        {
            if (rightParty == null)
            {
                rightParty = MobileParty.MainParty;
            }

            PartyScreenHelper.CloseScreen(isForced: true, fromCancel: true);
            UI.PartyVMMixin.PreserveFilterOnNextOpen();
            Open(leftParty, rightParty);
        }

        /// <summary>Back-compat alias.</summary>
        public static void ReopenWithLeft(MobileParty leftParty, MobileParty rightParty = null)
        {
            Reopen(leftParty, rightParty ?? MobileParty.MainParty);
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
