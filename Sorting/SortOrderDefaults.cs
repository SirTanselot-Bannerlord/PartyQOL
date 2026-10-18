using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace PartyQOL.Sorting
{
    internal static class SortOrderDefaults
    {
        /// <summary>Default Type order (Desc = top first): horse archers, cavalry, archers, infantry.</summary>
        public static readonly string[] TypeOrder =
        {
            nameof(FormationClass.HorseArcher),
            nameof(FormationClass.Cavalry),
            nameof(FormationClass.Ranged),
            nameof(FormationClass.Infantry)
        };

        public static List<string> CopyTypeOrder() => new List<string>(TypeOrder);

        public static List<string> NormalizeTypeOrder(IEnumerable<string> raw)
        {
            var result = new List<string>();
            if (raw != null)
            {
                foreach (string id in raw)
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    string name = id.Trim();
                    if (!Enum.TryParse(name, true, out FormationClass _) || result.Exists(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    result.Add(Enum.Parse(typeof(FormationClass), name, true).ToString());
                }
            }

            foreach (string fallback in TypeOrder)
            {
                if (!result.Exists(x => string.Equals(x, fallback, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(fallback);
                }
            }

            return result;
        }

        public static List<string> NormalizeCultureOrder(IEnumerable<string> raw)
        {
            var result = new List<string>();
            if (raw == null)
            {
                return MergeAllCultures(result);
            }

            foreach (string id in raw)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                string name = id.Trim();
                if (!result.Exists(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(name);
                }
            }

            return MergeAllCultures(result);
        }

        /// <summary>All campaign cultures (not just those currently in the party).</summary>
        public static List<string> AllCultureIds()
        {
            return MergeAllCultures(new List<string>());
        }

        public static string CultureDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            CultureObject culture = Campaign.Current?.ObjectManager?.GetObject<CultureObject>(id);
            string name = culture?.Name?.ToString();
            return string.IsNullOrWhiteSpace(name) ? id : name;
        }

        private static List<string> MergeAllCultures(List<string> existing)
        {
            var result = existing ?? new List<string>();
            if (Campaign.Current?.ObjectManager == null)
            {
                return result;
            }

            foreach (CultureObject culture in Campaign.Current.ObjectManager.GetObjectTypeList<CultureObject>())
            {
                if (culture == null || string.IsNullOrEmpty(culture.StringId))
                {
                    continue;
                }

                // Skip pure bandit factions; keep minor/main cultures (aserai, battania, nord, …).
                if (culture.IsBandit && !culture.IsMainCulture)
                {
                    continue;
                }

                if (!result.Exists(x => string.Equals(x, culture.StringId, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(culture.StringId);
                }
            }

            // Keep caller order; only append missing cultures (sort presets rely on this).
            return result;
        }

        /// <summary>
        /// Player culture, then kingdom culture (if different), then main cultures, then the rest A–Z.
        /// Used by Filter culture pickers — not by Sort culture-order chips.
        /// </summary>
        public static List<string> OrderCulturesForUi(IEnumerable<string> ids)
        {
            var unique = new List<string>();
            if (ids != null)
            {
                foreach (string id in ids)
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    string trim = id.Trim();
                    if (!unique.Exists(x => string.Equals(x, trim, StringComparison.OrdinalIgnoreCase)))
                    {
                        unique.Add(trim);
                    }
                }
            }

            string playerCulture = Hero.MainHero?.Culture?.StringId;
            string kingdomCulture = Hero.MainHero?.MapFaction?.Culture?.StringId
                                    ?? Hero.MainHero?.Clan?.Kingdom?.Culture?.StringId;

            var ordered = new List<string>();
            void Take(string id)
            {
                if (string.IsNullOrEmpty(id))
                {
                    return;
                }

                int idx = unique.FindIndex(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    ordered.Add(unique[idx]);
                    unique.RemoveAt(idx);
                }
            }

            Take(playerCulture);
            if (!string.Equals(kingdomCulture, playerCulture, StringComparison.OrdinalIgnoreCase))
            {
                Take(kingdomCulture);
            }

            // Stable main-faction order after the player/kingdom pins.
            foreach (string main in new[] { "empire", "aserai", "battania", "khuzait", "sturgia", "vlandia", "nord" })
            {
                Take(main);
            }

            // Remaining main cultures from the object manager, then everything else.
            if (Campaign.Current?.ObjectManager != null)
            {
                foreach (CultureObject culture in Campaign.Current.ObjectManager.GetObjectTypeList<CultureObject>())
                {
                    if (culture != null && culture.IsMainCulture)
                    {
                        Take(culture.StringId);
                    }
                }
            }

            unique.Sort(StringComparer.OrdinalIgnoreCase);
            ordered.AddRange(unique);
            return ordered;
        }
    }
}
