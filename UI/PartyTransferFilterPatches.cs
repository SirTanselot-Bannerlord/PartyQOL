using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PartyQOL.Filtering;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;

namespace PartyQOL.UI
{
    /// <summary>
    /// When a filter is active, Transfer All only moves currently visible stacks.
    /// A Wounded or Healthy filter also limits every transfer to that headcount,
    /// so healthy moves leave the wounded behind and the reverse.
    /// </summary>
    [HarmonyPatch(typeof(PartyVM))]
    internal static class PartyTransferFilterPatches
    {
        private static readonly MethodInfo OnTransferTroopMethod = AccessTools.Method(
            typeof(PartyVM),
            "OnTransferTroop",
            new[]
            {
                typeof(PartyCharacterVM),
                typeof(int),
                typeof(int),
                typeof(PartyScreenLogic.PartyRosterSide)
            });

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PartyVM.ExecuteTransferAllMainTroops))]
        private static bool ExecuteTransferAllMainTroopsPrefix(PartyVM __instance) =>
            !TryTransferVisible(__instance, __instance?.MainPartyTroops);

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PartyVM.ExecuteTransferAllOtherTroops))]
        private static bool ExecuteTransferAllOtherTroopsPrefix(PartyVM __instance) =>
            !TryTransferVisible(__instance, __instance?.OtherPartyTroops);

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PartyVM.ExecuteTransferAllMainPrisoners))]
        private static bool ExecuteTransferAllMainPrisonersPrefix(PartyVM __instance) =>
            !TryTransferVisible(__instance, __instance?.MainPartyPrisoners);

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PartyVM.ExecuteTransferAllOtherPrisoners))]
        private static bool ExecuteTransferAllOtherPrisonersPrefix(PartyVM __instance) =>
            !TryTransferVisible(__instance, __instance?.OtherPartyPrisoners);

        private static bool TryTransferVisible(PartyVM partyVm, MBBindingList<PartyCharacterVM> list)
        {
            if (!FilterState.HasActive || partyVm == null || list == null || OnTransferTroopMethod == null)
            {
                return false;
            }

            List<PartyCharacterVM> visible = list
                .Where(t => t != null
                            && t.IsTroopTransferrable
                            && t.Character != null
                            && FilterState.Passes(t.Character, t.Number, t.WoundedCount))
                .ToList();

            foreach (PartyCharacterVM troop in visible)
            {
                int amount = troop.Number;
                if (troop.Character != null
                    && !troop.Character.IsHero
                    && FilterState.TryConditionTransfer(troop.Number, troop.WoundedCount, amount, out int limited, out _))
                {
                    amount = limited;
                }

                if (amount <= 0)
                {
                    continue;
                }

                OnTransferTroopMethod.Invoke(
                    partyVm,
                    new object[] { troop, -1, amount, troop.Side });
            }

            return true;
        }

        /// <summary>
        /// Vanilla picks wounded or healthy first by side. While a condition filter is
        /// showing, the command's wounded count is the filter's count instead.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("OnTransferTroop")]
        private static bool OnTransferTroopPrefix(
            PartyVM __instance,
            PartyCharacterVM troop,
            int newIndex,
            int transferAmount,
            PartyScreenLogic.PartyRosterSide fromSide)
        {
            if (!FilterState.ShowsConditionCount
                || troop?.Character == null
                || troop.Character.IsHero
                || __instance?.PartyScreenLogic == null)
            {
                return true;
            }

            if (troop.Side == PartyScreenLogic.PartyRosterSide.None
                || fromSide == PartyScreenLogic.PartyRosterSide.None)
            {
                return false;
            }

            if (!FilterState.TryConditionTransfer(troop.Number, troop.WoundedCount, transferAmount, out int amount, out int wounded))
            {
                return false;
            }

            __instance.SetSelectedCharacter(troop);
            if (newIndex == -1)
            {
                newIndex = __instance.PartyScreenLogic.GetIndexToInsertTroop(1 - troop.Side, troop.Type, troop.Troop);
            }
            else if (fromSide == PartyScreenLogic.PartyRosterSide.Left)
            {
                __instance.MainPartySortController.SelectSortType(PartyScreenLogic.TroopSortType.Custom);
            }
            else if (fromSide == PartyScreenLogic.PartyRosterSide.Right)
            {
                __instance.OtherPartySortController.SelectSortType(PartyScreenLogic.TroopSortType.Custom);
            }

            PartyScreenLogic.PartyCommand command = new PartyScreenLogic.PartyCommand();
            command.FillForTransferTroop(fromSide, troop.Type, troop.Character, amount, wounded, newIndex);
            __instance.PartyScreenLogic.AddCommand(command);
            return false;
        }
    }
}
