using System.Runtime.CompilerServices;
using HarmonyLib;
using PartyQOL.Filtering;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PartyQOL.UI
{
    [HarmonyPatch(typeof(PartyCompositionVM), "RefreshCounts")]
    internal static class PartyCompositionFilterPatches
    {
        private static readonly ConditionalWeakTable<PartyCompositionVM, PartyCompositionVMMixin> Mixins =
            new ConditionalWeakTable<PartyCompositionVM, PartyCompositionVMMixin>();

        internal static void Register(PartyCompositionVM vm, PartyCompositionVMMixin mixin)
        {
            if (vm == null || mixin == null)
            {
                return;
            }

            Mixins.Remove(vm);
            Mixins.Add(vm, mixin);
        }

        [HarmonyPostfix]
        private static void Postfix(PartyCompositionVM __instance, MBBindingList<PartyCharacterVM> list)
        {
            if (__instance == null)
            {
                return;
            }

            if (!Mixins.TryGetValue(__instance, out PartyCompositionVMMixin mixin))
            {
                mixin = null;
            }

            mixin?.SetExcluded(
                FilterState.IsFormationExcluded("Infantry"),
                FilterState.IsFormationExcluded("Ranged"),
                FilterState.IsFormationExcluded("Cavalry"),
                FilterState.IsFormationExcluded("HorseArcher"));

            if (!FilterState.HasActive || list == null)
            {
                mixin?.SetCountColors(false, false, false, false);
                return;
            }

            CountByType(list, requirePass: false, out int fullInf, out int fullRng, out int fullCav, out int fullHa);
            CountByType(list, requirePass: true, out int filInf, out int filRng, out int filCav, out int filHa);

            __instance.InfantryCount = filInf;
            __instance.RangedCount = filRng;
            __instance.CavalryCount = filCav;
            __instance.HorseArcherCount = filHa;

            // Red only when that category's visible total changed vs unfiltered.
            mixin?.SetCountColors(
                filInf != fullInf,
                filRng != fullRng,
                filCav != fullCav,
                filHa != fullHa);
        }

        private static void CountByType(
            MBBindingList<PartyCharacterVM> list,
            bool requirePass,
            out int infantry,
            out int ranged,
            out int cavalry,
            out int horseArcher)
        {
            infantry = ranged = cavalry = horseArcher = 0;
            foreach (PartyCharacterVM item in list)
            {
                if (item?.Character == null)
                {
                    continue;
                }

                // Heroes stay visible under filters and are included in vanilla composition —
                // keep them in both full and filtered tallies so companions don't "vanish".
                if (requirePass && !item.Character.IsHero && !FilterState.Passes(item.Character, item.Number, item.WoundedCount))
                {
                    continue;
                }

                int n = item.Character.IsHero
                    ? 1
                    : requirePass
                        ? FilterState.VisibleCount(item.Number, item.WoundedCount)
                        : item.Number;
                switch (item.Character.DefaultFormationClass)
                {
                    case FormationClass.Infantry:
                        infantry += n;
                        break;
                    case FormationClass.Ranged:
                        ranged += n;
                        break;
                    case FormationClass.Cavalry:
                        cavalry += n;
                        break;
                    case FormationClass.HorseArcher:
                        horseArcher += n;
                        break;
                }
            }
        }
    }
}
