using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace PartyQOL.Upgrades
{
    public static class AutoUpgradeHelper
    {
        public static bool IsControlDown()
        {
            return Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
        }

        /// <summary>
        /// Click: goldCost == 0 only. Ctrl+click: any affordable ready upgrade via ExecuteUpgrade.
        /// </summary>
        public static void Run(PartyVM partyVM, bool freeOnly)
        {
            if (partyVM?.UpgradePopUp?.Troops == null)
            {
                return;
            }

            DesiredUpgradePathBehavior store = DesiredUpgradePathBehavior.Instance;
            PartyBase party = PartyBase.MainParty;
            if (party == null)
            {
                return;
            }

            // Snapshot so list mutations mid-loop don't skip rows unpredictably.
            List<PartyTroopManagerItemVM> rows = partyVM.UpgradePopUp.Troops.ToList();
            foreach (PartyTroopManagerItemVM row in rows)
            {
                PartyCharacterVM troop = row?.PartyCharacter;
                CharacterObject character = troop?.Character;
                if (character == null || character.IsHero)
                {
                    continue;
                }

                int index = store?.ResolvePreferredIndex(character) ?? -1;
                if (index < 0)
                {
                    // Single-path troops without an explicit preference still upgrade.
                    if (character.UpgradeTargets != null && character.UpgradeTargets.Length == 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (troop.Upgrades == null || index >= troop.Upgrades.Count)
                {
                    continue;
                }

                UpgradeTargetVM targetVm = troop.Upgrades[index];
                if (targetVm == null || targetVm.AvailableUpgrades <= 0 || !targetVm.IsAvailable)
                {
                    continue;
                }

                if (freeOnly && character.GetUpgradeGoldCost(party, index) != 0)
                {
                    continue;
                }

                partyVM.ExecuteUpgrade(troop, index, targetVm.AvailableUpgrades);
            }
        }
    }
}
