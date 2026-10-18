using System;
using System.Reflection;
using HarmonyLib;
using PartyQOL.UI;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;

namespace PartyQOL.Filtering
{
    internal static class TroopFilterApplicator
    {
        public static void ApplyPartyScreen(PartyVM partyVm)
        {
            if (partyVm == null)
            {
                return;
            }

            // Vanilla copies the cached PartySizeLimit once, at open. Refresh it so the header
            // and the transfer cap match the live hover (sea bonus on the water, land total in port).
            RefreshMemberLimits(partyVm.PartyScreenLogic);

            ApplyList(partyVm.MainPartyTroops);
            ApplyList(partyVm.MainPartyPrisoners);
            ApplyList(partyVm.OtherPartyTroops);
            ApplyList(partyVm.OtherPartyPrisoners);

            bool mainChanged = UpdateTroopsLabel(partyVm, isMain: true);
            bool otherChanged = UpdateTroopsLabel(partyVm, isMain: false);
            PartyVMMixin.Current?.SyncFilterActiveFlag(mainChanged, otherChanged);

            partyVm.MainPartyComposition?.RefreshCounts(partyVm.MainPartyTroops);
            partyVm.OtherPartyComposition?.RefreshCounts(partyVm.OtherPartyTroops);

            PartyScreenLogic logic = partyVm.PartyScreenLogic;
            if (logic != null)
            {
                partyVm.IsMainTroopsLimitWarningEnabled =
                    logic.RightPartyMembersSizeLimit < logic.MemberRosters[(int)PartyScreenLogic.PartyRosterSide.Right].TotalManCount
                    && partyVm.AreMembersRelevantOnCurrentMode;
            }
        }

        private static void RefreshMemberLimits(PartyScreenLogic logic)
        {
            if (logic == null)
            {
                return;
            }

            SetMemberLimit(logic, "RightPartyMembersSizeLimit", PairPartyScreen.LiveMemberLimit(logic.RightOwnerParty));
            if (logic.LeftOwnerParty != null)
            {
                SetMemberLimit(logic, "LeftPartyMembersSizeLimit", PairPartyScreen.LiveMemberLimit(logic.LeftOwnerParty));
            }
        }

        private static void SetMemberLimit(PartyScreenLogic logic, string propertyName, int value)
        {
            PropertyInfo property = AccessTools.Property(typeof(PartyScreenLogic), propertyName);
            MethodInfo setter = property?.GetSetMethod(true);
            setter?.Invoke(logic, new object[] { value });
        }

        private static void ApplyList(MBBindingList<PartyCharacterVM> list)
        {
            if (list == null)
            {
                return;
            }

            foreach (PartyCharacterVM item in list)
            {
                bool hidden = item?.Character != null
                              && !FilterState.Passes(item.Character, item.Number, item.WoundedCount);
                PartyCharacterVMMixin.SetFiltered(item, hidden);
                item?.OnPropertyChanged(nameof(PartyCharacterVM.TroopNum));
            }
        }

        /// <summary>Returns true when a filter is active and the visible headcount differs from the full headcount.</summary>
        private static bool UpdateTroopsLabel(PartyVM partyVm, bool isMain)
        {
            MBBindingList<PartyCharacterVM> list = isMain ? partyVm.MainPartyTroops : partyVm.OtherPartyTroops;
            if (list == null)
            {
                return false;
            }

            PartyScreenLogic logic = partyVm.PartyScreenLogic;
            int limit = isMain
                ? (logic?.RightPartyMembersSizeLimit ?? 0)
                : (logic?.LeftPartyMembersSizeLimit ?? 0);

            if (!FilterState.HasActive)
            {
                // Restore vanilla label (never reuse a previously written filtered "N / N").
                string restored = PopulateVanillaListLabel(list, limit);
                if (isMain)
                {
                    partyVm.MainPartyTroopsLbl = restored;
                }
                else
                {
                    partyVm.OtherPartyTroopsLbl = restored;
                }

                return false;
            }

            int full = 0;
            int visible = 0;
            foreach (PartyCharacterVM item in list)
            {
                if (item?.Character == null)
                {
                    continue;
                }

                int n = item.Character.IsHero ? 1 : Math.Max(0, item.Number);
                full += n;
                if (item.Character.IsHero || FilterState.Passes(item.Character, item.Number, item.WoundedCount))
                {
                    visible += item.Character.IsHero
                        ? 1
                        : FilterState.VisibleCount(item.Number, item.WoundedCount);
                }
            }

            if (limit <= 0)
            {
                limit = full;
            }

            // Filter active: Viewed / Total / Limit (red via Is*TroopsLblFiltered when Viewed != Total).
            string next = visible + " / " + full + " / " + limit;
            if (isMain)
            {
                partyVm.MainPartyTroopsLbl = next;
            }
            else
            {
                partyVm.OtherPartyTroopsLbl = next;
            }

            return visible != full;
        }

        private static string PopulateVanillaListLabel(MBBindingList<PartyCharacterVM> list, int limit)
        {
            try
            {
                string label = Traverse.Create(typeof(PartyVM))
                    .Method("PopulatePartyListLabel", list, limit)
                    .GetValue<string>();
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }
            }
            catch
            {
                // Fall through to a simple COUNT / MAX format.
            }

            int count = 0;
            foreach (PartyCharacterVM item in list)
            {
                if (item?.Character == null)
                {
                    continue;
                }

                count += item.Character.IsHero ? 1 : Math.Max(0, item.Number);
            }

            return limit > 0 ? count + " / " + limit : count.ToString();
        }
    }
}
