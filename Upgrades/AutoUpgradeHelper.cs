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
        public static bool IsShiftDown() =>
            Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);

        /// <summary>
        /// Upgrades every ready troop that has a single path, or a Ctrl+RMB preferred path.
        /// Multi-path troops with no preference are skipped.
        /// When <paramref name="zeroCostOnly"/> is true, upgrades one-at-a-time while
        /// GetUpgradeGoldCost stays 0 (spoils can make only a prefix of the stack free).
        /// </summary>
        public static void Run(PartyVM partyVM, bool zeroCostOnly = false)
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

            var jobs = new List<(string CharacterId, int UpgradeIndex)>();
            foreach (PartyTroopManagerItemVM row in partyVM.UpgradePopUp.Troops.ToList())
            {
                PartyCharacterVM troop = row?.PartyCharacter;
                CharacterObject character = troop?.Character;
                if (character == null || character.IsHero || string.IsNullOrEmpty(character.StringId))
                {
                    continue;
                }

                int index = ResolveUpgradeIndex(character, store);
                if (index < 0)
                {
                    continue;
                }

                jobs.Add((character.StringId, index));
            }

            if (jobs.Count == 0)
            {
                return;
            }

            bool previousEntire = partyVM.IsEntireStackModifierActive;
            bool previousFive = partyVM.IsFiveStackModifierActive;
            partyVM.IsFiveStackModifierActive = false;
            try
            {
                foreach ((string characterId, int upgradeIndex) in jobs)
                {
                    if (zeroCostOnly)
                    {
                        UpgradeZeroCostPrefix(partyVM, party, characterId, upgradeIndex);
                    }
                    else
                    {
                        UpgradeFullStack(partyVM, characterId, upgradeIndex);
                    }
                }
            }
            finally
            {
                partyVM.IsEntireStackModifierActive = previousEntire;
                partyVM.IsFiveStackModifierActive = previousFive;
            }
        }

        private static void UpgradeFullStack(PartyVM partyVm, string characterId, int upgradeIndex)
        {
            partyVm.IsEntireStackModifierActive = true;
            PartyCharacterVM troop = FindUpgradeableTroop(partyVm, characterId);
            if (troop?.Upgrades == null || upgradeIndex >= troop.Upgrades.Count)
            {
                return;
            }

            UpgradeTargetVM targetVm = troop.Upgrades[upgradeIndex];
            if (targetVm == null || targetVm.AvailableUpgrades <= 0 || !targetVm.IsAvailable)
            {
                return;
            }

            partyVm.ExecuteUpgrade(troop, upgradeIndex, targetVm.AvailableUpgrades);
        }

        /// <summary>
        /// Spoils / mounts can make only the first N upgrades free. Re-check gold cost after each unit.
        /// </summary>
        private static void UpgradeZeroCostPrefix(
            PartyVM partyVm,
            PartyBase party,
            string characterId,
            int upgradeIndex)
        {
            partyVm.IsEntireStackModifierActive = false;

            // Hard cap: AvailableUpgrades at start; stop earlier when cost becomes non-zero.
            for (int safety = 0; safety < 512; safety++)
            {
                PartyCharacterVM troop = FindUpgradeableTroop(partyVm, characterId);
                CharacterObject character = troop?.Character;
                if (character == null || troop.Upgrades == null || upgradeIndex >= troop.Upgrades.Count)
                {
                    return;
                }

                UpgradeTargetVM targetVm = troop.Upgrades[upgradeIndex];
                if (targetVm == null || targetVm.AvailableUpgrades <= 0 || !targetVm.IsAvailable)
                {
                    return;
                }

                if (character.GetUpgradeGoldCost(party, upgradeIndex) != 0)
                {
                    return;
                }

                partyVm.ExecuteUpgrade(troop, upgradeIndex, 1);
            }
        }

        private static PartyCharacterVM FindUpgradeableTroop(PartyVM partyVm, string characterId)
        {
            MBBindingList<PartyTroopManagerItemVM> troops = partyVm?.UpgradePopUp?.Troops;
            if (troops == null)
            {
                return null;
            }

            foreach (PartyTroopManagerItemVM row in troops)
            {
                PartyCharacterVM troop = row?.PartyCharacter;
                if (troop?.Character != null
                    && string.Equals(troop.Character.StringId, characterId, System.StringComparison.Ordinal)
                    && troop.NumOfReadyToUpgradeTroops > 0)
                {
                    return troop;
                }
            }

            return null;
        }

        private static int ResolveUpgradeIndex(CharacterObject character, DesiredUpgradePathBehavior store)
        {
            if (character.UpgradeTargets == null || character.UpgradeTargets.Length == 0)
            {
                return -1;
            }

            if (character.UpgradeTargets.Length == 1)
            {
                return 0;
            }

            return store?.ResolvePreferredIndex(character) ?? -1;
        }
    }
}
