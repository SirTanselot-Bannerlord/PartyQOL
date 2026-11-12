using System;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Localization;

namespace PartyQOL.UI
{
    /// <summary>
    /// Swaps the open party screen onto another pair without popping the Gauntlet layer.
    /// Closing and pushing the screen tears down the movie and the character scene, which
    /// access-violates in native UI and flashes a black frame.
    /// </summary>
    internal static class PartyScreenRetarget
    {
        internal static bool Apply(PartyVM vm, MobileParty left, MobileParty right)
        {
            if (vm == null)
            {
                PartyQolLog.Write("retarget skipped: no PartyVM");
                return false;
            }

            string fromLeft = vm.PartyScreenLogic?.LeftOwnerParty?.Name?.ToString() ?? "(dismiss)";
            string fromRight = vm.PartyScreenLogic?.RightOwnerParty?.Name?.ToString() ?? "(none)";
            PartyQolLog.Write("retarget from L=" + fromLeft + " R=" + fromRight
                              + " to L=" + (left?.Name?.ToString() ?? "(dismiss)")
                              + " R=" + (right?.Name?.ToString() ?? "(none)"));

            PartyScreenClosedDelegate closed = vm.PartyScreenLogic == null
                ? null
                : AccessTools.Field(typeof(PartyScreenLogic), "PartyScreenClosedEvent")
                    ?.GetValue(vm.PartyScreenLogic) as PartyScreenClosedDelegate;
            if (!PairPartyScreen.TryCreateLogic(left, right, out PartyScreenLogic logic, closed))
            {
                PartyQolLog.Write("retarget failed: could not build logic");
                return false;
            }

            PartyState state = PartyScreenHelper.GetActivePartyState();
            if (state == null)
            {
                PartyQolLog.Write("retarget failed: no active party state");
                return false;
            }

            state.PartyScreenLogic = logic;
            AccessTools.Property(typeof(PartyVM), "PartyScreenLogic").SetValue(vm, logic);
            Hook(vm, logic);

            Traverse view = Traverse.Create(vm);
            view.Method("InitializeStaticInformation").GetValue();
            view.Method("InitializeTroopLists").GetValue();
            view.Method("RefreshPartyInformation").GetValue();
            view.Method("UpdateTroopManagerPopUpCounts").GetValue();
            view.Method("OnPartyGoldChanged").GetValue();
            view.Method("OnPartyMoraleChanged").GetValue();
            view.Method("OnPartyHorseChanged").GetValue();
            view.Method("OnPartyInfluenceChanged").GetValue();

            vm.IsDoneDisabled = !logic.IsDoneActive();
            vm.IsCancelDisabled = !logic.IsCancelActive();
            if (vm.DoneHint != null)
            {
                vm.DoneHint.HintText = new TextObject("{=!}" + (logic.DoneReasonString ?? string.Empty));
            }

            PartyQolLog.Write("retarget ok Ltroops=" + vm.OtherPartyTroops?.Count
                              + " Rtroops=" + vm.MainPartyTroops?.Count);
            return true;
        }

        private static void Hook(PartyVM vm, PartyScreenLogic logic)
        {
            logic.PartyGoldChange += Create<PartyScreenLogic.PartyGoldDelegate>(vm, "OnPartyGoldChanged");
            logic.PartyMoraleChange += Create<PartyScreenLogic.PartyMoraleDelegate>(vm, "OnPartyMoraleChanged");
            logic.PartyInfluenceChange += Create<PartyScreenLogic.PartyInfluenceDelegate>(vm, "OnPartyInfluenceChanged");
            logic.PartyHorseChange += Create<PartyScreenLogic.PartyHorseDelegate>(vm, "OnPartyHorseChanged");
            logic.AfterReset += Create<PartyScreenLogic.AfterResetDelegate>(vm, "AfterReset");
            logic.UpdateDelegate = Create<PartyScreenLogic.PresentationUpdate>(vm, "Update");
        }

        private static T Create<T>(PartyVM vm, string methodName) where T : Delegate
        {
            MethodInfo method = AccessTools.Method(typeof(PartyVM), methodName);
            if (method == null)
            {
                throw new MissingMethodException(typeof(PartyVM).FullName, methodName);
            }

            return (T)Delegate.CreateDelegate(typeof(T), vm, method);
        }
    }
}
