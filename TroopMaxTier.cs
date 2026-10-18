using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace PartyQOL
{
    /// <summary>
    /// Highest <see cref="CharacterObject.Tier"/> reachable via the unit's upgrade tree
    /// (including the unit itself). Shared by Sort and Filter "Max Tier".
    /// </summary>
    internal static class TroopMaxTier
    {
        public static int Get(CharacterObject character)
        {
            if (character == null)
            {
                return 0;
            }

            return Walk(character, new HashSet<CharacterObject>());
        }

        private static int Walk(CharacterObject character, HashSet<CharacterObject> visited)
        {
            if (character == null || !visited.Add(character))
            {
                return 0;
            }

            int max = character.Tier;
            CharacterObject[] targets = character.UpgradeTargets;
            if (targets == null || targets.Length == 0)
            {
                return max;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                CharacterObject next = targets[i];
                if (next != null)
                {
                    max = Math.Max(max, Walk(next, visited));
                }
            }

            return max;
        }
    }
}
