using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PartyQOL.Sorting
{
    public static class TroopRosterSorter
    {
        public static void SortPartyScreen(PartyVM partyVM, IReadOnlyList<SortKey> keys)
        {
            if (partyVM?.PartyScreenLogic == null)
            {
                return;
            }

            if (keys == null || keys.Count == 0)
            {
                keys = TroopSortState.DefaultKeys;
            }

            PartyScreenLogic logic = partyVM.PartyScreenLogic;
            SortSide(
                logic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member),
                partyVM.MainPartyTroops,
                keys);
            SortSide(
                logic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner),
                partyVM.MainPartyPrisoners,
                keys);
            SortSide(
                logic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member),
                partyVM.OtherPartyTroops,
                keys);
            SortSide(
                logic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner),
                partyVM.OtherPartyPrisoners,
                keys);
        }

        public static void SortPartyScreen(PartyVM partyVM)
        {
            SortPartyScreen(partyVM, TroopSortState.Keys);
        }

        private static void SortSide(
            TroopRoster roster,
            MBBindingList<PartyCharacterVM> list,
            IReadOnlyList<SortKey> keys)
        {
            if (roster == null || roster.Count <= 1)
            {
                return;
            }

            IReadOnlyList<SortKey> expanded = ExpandWithTiebreaks(keys);
            List<TroopRosterElement> ordered = GetListFromRoster(roster);
            ordered.Sort((a, b) => CompareLex(a, b, expanded));
            SyncRosterWithList(roster, ordered);
            if (list != null)
            {
                ReorderBindingList(list, ordered);
            }
        }

        /// <summary>
        /// After the user’s keys, always break ties with Tier (desc) then Name (asc), unless those modes
        /// were already chosen explicitly (so the user’s Asc/Desc / position wins).
        /// </summary>
        private static IReadOnlyList<SortKey> ExpandWithTiebreaks(IReadOnlyList<SortKey> keys)
        {
            var expanded = new List<SortKey>(keys ?? Array.Empty<SortKey>());
            if (expanded.All(k => k.Mode != TroopSortMode.Tier))
            {
                expanded.Add(new SortKey(TroopSortMode.Tier, ascending: false));
            }

            if (expanded.All(k => k.Mode != TroopSortMode.Name))
            {
                expanded.Add(new SortKey(TroopSortMode.Name, ascending: true));
            }

            return expanded;
        }

        private static int CompareLex(
            TroopRosterElement a,
            TroopRosterElement b,
            IReadOnlyList<SortKey> keys)
        {
            CharacterObject ca = a.Character;
            CharacterObject cb = b.Character;
            if (ca == null || cb == null)
            {
                return 0;
            }

            // Heroes always stay above regular troops; MainHero first (not flipped by ascending).
            int hero = HeroRank(ca).CompareTo(HeroRank(cb));
            if (hero != 0)
            {
                return hero;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                int result = CompareKey(a, b, keys[i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        private static int CompareKey(TroopRosterElement a, TroopRosterElement b, SortKey key)
        {
            CharacterObject ca = a.Character;
            CharacterObject cb = b.Character;
            int dir = key.Ascending ? 1 : -1;
            return key.Mode switch
            {
                TroopSortMode.Type => ca.DefaultFormationClass.CompareTo(cb.DefaultFormationClass) * dir,
                TroopSortMode.Tier => ca.Tier.CompareTo(cb.Tier) * dir,
                TroopSortMode.Level => ca.Level.CompareTo(cb.Level) * dir,
                TroopSortMode.Culture => string.Compare(CultureName(ca), CultureName(cb), StringComparison.OrdinalIgnoreCase) * dir,
                TroopSortMode.Name => string.Compare(ca.Name?.ToString(), cb.Name?.ToString(), StringComparison.OrdinalIgnoreCase) * dir,
                TroopSortMode.Count => a.Number.CompareTo(b.Number) * dir,
                _ => 0
            };
        }

        private static int HeroRank(CharacterObject character)
        {
            if (!character.IsHero)
            {
                return 100;
            }

            Hero hero = character.HeroObject;
            if (hero == Hero.MainHero)
            {
                return 0;
            }

            if (hero?.Clan == Clan.PlayerClan)
            {
                return 1;
            }

            return 2;
        }

        private static string CultureName(CharacterObject character)
        {
            return character.Culture?.Name?.ToString() ?? string.Empty;
        }

        private static List<TroopRosterElement> GetListFromRoster(TroopRoster roster)
        {
            var list = new List<TroopRosterElement>(roster.Count);
            for (int i = 0; i < roster.Count; i++)
            {
                list.Add(roster.GetElementCopyAtIndex(i));
            }

            return list;
        }

        private static void SyncRosterWithList(TroopRoster roster, List<TroopRosterElement> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int firstIndex = roster.FindIndexOfTroop(list[i].Character);
                if (firstIndex >= 0 && firstIndex != i)
                {
                    roster.SwapTroopsAtIndices(firstIndex, i);
                }
            }
        }

        private static void ReorderBindingList(MBBindingList<PartyCharacterVM> list, List<TroopRosterElement> ordered)
        {
            var byCharacter = new Dictionary<CharacterObject, PartyCharacterVM>();
            foreach (PartyCharacterVM item in list)
            {
                if (item?.Character != null && !byCharacter.ContainsKey(item.Character))
                {
                    byCharacter[item.Character] = item;
                }
            }

            list.Clear();
            foreach (TroopRosterElement element in ordered)
            {
                if (element.Character != null && byCharacter.TryGetValue(element.Character, out PartyCharacterVM vm))
                {
                    list.Add(vm);
                    byCharacter.Remove(element.Character);
                }
            }

            foreach (PartyCharacterVM leftover in byCharacter.Values)
            {
                list.Add(leftover);
            }
        }
    }
}
