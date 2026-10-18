using HarmonyLib;
using PartyQOL.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

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

            store.TogglePreferred(original, target);
            RefreshHighlightsFor(original);
            return false;
        }

        private static void RefreshHighlightsFor(CharacterObject original)
        {
            DesiredUpgradePathBehavior store = DesiredUpgradePathBehavior.Instance;
            PartyVM partyVm = PartyVMMixin.Current?.Party;
            MBBindingList<PartyTroopManagerItemVM> troops = partyVm?.UpgradePopUp?.Troops;
            if (store == null || troops == null)
            {
                return;
            }

            foreach (PartyTroopManagerItemVM row in troops)
            {
                PartyCharacterVM characterVm = row?.PartyCharacter;
                if (characterVm?.Character != original || characterVm.Upgrades == null)
                {
                    continue;
                }

                foreach (UpgradeTargetVM upgrade in characterVm.Upgrades)
                {
                    CharacterObject upgradeTarget = Traverse.Create(upgrade).Field("_upgradeTarget").GetValue<CharacterObject>();
                    upgrade.IsHighlighted = store.IsPreferred(original, upgradeTarget);
                }
            }
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
