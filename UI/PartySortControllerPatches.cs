using System;
using HarmonyLib;
using PartyQOL.Sorting;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    /// <summary>
    /// Replaces the vanilla "Custom" dropdown entry with saved PartyQOL sort presets (still TroopSortType.Custom,
    /// which skips the built-in single-key comparer).
    /// </summary>
    [HarmonyPatch(typeof(PartySortControllerVM))]
    internal static class PartySortControllerPatches
    {
        private static int _suppressSelect;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, typeof(PartyScreenLogic.PartyRosterSide), typeof(Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool>))]
        private static void ConstructorPostfix(PartySortControllerVM __instance)
        {
            SortPresets.EnsureLoadedPublic();
            RebuildItems(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnSortSelected")]
        private static void OnSortSelectedPostfix(PartySortControllerVM __instance)
        {
            if (_suppressSelect > 0)
            {
                return;
            }

            TroopSortSelectorItemVM selected = __instance.SortOptions?.SelectedItem;
            if (selected == null || selected.SortType != PartyScreenLogic.TroopSortType.Custom)
            {
                return;
            }

            string name = selected.StringItem;
            if (string.IsNullOrEmpty(name) || !SortPresets.Activate(name))
            {
                return;
            }

            PartyVM partyVm = PartyVMMixin.Current?.Party;
            if (partyVm == null)
            {
                return;
            }

            TroopRosterSorter.SortPartyScreen(partyVm);
            SyncOtherControllerSelection(partyVm, __instance, name);
        }

        internal static void RefreshPresetItems(PartyVM partyVm)
        {
            if (partyVm == null)
            {
                return;
            }

            RebuildItems(partyVm.MainPartySortController);
            RebuildItems(partyVm.OtherPartySortController);
            SelectActivePreset(partyVm);
        }

        internal static void SelectActivePreset(PartyVM partyVm)
        {
            if (partyVm == null)
            {
                return;
            }

            SelectByName(partyVm.MainPartySortController, SortPresets.ActiveName);
            SelectByName(partyVm.OtherPartySortController, SortPresets.ActiveName);
        }

        private static void SyncOtherControllerSelection(PartyVM partyVm, PartySortControllerVM source, string name)
        {
            if (partyVm.MainPartySortController != null && partyVm.MainPartySortController != source)
            {
                SelectByName(partyVm.MainPartySortController, name);
            }

            if (partyVm.OtherPartySortController != null && partyVm.OtherPartySortController != source)
            {
                SelectByName(partyVm.OtherPartySortController, name);
            }
        }

        private static void RebuildItems(PartySortControllerVM controller)
        {
            if (controller?.SortOptions?.ItemList == null)
            {
                return;
            }

            SelectorVM<TroopSortSelectorItemVM> options = controller.SortOptions;
            MBBindingList<TroopSortSelectorItemVM> list = options.ItemList;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].SortType == PartyScreenLogic.TroopSortType.Custom)
                {
                    list.RemoveAt(i);
                }
            }

            foreach (SortPreset preset in SortPresets.All)
            {
                list.Add(new TroopSortSelectorItemVM(new TextObject(preset.Name), PartyScreenLogic.TroopSortType.Custom));
            }

            SelectByName(controller, SortPresets.ActiveName);
        }

        private static void SelectByName(PartySortControllerVM controller, string name)
        {
            if (controller?.SortOptions?.ItemList == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            MBBindingList<TroopSortSelectorItemVM> list = controller.SortOptions.ItemList;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].SortType == PartyScreenLogic.TroopSortType.Custom
                    && string.Equals(list[i].StringItem, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (controller.SortOptions.SelectedIndex == i)
                    {
                        return;
                    }

                    _suppressSelect++;
                    try
                    {
                        controller.SortOptions.SelectedIndex = i;
                    }
                    finally
                    {
                        _suppressSelect--;
                    }

                    return;
                }
            }
        }
    }
}
