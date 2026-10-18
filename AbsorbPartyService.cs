using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
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
            return IsLocalTransferPool(party);
        }

        /// <summary>
        /// Left-panel pools for troop transfer (not absorb). Clan war parties nearby, plus
        /// the current settlement garrison when the player can manage it.
        /// </summary>
        public static bool IsLocalTransferPool(MobileParty party)
        {
            if (party == null || party == MobileParty.MainParty || !party.IsActive)
            {
                return false;
            }

            if (IsManageableOwnedParty(party))
            {
                return IsInPlayerVicinity(party);
            }

            return IsManageableGarrisonAtPlayerLocation(party);
        }

        public static bool IsManageableGarrisonAtPlayerLocation(MobileParty party)
        {
            if (party == null || !party.IsGarrison || !party.IsActive)
            {
                return false;
            }

            Settlement settlement = MobileParty.MainParty?.CurrentSettlement;
            MobileParty garrison = settlement?.Town?.GarrisonParty;
            if (garrison == null || garrison != party)
            {
                return false;
            }

            return settlement.OwnerClan == Clan.PlayerClan;
        }

        /// <summary>
        /// Player-owned settlement garrison at the player's location, creating one if missing.
        /// Empty garrisons are often destroyed when the party screen closes as cancel (dual-side
        /// reopen); vanilla recreates via <see cref="Settlement.AddGarrisonParty"/>.
        /// </summary>
        public static MobileParty GetManageableGarrisonAtPlayerLocation()
        {
            Settlement settlement = MobileParty.MainParty?.CurrentSettlement;
            if (settlement?.Town == null || settlement.OwnerClan != Clan.PlayerClan)
            {
                return null;
            }

            MobileParty garrison = settlement.Town.GarrisonParty;
            if (garrison == null)
            {
                settlement.AddGarrisonParty();
                garrison = settlement.Town.GarrisonParty;
            }

            if (garrison == null || !garrison.IsActive)
            {
                return null;
            }

            return garrison;
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
            return GetLocalTransferPoolsForPartyScreen();
        }

        public static MobileParty[] GetLocalTransferPoolsForPartyScreen()
        {
            List<MobileParty> pools = MobileParty.All
                .Where(p => IsManageableOwnedParty(p) && IsInPlayerVicinity(p))
                .OrderBy(p => p.Name?.ToString() ?? string.Empty)
                .ToList();

            MobileParty garrison = GetManageableGarrisonAtPlayerLocation();
            if (garrison != null && !pools.Contains(garrison))
            {
                pools.Insert(0, garrison);
            }

            return pools.ToArray();
        }

        /// <summary>Main party or any local transfer pool (companion / garrison).</summary>
        public static bool IsTransferSideParty(MobileParty party)
        {
            return party != null
                   && (party == MobileParty.MainParty || IsLocalTransferPool(party));
        }

        /// <summary>
        /// Choices for a side dropdown. Optionally includes MainParty; always skips <paramref name="exclude"/>.
        /// </summary>
        public static MobileParty[] GetTransferSideChoices(MobileParty exclude = null, bool includeMainParty = true)
        {
            var list = new List<MobileParty>();
            if (includeMainParty
                && MobileParty.MainParty != null
                && MobileParty.MainParty != exclude)
            {
                list.Add(MobileParty.MainParty);
            }

            foreach (MobileParty pool in GetLocalTransferPoolsForPartyScreen())
            {
                if (pool != null && pool != exclude && !list.Contains(pool))
                {
                    list.Add(pool);
                }
            }

            return list.ToArray();
        }

        public static TextObject GetTransferPoolLabel(MobileParty party)
        {
            if (party == null)
            {
                return new TextObject("{=pqol_dismiss}Dismiss");
            }

            if (party.IsGarrison)
            {
                Settlement settlement = party.CurrentSettlement
                                        ?? MobileParty.MainParty?.CurrentSettlement;
                if (settlement?.Party?.PrisonRoster != null)
                {
                    return new TextObject("{=pqol_pool_garrison_dungeon}Garrison & Prison");
                }

                return new TextObject("{=pqol_pool_garrison}Garrison");
            }

            return party.Name ?? TextObject.GetEmpty();
        }
    }
}
