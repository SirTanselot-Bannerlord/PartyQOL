using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.Library;

namespace PartyQOL.Sorting
{
    internal sealed class SortPreset
    {
        public string Name;
        public List<SortKey> Keys = new List<SortKey>();
        public List<string> TypeOrder = SortOrderDefaults.CopyTypeOrder();
        public List<string> CultureOrder = new List<string>();
    }

    /// <summary>
    /// Named multi-key troop sort orders. No built-in Default — unsaved work is a session draft ("New Custom").
    /// </summary>
    internal static class SortPresets
    {
        internal const int MaxCount = 16;
        internal const int MaxNameLength = 22;
        internal const string NewCustomName = "New Custom";

        private static readonly List<SortPreset> Items = new List<SortPreset>();
        private static bool _loaded;
        private static string _activeName = string.Empty;

        internal static string ActiveName
        {
            get
            {
                EnsureLoaded();
                return _activeName ?? string.Empty;
            }
        }

        internal static bool HasActiveSaved
        {
            get
            {
                EnsureLoaded();
                return Find(_activeName) != null;
            }
        }

        internal static IReadOnlyList<SortPreset> All
        {
            get
            {
                EnsureLoaded();
                return Items;
            }
        }

        internal static void EnsureLoadedPublic() => EnsureLoaded();

        internal static bool TryGet(string name, out SortPreset preset)
        {
            EnsureLoaded();
            preset = Find(name);
            return preset != null;
        }

        internal static bool Activate(string name)
        {
            EnsureLoaded();
            SortPreset preset = Find(name);
            if (preset == null)
            {
                return false;
            }

            _activeName = preset.Name;
            TroopSortState.SetAll(preset.Keys, preset.TypeOrder, preset.CultureOrder);
            return true;
        }

        internal static void ClearActive()
        {
            EnsureLoaded();
            _activeName = string.Empty;
        }

        internal static bool TrySaveAs(
            string rawName,
            IReadOnlyList<SortKey> keys,
            IReadOnlyList<string> typeOrder,
            IReadOnlyList<string> cultureOrder,
            out string savedName,
            out string error)
        {
            savedName = null;
            error = null;
            if (!TryNormalize(rawName, out string name, out error))
            {
                return false;
            }

            if (string.Equals(name, NewCustomName, StringComparison.OrdinalIgnoreCase))
            {
                error = "Choose a different name than New Custom.";
                return false;
            }

            EnsureLoaded();
            List<SortKey> normalizedKeys = Normalize(keys);
            List<string> types = SortOrderDefaults.NormalizeTypeOrder(typeOrder);
            List<string> cultures = SortOrderDefaults.NormalizeCultureOrder(cultureOrder);
            SortPreset existing = Find(name);
            if (existing != null)
            {
                existing.Keys = normalizedKeys;
                existing.TypeOrder = types;
                existing.CultureOrder = cultures;
            }
            else
            {
                if (Items.Count >= MaxCount)
                {
                    error = "The list holds 16 sort orders. Delete one first.";
                    return false;
                }

                Items.Add(new SortPreset
                {
                    Name = name,
                    Keys = normalizedKeys,
                    TypeOrder = types,
                    CultureOrder = cultures
                });
            }

            // Persist only. Activate via the party-bar sort selector (same as Filter Save).
            if (string.Equals(_activeName, name, StringComparison.OrdinalIgnoreCase))
            {
                TroopSortState.SetAll(normalizedKeys, types, cultures);
            }

            if (!TryWrite())
            {
                error = "Could not save sort orders.";
                return false;
            }

            savedName = name;
            return true;
        }

        internal static bool TryRename(string rawOldName, string rawNewName, out string savedName, out string error)
        {
            savedName = null;
            error = null;
            if (!TryNormalize(rawOldName, out string oldName, out error)
                || !TryNormalize(rawNewName, out string newName, out error))
            {
                return false;
            }

            if (IsFactoryName(oldName))
            {
                error = "Factory orders cannot be renamed.";
                return false;
            }

            if (IsFactoryName(newName) || string.Equals(newName, NewCustomName, StringComparison.OrdinalIgnoreCase))
            {
                error = "Choose a different name.";
                return false;
            }

            EnsureLoaded();
            SortPreset preset = Find(oldName);
            if (preset == null)
            {
                error = "No saved order with that name.";
                return false;
            }

            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) && Find(newName) != null)
            {
                error = "A sort order with that name already exists.";
                return false;
            }

            preset.Name = newName;
            if (string.Equals(_activeName, oldName, StringComparison.OrdinalIgnoreCase))
            {
                _activeName = newName;
            }

            if (!TryWrite())
            {
                error = "Could not save sort orders.";
                return false;
            }

            savedName = newName;
            return true;
        }

        internal static bool TryDelete(string rawName, out string error)
        {
            error = null;
            EnsureLoaded();
            if (!TryNormalize(rawName, out string name, out error))
            {
                return false;
            }

            if (IsFactoryName(name))
            {
                error = "Factory orders cannot be deleted.";
                return false;
            }

            SortPreset preset = Find(name);
            if (preset == null)
            {
                error = "No saved order with that name.";
                return false;
            }

            Items.Remove(preset);
            if (string.Equals(_activeName, name, StringComparison.OrdinalIgnoreCase))
            {
                _activeName = string.Empty;
                TroopSortState.SetAll(TroopSortState.DefaultKeys, SortOrderDefaults.TypeOrder, Array.Empty<string>());
            }

            if (!TryWrite())
            {
                error = "Could not save sort orders.";
                return false;
            }

            return true;
        }

        internal static string NextFreeName()
        {
            EnsureLoaded();
            for (int i = 1; i <= MaxCount + 2; i++)
            {
                string candidate = "Sort " + i;
                if (Find(candidate) == null)
                {
                    return candidate;
                }
            }

            return "Sort";
        }

        private static List<SortKey> Normalize(IReadOnlyList<SortKey> keys)
        {
            var result = new List<SortKey>();
            if (keys != null)
            {
                foreach (SortKey key in keys)
                {
                    if (result.All(k => k.Mode != key.Mode))
                    {
                        result.Add(key);
                    }
                }
            }

            if (result.Count == 0)
            {
                result.AddRange(TroopSortState.DefaultKeys);
            }

            return result;
        }

        private static SortPreset Find(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (string.Equals(Items[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return Items[i];
                }
            }

            return null;
        }

        private static bool TryNormalize(string rawName, out string name, out string error)
        {
            name = (rawName ?? string.Empty).Trim().Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            while (name.Contains("  "))
            {
                name = name.Replace("  ", " ");
            }

            name = name.Replace("{", string.Empty).Replace("}", string.Empty);
            if (name.Length == 0)
            {
                error = "Type a name first.";
                return false;
            }

            if (name.Length > MaxNameLength)
            {
                name = name.Substring(0, MaxNameLength).Trim();
            }

            if (name.Length == 0)
            {
                error = "Type a name first.";
                return false;
            }

            error = null;
            return true;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            try
            {
                string path = FilePath;
                if (File.Exists(path))
                {
                    foreach (string line in File.ReadAllLines(path))
                    {
                        if (Items.Count >= MaxCount || string.IsNullOrWhiteSpace(line) || line[0] == '#')
                        {
                            continue;
                        }

                        int tab = line.IndexOf('\t');
                        if (tab <= 0 || tab >= line.Length - 1)
                        {
                            continue;
                        }

                        if (!TryNormalize(line.Substring(0, tab), out string name, out _) || Find(name) != null)
                        {
                            continue;
                        }

                        // Drop the old auto-created Default entry if present with starter keys only — keep if user edited name.
                        if (string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase))
                        {
                            // Keep as a normal saved preset so existing users are not wiped; they can delete it.
                        }

                        Items.Add(ParsePreset(name, line.Substring(tab + 1)));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("PartyQOL: could not read sort presets (" + e.Message + ")");
            }

            EnsureStarterPresets();

            if (Find(_activeName) == null)
            {
                _activeName = Find("Type") != null ? "Type" : (Items.Count > 0 ? Items[0].Name : string.Empty);
                SortPreset active = Find(_activeName);
                if (active != null)
                {
                    TroopSortState.SetAll(active.Keys, active.TypeOrder, active.CultureOrder);
                }
            }
        }

        internal static bool IsFactoryName(string name) =>
            string.Equals(name, "Type", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Tier", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Max Tier", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Culture", StringComparison.OrdinalIgnoreCase);

        internal static SortPreset CreateFactoryPreset(string name)
        {
            if (string.Equals(name, "Tier", StringComparison.OrdinalIgnoreCase))
            {
                return new SortPreset
                {
                    Name = "Tier",
                    Keys = new List<SortKey>
                    {
                        new SortKey(TroopSortMode.Tier, ascending: false),
                        new SortKey(TroopSortMode.Type, ascending: false),
                        new SortKey(TroopSortMode.Culture, ascending: false),
                        new SortKey(TroopSortMode.Name, ascending: true)
                    },
                    TypeOrder = SortOrderDefaults.CopyTypeOrder(),
                    CultureOrder = SortOrderDefaults.AllCultureIds()
                };
            }

            if (string.Equals(name, "Max Tier", StringComparison.OrdinalIgnoreCase))
            {
                return new SortPreset
                {
                    Name = "Max Tier",
                    Keys = new List<SortKey>
                    {
                        new SortKey(TroopSortMode.MaxTier, ascending: false),
                        new SortKey(TroopSortMode.Type, ascending: false),
                        new SortKey(TroopSortMode.Culture, ascending: false),
                        new SortKey(TroopSortMode.Name, ascending: true)
                    },
                    TypeOrder = SortOrderDefaults.CopyTypeOrder(),
                    CultureOrder = SortOrderDefaults.AllCultureIds()
                };
            }

            if (string.Equals(name, "Culture", StringComparison.OrdinalIgnoreCase))
            {
                return new SortPreset
                {
                    Name = "Culture",
                    Keys = new List<SortKey>
                    {
                        new SortKey(TroopSortMode.Culture, ascending: false),
                        new SortKey(TroopSortMode.Type, ascending: false),
                        new SortKey(TroopSortMode.Tier, ascending: false),
                        new SortKey(TroopSortMode.Name, ascending: true)
                    },
                    TypeOrder = SortOrderDefaults.CopyTypeOrder(),
                    CultureOrder = SortOrderDefaults.AllCultureIds()
                };
            }

            // Type (default): Type > Tier > Culture > Name
            return new SortPreset
            {
                Name = "Type",
                Keys = new List<SortKey>(TroopSortState.DefaultKeys),
                TypeOrder = SortOrderDefaults.CopyTypeOrder(),
                CultureOrder = SortOrderDefaults.AllCultureIds()
            };
        }

        /// <summary>Upgrade unedited legacy starters to the new factory key lists.</summary>
        private static bool TryUpgradeFactoryPreset(SortPreset existing, string factoryName)
        {
            if (existing == null)
            {
                return false;
            }

            SortPreset factory = CreateFactoryPreset(factoryName);
            if (KeysEqual(existing.Keys, factory.Keys))
            {
                // Already current; still refresh culture list if empty.
                if (existing.CultureOrder == null || existing.CultureOrder.Count == 0)
                {
                    existing.CultureOrder = SortOrderDefaults.AllCultureIds();
                    return true;
                }

                return false;
            }

            // Same criteria order, updated directions (e.g. Culture Desc = top first).
            if (ModesEqual(existing.Keys, factory.Keys))
            {
                existing.Keys = factory.Keys;
                if (existing.CultureOrder == null || existing.CultureOrder.Count == 0)
                {
                    existing.CultureOrder = factory.CultureOrder;
                }

                return true;
            }

            // Legacy Type was Type+Tier+Name; Tier was Tier+Name; Culture was Culture+Tier+Name.
            bool legacyType = string.Equals(factoryName, "Type", StringComparison.OrdinalIgnoreCase)
                              && existing.Keys.Count == 3
                              && existing.Keys[0].Mode == TroopSortMode.Type
                              && existing.Keys[1].Mode == TroopSortMode.Tier
                              && existing.Keys[2].Mode == TroopSortMode.Name;
            bool legacyTier = string.Equals(factoryName, "Tier", StringComparison.OrdinalIgnoreCase)
                              && existing.Keys.Count == 2
                              && existing.Keys[0].Mode == TroopSortMode.Tier
                              && existing.Keys[1].Mode == TroopSortMode.Name;
            bool legacyCulture = string.Equals(factoryName, "Culture", StringComparison.OrdinalIgnoreCase)
                                 && existing.Keys.Count == 3
                                 && existing.Keys[0].Mode == TroopSortMode.Culture
                                 && existing.Keys[1].Mode == TroopSortMode.Tier
                                 && existing.Keys[2].Mode == TroopSortMode.Name;
            if (!legacyType && !legacyTier && !legacyCulture)
            {
                return false;
            }

            existing.Keys = factory.Keys;
            existing.TypeOrder = factory.TypeOrder;
            existing.CultureOrder = factory.CultureOrder;
            return true;
        }

        private static bool KeysEqual(IReadOnlyList<SortKey> a, IReadOnlyList<SortKey> b)
        {
            if (a == null || b == null || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Mode != b[i].Mode || a[i].Ascending != b[i].Ascending)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ModesEqual(IReadOnlyList<SortKey> a, IReadOnlyList<SortKey> b)
        {
            if (a == null || b == null || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Mode != b[i].Mode)
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnsureStarterPresets()
        {
            bool changed = false;
            // Rename legacy "Default" → "Type" (same starter keys).
            SortPreset legacyDefault = Find("Default");
            if (legacyDefault != null && Find("Type") == null)
            {
                legacyDefault.Name = "Type";
                if (string.Equals(_activeName, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    _activeName = "Type";
                }

                changed = true;
            }

            if (Find("Type") == null && Items.Count < MaxCount)
            {
                Items.Insert(0, CreateFactoryPreset("Type"));
                changed = true;
            }
            else if (TryUpgradeFactoryPreset(Find("Type"), "Type"))
            {
                changed = true;
            }

            if (Find("Tier") == null && Items.Count < MaxCount)
            {
                Items.Add(CreateFactoryPreset("Tier"));
                changed = true;
            }
            else if (TryUpgradeFactoryPreset(Find("Tier"), "Tier"))
            {
                changed = true;
            }

            if (Find("Max Tier") == null && Items.Count < MaxCount)
            {
                Items.Add(CreateFactoryPreset("Max Tier"));
                changed = true;
            }
            else if (TryUpgradeFactoryPreset(Find("Max Tier"), "Max Tier"))
            {
                changed = true;
            }

            if (Find("Culture") == null && Items.Count < MaxCount)
            {
                Items.Add(CreateFactoryPreset("Culture"));
                changed = true;
            }
            else if (TryUpgradeFactoryPreset(Find("Culture"), "Culture"))
            {
                changed = true;
            }

            if (changed)
            {
                if (string.IsNullOrEmpty(_activeName))
                {
                    _activeName = "Type";
                    SortPreset type = Find("Type");
                    if (type != null)
                    {
                        TroopSortState.SetAll(type.Keys, type.TypeOrder, type.CultureOrder);
                    }
                }

                TryWrite();
            }
        }

        private static SortPreset ParsePreset(string name, string raw)
        {
            string[] sections = raw.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            string keysRaw = sections.Length > 0 ? sections[0] : raw;
            var typeOrder = new List<string>();
            var cultureOrder = new List<string>();
            for (int i = 1; i < sections.Length; i++)
            {
                string section = sections[i];
                int eq = section.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = section.Substring(0, eq).Trim();
                string value = section.Substring(eq + 1).Trim();
                if (string.Equals(key, "TYPE", StringComparison.OrdinalIgnoreCase))
                {
                    typeOrder.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                }
                else if (string.Equals(key, "CULTURE", StringComparison.OrdinalIgnoreCase))
                {
                    cultureOrder.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                }
            }

            return new SortPreset
            {
                Name = name,
                Keys = ParseKeys(keysRaw),
                TypeOrder = SortOrderDefaults.NormalizeTypeOrder(typeOrder),
                CultureOrder = SortOrderDefaults.NormalizeCultureOrder(cultureOrder)
            };
        }

        private static List<SortKey> ParseKeys(string raw)
        {
            var keys = new List<SortKey>();
            foreach (string part in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Skip TYPE=/CULTURE= if someone saved old single-section lines oddly.
                if (part.IndexOf('=') >= 0)
                {
                    continue;
                }

                string[] bits = part.Trim().Split(':');
                if (bits.Length < 1)
                {
                    continue;
                }

                string modeName = bits[0].Trim();
                // Legacy Combat chip → Type (same role idea; Combat was removed).
                if (string.Equals(modeName, "Combat", StringComparison.OrdinalIgnoreCase))
                {
                    modeName = nameof(TroopSortMode.Type);
                }

                if (!Enum.TryParse(modeName, true, out TroopSortMode mode))
                {
                    continue;
                }

                bool ascending = bits.Length < 2 || bits[1].Trim() != "0";
                if (keys.All(k => k.Mode != mode))
                {
                    keys.Add(new SortKey(mode, ascending));
                }
            }

            return Normalize(keys);
        }

        private static bool TryWrite()
        {
            try
            {
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var lines = new List<string>
                {
                    "# PartyQOL sort presets. name<TAB>Mode:1|0,...<TAB>TYPE=Infantry,...<TAB>CULTURE=id,...  (1=asc, 0=desc)"
                };
                foreach (SortPreset preset in Items)
                {
                    string keys = string.Join(",", preset.Keys.Select(k => k.Mode + ":" + (k.Ascending ? "1" : "0")));
                    string types = string.Join(",", preset.TypeOrder ?? SortOrderDefaults.CopyTypeOrder());
                    string cultures = string.Join(",", preset.CultureOrder ?? new List<string>());
                    lines.Add(preset.Name + "\t" + keys + "\tTYPE=" + types + "\tCULTURE=" + cultures);
                }

                string temp = path + ".tmp";
                File.WriteAllLines(temp, lines);
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.Print("PartyQOL: could not write sort presets (" + e.Message + ")");
                return false;
            }
        }

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModSettings", "Global", "PartyQOL", "sort_presets.txt");
    }
}
