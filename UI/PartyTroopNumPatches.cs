using HarmonyLib;
using PartyQOL.Filtering;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;

namespace PartyQOL.UI
{
    /// <summary>
    /// Row count follows the Wounded criterion: wounded-only or healthy-only, with no "+Nw" suffix.
    /// </summary>
    [HarmonyPatch(typeof(PartyCharacterVM), nameof(PartyCharacterVM.TroopNum), MethodType.Getter)]
    internal static class PartyTroopNumPatches
    {
        private static void Postfix(PartyCharacterVM __instance, ref string __result)
        {
            if (!FilterState.ShowsConditionCount || __instance?.Character == null || __instance.Character.IsHero)
            {
                return;
            }

            __result = FilterState.VisibleCount(__instance.Number, __instance.WoundedCount).ToString();
        }
    }
}
