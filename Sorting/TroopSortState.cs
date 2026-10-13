using System.Collections.Generic;
using System.Linq;

namespace PartyQOL.Sorting
{
    /// <summary>Active multi-key sort shared by the party screen and the sort popup.</summary>
    internal static class TroopSortState
    {
        /// <summary>Starter template for New Custom: Type ↓, Tier ↓, Name ↑.</summary>
        public static readonly SortKey[] DefaultKeys =
        {
            new SortKey(TroopSortMode.Type, ascending: false),
            new SortKey(TroopSortMode.Tier, ascending: false),
            new SortKey(TroopSortMode.Name, ascending: true)
        };

        private static List<SortKey> _keys = new List<SortKey>(DefaultKeys);

        public static IReadOnlyList<SortKey> Keys => _keys;

        public static void SetKeys(IEnumerable<SortKey> keys)
        {
            _keys = NormalizeModes(keys);
            if (_keys.Count == 0)
            {
                _keys.AddRange(DefaultKeys);
            }
        }

        public static TroopSortMode[] AllModes()
        {
            return new[]
            {
                TroopSortMode.Type,
                TroopSortMode.Tier,
                TroopSortMode.Level,
                TroopSortMode.Culture,
                TroopSortMode.Name,
                TroopSortMode.Count
            };
        }

        public static List<SortKey> NormalizeModes(IEnumerable<SortKey> keys)
        {
            var result = new List<SortKey>();
            if (keys == null)
            {
                return result;
            }

            foreach (SortKey key in keys)
            {
                if (result.All(k => k.Mode != key.Mode))
                {
                    result.Add(key);
                }
            }

            return result;
        }
    }
}
