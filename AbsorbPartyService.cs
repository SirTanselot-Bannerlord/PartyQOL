using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL
{
    public static class AbsorbPartyService
    {
        public static bool CanAbsorb(MobileParty party, out TextObject reason)
        {
            reason = TextObject.GetEmpty();
            if (party == null || party == MobileParty.MainParty || !party.IsActive)
            {
                reason = new TextObject("{=pqol_abs_bad}There is no party to absorb.");
                return false;
            }

            if (party.Owner?.Clan != Clan.PlayerClan || !party.IsLordParty)
            {
                reason = new TextObject("{=pqol_abs_owned}You can only absorb your own clan war parties.");
                return false;
            }

            if (party.IsCaravan || party.IsMilitia || party.IsVillager || party.IsPatrolParty || party.IsGarrison)
            {
                reason = new TextObject("{=pqol_abs_type}This party type cannot be absorbed.");
                return false;
            }

            if (party.Army != null)
            {
                reason = new TextObject("{=pqol_abs_army}Leave the army before absorbing this party.");
                return false;
            }

            if (party.MapEvent != null || party.SiegeEvent != null)
            {
                reason = new TextObject("{=pqol_abs_busy}This party is busy and cannot be absorbed right now.");
                return false;
            }

            return true;
        }

        public static bool TryAbsorbIntoMainParty(MobileParty source)
        {
            if (!CanAbsorb(source, out TextObject reason))
            {
                MBInformationManager.AddQuickInformation(reason);
                return false;
            }

            MobileParty main = MobileParty.MainParty;
            ClearPartyRoles(source);
            TransferShips(source, main);
            TransferRoster(main.ItemRoster, source.ItemRoster);
            TransferTroopRoster(main.PrisonRoster, source.PrisonRoster);
            TransferTroopRoster(main.MemberRoster, source.MemberRoster);
            DestroyPartyAction.Apply(main.Party, source);

            MBInformationManager.AddQuickInformation(
                new TextObject("{=pqol_abs_done}Party absorbed into your main party."));
            return true;
        }

        private static void ClearPartyRoles(MobileParty party)
        {
            party.SetPartyScout(null);
            party.SetPartyQuartermaster(null);
            party.SetPartyEngineer(null);
            party.SetPartySurgeon(null);
            party.SetPartyFirstMate(null);
            party.SetPartyNavigator(null);
        }

        private static void TransferShips(MobileParty source, MobileParty destination)
        {
            for (int i = source.Ships.Count - 1; i >= 0; i--)
            {
                ChangeShipOwnerAction.ApplyByTransferring(destination.Party, source.Ships[i]);
            }
        }

        private static void TransferRoster(ItemRoster destination, ItemRoster source)
        {
            if (source.Count == 0)
            {
                return;
            }

            destination.Add(source);
            source.Clear();
        }

        private static void TransferTroopRoster(TroopRoster destination, TroopRoster source)
        {
            if (source.TotalManCount == 0)
            {
                return;
            }

            destination.Add(source);
            source.Clear();
        }

        public static bool IsManageableOwnedParty(MobileParty party)
        {
            if (party == null || party == MobileParty.MainParty || !party.IsActive)
            {
                return false;
            }

            if (party.Owner?.Clan != Clan.PlayerClan || !party.IsLordParty)
            {
                return false;
            }

            if (party.IsCaravan || party.IsMilitia || party.IsVillager || party.IsPatrolParty || party.IsGarrison)
            {
                return false;
            }

            return party.LeaderHero != null;
        }

        /// <summary>
        /// Same settlement, same army, or within main-party seeing range (min 1.0 map units).
        /// </summary>
        public static bool IsInPlayerVicinity(MobileParty party)
        {
            MobileParty main = MobileParty.MainParty;
            if (party == null || main == null)
            {
                return false;
            }

            if (main.CurrentSettlement != null && party.CurrentSettlement == main.CurrentSettlement)
            {
                return true;
            }

            if (main.Army != null && party.Army == main.Army)
            {
                return true;
            }

            float range = MathF.Max(main.SeeingRange, 1f);
            return main.Position.Distance(party.Position) <= range;
        }

        public static bool IsSelectableForPartyScreen(MobileParty party)
        {
            return IsManageableOwnedParty(party) && IsInPlayerVicinity(party);
        }

        public static MobileParty[] GetManageableOwnedParties()
        {
            return MobileParty.All
                .Where(IsManageableOwnedParty)
                .OrderBy(p => p.Name?.ToString() ?? string.Empty)
                .ToArray();
        }

        public static MobileParty[] GetSelectablePartiesForPartyScreen()
        {
            return MobileParty.All
                .Where(IsSelectableForPartyScreen)
                .OrderBy(p => p.Name?.ToString() ?? string.Empty)
                .ToArray();
        }
    }
}
