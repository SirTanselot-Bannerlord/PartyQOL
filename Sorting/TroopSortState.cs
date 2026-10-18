using System.Collections.Generic;
using System.Linq;

namespace PartyQOL.Sorting
{
    /// <summary>Active multi-key sort shared by the party screen and the sort popup.</summary>
    internal static class TroopSortState
    {
        /// <summary>Starter template for New Custom / Type factory: Type ↓, Tier ↓, Culture ↓, Name ↑.</summary>
        public static readonly SortKey[] DefaultKeys =
        {
            new SortKey(TroopSortMode.Type, ascending: false),
            new SortKey(TroopSortMode.Tier, ascending: false),
            new SortKey(TroopSortMode.Culture, ascending: false),
            new SortKey(TroopSortMode.Name, ascending: true)
        };

        private static List<SortKey> _keys = new List<SortKey>(DefaultKeys);
        private static List<string> _typeOrder = SortOrderDefaults.CopyTypeOrder();
        private static List<string> _cultureOrder = new List<string>();

        public static IReadOnlyList<SortKey> Keys => _keys;
        public static IReadOnlyList<string> TypeOrder => _typeOrder;
        public static IReadOnlyList<string> CultureOrder => _cultureOrder;

        public static void SetKeys(IEnumerable<SortKey> keys)
        {
            _keys = NormalizeModes(keys);
            if (_keys.Count == 0)
            {
                _keys.AddRange(DefaultKeys);
            }
        }

        public static void SetTypeOrder(IEnumerable<string> order)
        {
            _typeOrder = SortOrderDefaults.NormalizeTypeOrder(order);
        }

        public static void SetCultureOrder(IEnumerable<string> order)
        {
            _cultureOrder = SortOrderDefaults.NormalizeCultureOrder(order);
        }

        public static void SetAll(IEnumerable<SortKey> keys, IEnumerable<string> typeOrder, IEnumerable<string> cultureOrder)
        {
            SetKeys(keys);
            SetTypeOrder(typeOrder);
            SetCultureOrder(cultureOrder);
        }

        public static TroopSortMode[] AllModes()
        {
            return new[]
            {
                TroopSortMode.Type,
                TroopSortMode.Tier,
                TroopSortMode.MaxTier,
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
