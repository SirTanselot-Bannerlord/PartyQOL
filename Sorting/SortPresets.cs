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
            TroopSortState.SetKeys(preset.Keys);
            return true;
        }

        internal static void ClearActive()
        {
            EnsureLoaded();
            _activeName = string.Empty;
        }

        internal static bool TrySaveAs(string rawName, IReadOnlyList<SortKey> keys, out string savedName, out string error)
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
            SortPreset existing = Find(name);
            if (existing != null)
            {
                existing.Keys = Normalize(keys);
            }
            else
            {
                if (Items.Count >= MaxCount)
                {
                    error = "The list holds 16 sort orders. Delete one first.";
                    return false;
                }

                Items.Add(new SortPreset { Name = name, Keys = Normalize(keys) });
            }

            _activeName = name;
            TroopSortState.SetKeys(Normalize(keys));
            if (!TryWrite())
            {
                error = "Could not save sort orders.";
                return false;
            }

            savedName = name;
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
                TroopSortState.SetKeys(TroopSortState.DefaultKeys);
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

                        Items.Add(new SortPreset { Name = name, Keys = ParseKeys(line.Substring(tab + 1)) });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("PartyQOL: could not read sort presets (" + e.Message + ")");
            }

            if (Find(_activeName) == null)
            {
                _activeName = string.Empty;
            }
        }

        private static List<SortKey> ParseKeys(string raw)
        {
            var keys = new List<SortKey>();
            foreach (string part in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
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
                    "# PartyQOL sort presets. name<TAB>Mode:1|0,Mode:1|0,...  (1=asc, 0=desc)"
                };
                foreach (SortPreset preset in Items)
                {
                    string keys = string.Join(",", preset.Keys.Select(k => k.Mode + ":" + (k.Ascending ? "1" : "0")));
                    lines.Add(preset.Name + "\t" + keys);
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
