using System.Collections.Generic;
using System.Linq;
using PartyQOL.Sorting;
using TaleWorlds.CampaignSystem;

namespace PartyQOL.Filtering
{
    /// <summary>Active filter applied to the party screen (mirrors TroopSortState).</summary>
    internal static class FilterState
    {
        public static bool HasActive { get; private set; }

        public static bool UseType { get; private set; } = true;
        public static bool UseCulture { get; private set; } = true;
        public static bool UseTier { get; private set; }
        public static bool UseMaxTier { get; private set; }
        public static bool UseLevel { get; private set; }
        public static bool UseCount { get; private set; }
        public static bool UseCondition { get; private set; }
        public static ConditionFilterMode ConditionMode { get; private set; } = ConditionFilterMode.Off;

        public static List<FilterToggleItem> Types { get; private set; } = DefaultTypes();
        public static List<FilterToggleItem> Cultures { get; private set; } = new List<FilterToggleItem>();
        public static int TierMin { get; private set; }
        public static int TierMax { get; private set; } = 6;
        public static int MaxTierMin { get; private set; }
        public static int MaxTierMax { get; private set; } = 6;
        public static int LevelMin { get; private set; } = 1;
        public static int LevelMax { get; private set; } = 99;
        public static int CountMin { get; private set; } = 1;
        public static int CountMax { get; private set; } = 999;

        public static void Clear()
        {
            HasActive = false;
            UseType = true;
            UseCulture = true;
            UseTier = false;
            UseMaxTier = false;
            UseLevel = false;
            UseCount = false;
            UseCondition = false;
            ConditionMode = ConditionFilterMode.Off;
            Types = DefaultTypes();
            Cultures = DefaultCultures();
            TierMin = 0;
            TierMax = 6;
            MaxTierMin = 0;
            MaxTierMax = 6;
            LevelMin = 1;
            LevelMax = 99;
            CountMin = 1;
            CountMax = 999;
        }

        public static void ApplyFrom(FilterPreset preset)
        {
            if (preset == null)
            {
                Clear();
                return;
            }

            HasActive = true;
            UseType = preset.UseType;
            UseCulture = preset.UseCulture;
            UseTier = preset.UseTier;
            UseMaxTier = preset.UseMaxTier;
            UseLevel = preset.UseLevel;
            UseCount = preset.UseCount;
            UseCondition = preset.UseCondition;
            ConditionMode = preset.ConditionMode;
            Types = preset.Types.Select(t => t.Clone()).ToList();
            Cultures = preset.Cultures.Select(c => c.Clone()).ToList();
            TierMin = preset.TierMin;
            TierMax = preset.TierMax;
            MaxTierMin = preset.MaxTierMin;
            MaxTierMax = preset.MaxTierMax;
            LevelMin = preset.LevelMin;
            LevelMax = preset.LevelMax;
            CountMin = preset.CountMin;
            CountMax = preset.CountMax;
        }

        /// <summary>Headcount to display for this stack. Heroes stay at 1.</summary>
        public static int VisibleCount(int stackCount, int woundedCount)
        {
            int total = stackCount < 0 ? 0 : stackCount;
            int wounded = woundedCount < 0 ? 0 : woundedCount;
            if (wounded > total)
            {
                wounded = total;
            }

            if (!ShowsConditionCount)
            {
                return total;
            }

            return ConditionMode == ConditionFilterMode.Wounded ? wounded : total - wounded;
        }

        public static bool ShowsConditionCount =>
            HasActive && UseCondition && ConditionMode != ConditionFilterMode.Off;

        /// <summary>
        /// How many bodies a transfer should move while Wounded or Healthy is showing.
        /// Healthy moves that many unwounded; Wounded moves that many wounded.
        /// </summary>
        public static bool TryConditionTransfer(
            int stackCount,
            int woundedCount,
            int requested,
            out int amount,
            out int woundedToMove)
        {
            amount = 0;
            woundedToMove = 0;
            if (!ShowsConditionCount || requested <= 0)
            {
                return false;
            }

            int visible = VisibleCount(stackCount, woundedCount);
            if (visible <= 0)
            {
                return false;
            }

            amount = requested < visible ? requested : visible;
            woundedToMove = ConditionMode == ConditionFilterMode.Wounded ? amount : 0;
            return true;
        }

        public static bool Passes(CharacterObject character, int stackCount = 1, int woundedCount = 0)
        {
            if (!HasActive || character == null)
            {
                return true;
            }

            // Heroes always remain visible (same spirit as sort).
            if (character.IsHero)
            {
                return true;
            }

            if (UseType)
            {
                string typeId = character.DefaultFormationClass.ToString();
                FilterToggleItem match = Types.FirstOrDefault(t => string.Equals(t.Id, typeId, System.StringComparison.OrdinalIgnoreCase));
                if (match == null || !match.Enabled)
                {
                    return false;
                }
            }

            if (UseCulture)
            {
                string cultureId = character.Culture?.StringId ?? string.Empty;
                if (Cultures.Count > 0)
                {
                    FilterToggleItem match = Cultures.FirstOrDefault(c => string.Equals(c.Id, cultureId, System.StringComparison.OrdinalIgnoreCase));
                    if (match == null || !match.Enabled)
                    {
                        return false;
                    }
                }
            }

            if (UseTier)
            {
                int tier = character.Tier;
                if (tier < TierMin || tier > TierMax)
                {
                    return false;
                }
            }

            if (UseMaxTier)
            {
                int maxTier = TroopMaxTier.Get(character);
                if (maxTier < MaxTierMin || maxTier > MaxTierMax)
                {
                    return false;
                }
            }

            if (UseLevel)
            {
                int level = character.Level;
                if (level < LevelMin || level > LevelMax)
                {
                    return false;
                }
            }

            if (UseCount)
            {
                if (stackCount < CountMin || stackCount > CountMax)
                {
                    return false;
                }
            }

            if (ShowsConditionCount && VisibleCount(stackCount, woundedCount) <= 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>True when the formation is explicitly excluded by the active Type filter.</summary>
        public static bool IsFormationExcluded(string typeId)
        {
            if (!HasActive || !UseType || string.IsNullOrEmpty(typeId))
            {
                return false;
            }

            FilterToggleItem match = Types.FirstOrDefault(t => SameType(t.Id, typeId));
            return match == null || !match.Enabled;
        }

        /// <summary>Left-click: flip one formation. Starts a live type filter when none is active.</summary>
        public static void ToggleFormation(string typeId)
        {
            BeginLiveTypeFilter();
            FilterToggleItem item = EnsureType(typeId);
            item.Enabled = !item.Enabled;
            CollapseIfIdle();
        }

        /// <summary>
        /// Right-click: exclude every other formation. If those are already excluded, enable them again.
        /// The clicked formation is left as it is.
        /// </summary>
        public static void IsolateFormation(string typeId)
        {
            bool othersExcluded = HasActive
                                  && UseType
                                  && Types.Any(t => !SameType(t.Id, typeId))
                                  && Types.Where(t => !SameType(t.Id, typeId)).All(t => !t.Enabled);
            BeginLiveTypeFilter();
            EnsureType(typeId);
            foreach (FilterToggleItem type in Types)
            {
                if (!SameType(type.Id, typeId))
                {
                    type.Enabled = othersExcluded;
                }
            }

            CollapseIfIdle();
        }

        private static void BeginLiveTypeFilter()
        {
            if (!HasActive)
            {
                HasActive = true;
                UseType = true;
                UseCulture = false;
                UseTier = false;
                UseMaxTier = false;
                UseLevel = false;
                UseCount = false;
                UseCondition = false;
                Types = DefaultTypes();
                return;
            }

            UseType = true;
            foreach (string id in SortOrderDefaults.TypeOrder)
            {
                EnsureType(id);
            }
        }

        private static FilterToggleItem EnsureType(string typeId)
        {
            FilterToggleItem match = Types.FirstOrDefault(t => SameType(t.Id, typeId));
            if (match != null)
            {
                return match;
            }

            match = new FilterToggleItem { Id = typeId, Enabled = true };
            Types.Add(match);
            return match;
        }

        /// <summary>Drop a composition-only live filter once every formation is included again.</summary>
        private static void CollapseIfIdle()
        {
            if (FilterPresets.HasActiveSaved)
            {
                return;
            }

            if (UseCulture || UseTier || UseMaxTier || UseLevel || UseCount || UseCondition)
            {
                return;
            }

            if (Types.Exists(t => !t.Enabled))
            {
                return;
            }

            Clear();
        }

        private static bool SameType(string a, string b) =>
            string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);

        public static List<FilterToggleItem> DefaultTypes()
        {
            return SortOrderDefaults.TypeOrder
                .Select(id => new FilterToggleItem { Id = id, Enabled = true })
                .ToList();
        }

        public static List<FilterToggleItem> DefaultCultures()
        {
            return SortOrderDefaults.OrderCulturesForUi(SortOrderDefaults.AllCultureIds())
                .Select(id => new FilterToggleItem { Id = id, Enabled = true })
                .ToList();
        }
    }
}
