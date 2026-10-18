using HarmonyLib;
using PartyQOL.Filtering;
using PartyQOL.Sorting;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;

namespace PartyQOL.UI
{
    [HarmonyPatch(typeof(PartyVM))]
    internal static class PartyFilterRefreshPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("InitializeTroopLists")]
        private static void InitializeTroopListsPostfix(PartyVM __instance)
        {
            // Full list rebuild: always re-bind sort + filter.
            Reapply(__instance, allowAutoSort: true, forceFilter: true);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnTransferTroop")]
        private static void OnTransferTroopPostfix(PartyVM __instance)
        {
            // Transfer: sort only if opted in. Filter only when a filter is active
            // (stack size / new VM rows / labels can change; type/culture alone usually wouldn't).
            PartyQolSettings.EnsureLoaded();
            Reapply(
                __instance,
                allowAutoSort: PartyQolSettings.AutoSortOnTransfer,
                forceFilter: false);
        }

        [HarmonyPostfix]
        [HarmonyPatch("SortTroops")]
        private static void SortTroopsPostfix(PartyVM __instance)
        {
            // Player (or vanilla) sorted — apply our preset if one is active.
            Reapply(__instance, allowAutoSort: true, forceFilter: true);
        }

        private static void Reapply(PartyVM partyVm, bool allowAutoSort, bool forceFilter)
        {
            if (partyVm == null)
            {
                return;
            }

            if (allowAutoSort && SortPresets.HasActiveSaved)
            {
                // SortPartyScreen already ends with ApplyPartyScreen.
                TroopRosterSorter.SortPartyScreen(partyVm);
                return;
            }

            if (forceFilter || FilterState.HasActive)
            {
                TroopFilterApplicator.ApplyPartyScreen(partyVm);
            }
        }
    }
}
