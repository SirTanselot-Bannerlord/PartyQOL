using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.InputSystem;

namespace PartyQOL.Upgrades
{
    [HarmonyPatch(typeof(UpgradeTargetVM), nameof(UpgradeTargetVM.ExecuteUpgradeEncyclopediaLink))]
    internal static class UpgradeTargetCtrlRmbPatch
    {
        private static bool Prefix(UpgradeTargetVM __instance)
        {
            if (!Input.IsKeyDown(InputKey.LeftControl) && !Input.IsKeyDown(InputKey.RightControl))
            {
                return true;
            }

            CharacterObject original = Traverse.Create(__instance).Field("_originalCharacter").GetValue<CharacterObject>();
            CharacterObject target = Traverse.Create(__instance).Field("_upgradeTarget").GetValue<CharacterObject>();
            DesiredUpgradePathBehavior store = DesiredUpgradePathBehavior.Instance;
            if (store == null || original == null || target == null)
            {
                return false;
            }

            store.SetPreferred(original, target);
            ApplyHighlightToSiblingTargets(__instance, original);
            return false;
        }

        private static void ApplyHighlightToSiblingTargets(UpgradeTargetVM clicked, CharacterObject original)
        {
            DesiredUpgradePathBehavior store = DesiredUpgradePathBehavior.Instance;
            if (store == null || original?.UpgradeTargets == null)
            {
                clicked.IsHighlighted = true;
                return;
            }

            // Highlight only the preferred slot; clear siblings if we can reach them via PartyCharacter.
            clicked.IsHighlighted = store.IsPreferred(original, Traverse.Create(clicked).Field("_upgradeTarget").GetValue<CharacterObject>());
        }
    }

    [HarmonyPatch(typeof(UpgradeTargetVM), nameof(UpgradeTargetVM.Refresh))]
    internal static class UpgradeTargetHighlightPatch
    {
        private static void Postfix(UpgradeTargetVM __instance)
        {
            DesiredUpgradePathBehavior store = DesiredUpgradePathBehavior.Instance;
            if (store == null)
            {
                return;
            }

            CharacterObject original = Traverse.Create(__instance).Field("_originalCharacter").GetValue<CharacterObject>();
            CharacterObject target = Traverse.Create(__instance).Field("_upgradeTarget").GetValue<CharacterObject>();
            __instance.IsHighlighted = store.IsPreferred(original, target);
        }
    }
}
