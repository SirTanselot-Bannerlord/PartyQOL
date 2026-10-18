using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem.Party;

namespace PartyQOL
{
    /// <summary>
    /// Town menu "manage garrison" / "manage prisoners" both open the same combined
    /// left view: garrison members + settlement dungeon prisoners.
    /// </summary>
    internal static class PartyScreenGarrisonPatches
    {
        [HarmonyPatch(typeof(PartyScreenHelper), nameof(PartyScreenHelper.OpenScreenAsManageTroops))]
        private static class ManageTroopsPatch
        {
            private static bool Prefix(MobileParty leftParty)
            {
                if (!AbsorbPartyService.IsManageableGarrisonAtPlayerLocation(leftParty))
                {
                    return true;
                }

                PairPartyScreen.Open(leftParty, MobileParty.MainParty);
                return false;
            }
        }

        [HarmonyPatch(typeof(PartyScreenHelper), nameof(PartyScreenHelper.OpenScreenAsManagePrisoners))]
        private static class ManagePrisonersPatch
        {
            private static bool Prefix()
            {
                MobileParty garrison = AbsorbPartyService.GetManageableGarrisonAtPlayerLocation();
                if (garrison == null)
                {
                    return true;
                }

                PairPartyScreen.Open(garrison, MobileParty.MainParty);
                return false;
            }
        }

        [HarmonyPatch(typeof(PartyScreenHelper), nameof(PartyScreenHelper.OpenScreenAsManageTroopsAndPrisoners))]
        private static class ManageTroopsAndPrisonersPatch
        {
            private static bool Prefix(MobileParty leftParty)
            {
                if (!AbsorbPartyService.IsManageableGarrisonAtPlayerLocation(leftParty))
                {
                    return true;
                }

                PairPartyScreen.Open(leftParty, MobileParty.MainParty);
                return false;
            }
        }
    }
}
