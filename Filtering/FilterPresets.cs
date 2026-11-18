using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PartyQOL.Sorting;
using TaleWorlds.Library;

namespace PartyQOL.Filtering
{
    internal sealed class FilterPreset
    {
        public string Name;
        public bool UseType = true;
        public bool UseCulture = true;
        public bool UseTier;
        public bool UseMaxTier;
        public bool UseLevel;
        public bool UseCount;
        public bool UseCondition;
        public ConditionFilterMode ConditionMode = ConditionFilterMode.Off;
        public List<FilterToggleItem> Types = FilterState.DefaultTypes();
        public List<FilterToggleItem> Cultures = new List<FilterToggleItem>();
        public int TierMin;
        public int TierMax = 6;
        public int MaxTierMin;
        public int MaxTierMax = 6;
        public int LevelMin = 1;
        public int LevelMax = 99;
        public int CountMin = 1;
        public int CountMax = 999;

        public FilterPreset Clone(string name = null)
        {
            return new FilterPreset
            {
                Name = name ?? Name,
                UseType = UseType,
                UseCulture = UseCulture,
                UseTier = UseTier,
                UseMaxTier = UseMaxTier,
                UseLevel = UseLevel,
                UseCount = UseCount,
                UseCondition = UseCondition,
                ConditionMode = ConditionMode,
                Types = Types.Select(t => t.Clone()).ToList(),
                Cultures = Cultures.Select(c => c.Clone()).ToList(),
                TierMin = TierMin,
                TierMax = TierMax,
                MaxTierMin = MaxTierMin,
                MaxTierMax = MaxTierMax,
                LevelMin = LevelMin,
                LevelMax = LevelMax,
                CountMin = CountMin,
                CountMax = CountMax
            };
        }

        public string SummaryFor(FilterCriterionKind kind)
        {
            switch (kind)
            {
                case FilterCriterionKind.Type:
                    if (!UseType)
                    {
                        return "off";
                    }

                    List<string> on = Types.Where(t => t.Enabled).Select(ShortType).ToList();
                    if (on.Count == 0)
                    {
                        return "none";
                    }

                    return on.Count >= Types.Count ? "all" : string.Join(", ", on);
                case FilterCriterionKind.Culture:
                    if (!UseCulture)
                    {
                        return "off";
                    }

                    if (Cultures.Count == 0)
                    {
                        return "all";
                    }

                    List<string> cultures = Cultures.Where(c => c.Enabled).Select(c => c.Id).ToList();
                    if (cultures.Count == 0)
                    {
                        return "none";
                    }

                    return cultures.Count >= Cultures.Count ? "all" : string.Join(", ", cultures.Take(3)) + (cultures.Count > 3 ? "…" : "");
                case FilterCriterionKind.Tier:
                    return UseTier ? TierMin + "–" + TierMax : "off";
                case FilterCriterionKind.MaxTier:
                    return UseMaxTier ? MaxTierMin + "–" + MaxTierMax : "off";
                case FilterCriterionKind.Level:
                    return UseLevel ? LevelMin + "–" + LevelMax : "off";
                case FilterCriterionKind.Count:
                    return UseCount ? CountMin + "–" + CountMax : "off";
                case FilterCriterionKind.Condition:
                    if (!UseCondition || ConditionMode == ConditionFilterMode.Off)
                    {
                        return "off";
                    }

                    return ConditionMode == ConditionFilterMode.Wounded ? "wounded" : "healthy";
                default:
                    return string.Empty;
            }
        }

        private static string ShortType(FilterToggleItem item)
        {
            return item.Id switch
            {
                "Infantry" => "Inf",
                "Ranged" => "Rng",
                "Cavalry" => "Cav",
                "HorseArcher" => "HA",
                _ => item.Id.Length <= 3 ? item.Id : item.Id.Substring(0, 3)
            };
        }
    }

    internal static class FilterPresets
    {
        internal const int MaxCount = 16;
        internal const int MaxNameLength = 22;
        internal const string NewCustomName = "New Custom";

        /// <summary>Shipped filters. Wounded and Healthy only.</summary>
        private static readonly string[] FactoryNames = { "Wounded", "Healthy" };

        private static readonly List<FilterPreset> Items = new List<FilterPreset>();
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

        internal static IReadOnlyList<FilterPreset> All
        {
            get
            {
                EnsureLoaded();
                return Items;
            }
        }

        internal static void EnsureLoadedPublic() => EnsureLoaded();

        internal static bool TryGet(string name, out FilterPreset preset)
        {
            EnsureLoaded();
            preset = Find(name);
            return preset != null;
        }

        internal static bool Activate(string name)
        {
            EnsureLoaded();
            FilterPreset preset = Find(name);
            if (preset == null)
            {
                return false;
            }

            _activeName = preset.Name;
            FilterState.ApplyFrom(preset);
            return true;
        }

        internal static void ClearActive()
        {
            EnsureLoaded();
            _activeName = string.Empty;
            FilterState.Clear();
        }

        internal static FilterPreset CreateDraft()
        {
            return new FilterPreset { Name = NewCustomName };
        }

        internal static bool IsFactoryName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (int i = 0; i < FactoryNames.Length; i++)
            {
                if (string.Equals(name, FactoryNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static FilterPreset CreateFactoryPreset(string name)
        {
            var preset = new FilterPreset
            {
                Name = CanonicalFactoryName(name),
                Types = FilterState.DefaultTypes(),
                Cultures = FilterState.DefaultCultures()
            };

            if (string.Equals(name, "Wounded", StringComparison.OrdinalIgnoreCase))
            {
                preset.UseCondition = true;
                preset.ConditionMode = ConditionFilterMode.Wounded;
            }
            else if (string.Equals(name, "Healthy", StringComparison.OrdinalIgnoreCase))
            {
                preset.UseCondition = true;
                preset.ConditionMode = ConditionFilterMode.Healthy;
            }

            return preset;
        }

        internal static bool TrySaveAs(string rawName, FilterPreset draft, out string savedName, out string error)
        {
            savedName = null;
            error = null;
            if (draft == null)
            {
                error = "Nothing to save.";
                return false;
            }

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
            FilterPreset existing = Find(name);
            FilterPreset copy = draft.Clone(name);
            if (existing != null)
            {
                int idx = Items.IndexOf(existing);
                Items[idx] = copy;
            }
            else
            {
                if (Items.Count >= MaxCount)
                {
                    error = "The list holds 16 filter presets. Delete one first.";
                    return false;
                }

                Items.Add(copy);
            }

            // Persist only. Activate via the party-bar filter selector (same as Sort Save).
            if (string.Equals(_activeName, name, StringComparison.OrdinalIgnoreCase))
            {
                FilterState.ApplyFrom(copy);
            }

            if (!TryWrite())
            {
                error = "Could not save filter presets.";
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
                error = "Factory filters cannot be renamed.";
                return false;
            }

            if (IsFactoryName(newName) || string.Equals(newName, NewCustomName, StringComparison.OrdinalIgnoreCase))
            {
                error = "Choose a different name.";
                return false;
            }

            EnsureLoaded();
            FilterPreset preset = Find(oldName);
            if (preset == null)
            {
                error = "No saved filter with that name.";
                return false;
            }

            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) && Find(newName) != null)
            {
                error = "A filter with that name already exists.";
                return false;
            }

            preset.Name = newName;
            if (string.Equals(_activeName, oldName, StringComparison.OrdinalIgnoreCase))
            {
                _activeName = newName;
            }

            if (!TryWrite())
            {
                error = "Could not save filter presets.";
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
                error = "Factory filters cannot be deleted.";
                return false;
            }

            FilterPreset preset = Find(name);
            if (preset == null)
            {
                error = "No saved filter with that name.";
                return false;
            }

            Items.Remove(preset);
            if (string.Equals(_activeName, name, StringComparison.OrdinalIgnoreCase))
            {
                ClearActive();
            }

            if (!TryWrite())
            {
                error = "Could not save filter presets.";
                return false;
            }

            return true;
        }

        internal static string NextFreeName()
        {
            EnsureLoaded();
            for (int i = 1; i <= MaxCount + 2; i++)
            {
                string candidate = "Filter " + i;
                if (Find(candidate) == null)
                {
                    return candidate;
                }
            }

            return "Filter";
        }

        internal static void MergeCultureIds(IEnumerable<string> cultureIds)
        {
            EnsureLoaded();
            if (cultureIds == null)
            {
                return;
            }

            foreach (FilterPreset preset in Items)
            {
                foreach (string id in cultureIds)
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    if (!preset.Cultures.Exists(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase)))
                    {
                        preset.Cultures.Add(new FilterToggleItem { Id = id, Enabled = true });
                    }
                }
            }
        }

        private static FilterPreset Find(string name)
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

                        FilterPreset parsed = ParseLine(line);
                        if (parsed != null && Find(parsed.Name) == null)
                        {
                            Items.Add(parsed);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("PartyQOL: could not read filter presets (" + e.Message + ")");
            }

            EnsureStarterPresets();

            if (Find(_activeName) == null)
            {
                _activeName = string.Empty;
            }
        }

        private static void EnsureStarterPresets()
        {
            bool changed = false;
            for (int i = 0; i < FactoryNames.Length; i++)
            {
                if (Find(FactoryNames[i]) != null || Items.Count >= MaxCount)
                {
                    continue;
                }

                Items.Add(CreateFactoryPreset(FactoryNames[i]));
                changed = true;
            }

            if (changed)
            {
                TryWrite();
            }
        }

        private static string CanonicalFactoryName(string name)
        {
            for (int i = 0; i < FactoryNames.Length; i++)
            {
                if (string.Equals(name, FactoryNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return FactoryNames[i];
                }
            }

            return name;
        }

        private static FilterPreset ParseLine(string line)
        {
            string[] parts = line.Split('\t');
            if (parts.Length < 1 || !TryNormalize(parts[0], out string name, out _))
            {
                return null;
            }

            FilterPreset preset = new FilterPreset { Name = name };
            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i];
                int eq = part.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = part.Substring(0, eq).Trim();
                string value = part.Substring(eq + 1).Trim();
                switch (key.ToUpperInvariant())
                {
                    case "USE":
                        preset.UseType = value.IndexOf('T') >= 0;
                        preset.UseCulture = value.IndexOf('C') >= 0;
                        preset.UseTier = value.IndexOf('R') >= 0;
                        preset.UseMaxTier = value.IndexOf('X') >= 0;
                        preset.UseLevel = value.IndexOf('L') >= 0;
                        preset.UseCount = value.IndexOf('N') >= 0;
                        preset.UseCondition = value.IndexOf('W') >= 0;
                        break;
                    case "TYPES":
                        preset.Types = ParseToggles(value, SortOrderDefaults.TypeOrder);
                        break;
                    case "CULTURES":
                        preset.Cultures = ParseToggles(value, Array.Empty<string>());
                        break;
                    case "TIER":
                        ParseRange(value, out int tMin, out int tMax, 0, 6);
                        preset.TierMin = tMin;
                        preset.TierMax = tMax;
                        break;
                    case "MAXTIER":
                        ParseRange(value, out int mtMin, out int mtMax, 0, 6);
                        preset.MaxTierMin = mtMin;
                        preset.MaxTierMax = mtMax;
                        break;
                    case "LEVEL":
                        ParseRange(value, out int lMin, out int lMax, 1, 99);
                        preset.LevelMin = lMin;
                        preset.LevelMax = lMax;
                        break;
                    case "COUNT":
                        ParseRange(value, out int cMin, out int cMax, 1, 999);
                        preset.CountMin = cMin;
                        preset.CountMax = cMax;
                        break;
                    case "COND":
                        preset.ConditionMode = ParseCondition(value);
                        break;
                }
            }

            if (preset.Types.Count == 0)
            {
                preset.Types = FilterState.DefaultTypes();
            }

            if (preset.UseCondition && preset.ConditionMode == ConditionFilterMode.Off)
            {
                preset.ConditionMode = ConditionFilterMode.Wounded;
            }

            return preset;
        }

        private static ConditionFilterMode ParseCondition(string raw)
        {
            if (string.Equals(raw, "wounded", StringComparison.OrdinalIgnoreCase) || raw == "1")
            {
                return ConditionFilterMode.Wounded;
            }

            if (string.Equals(raw, "healthy", StringComparison.OrdinalIgnoreCase) || raw == "2")
            {
                return ConditionFilterMode.Healthy;
            }

            return ConditionFilterMode.Off;
        }

        private static List<FilterToggleItem> ParseToggles(string raw, IReadOnlyList<string> required)
        {
            var list = new List<FilterToggleItem>();
            foreach (string part in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] bits = part.Trim().Split(':');
                if (bits.Length < 1 || string.IsNullOrWhiteSpace(bits[0]))
                {
                    continue;
                }

                string id = bits[0].Trim();
                bool enabled = bits.Length < 2 || bits[1].Trim() != "0";
                if (!list.Exists(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new FilterToggleItem { Id = id, Enabled = enabled });
                }
            }

            foreach (string id in required)
            {
                if (!list.Exists(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new FilterToggleItem { Id = id, Enabled = true });
                }
            }

            return list;
        }

        private static void ParseRange(string raw, out int min, out int max, int defaultMin, int defaultMax)
        {
            min = defaultMin;
            max = defaultMax;
            string[] bits = raw.Split('-');
            if (bits.Length >= 2 && int.TryParse(bits[0].Trim(), out int a) && int.TryParse(bits[1].Trim(), out int b))
            {
                min = Math.Min(a, b);
                max = Math.Max(a, b);
            }
        }

        private static string ConditionToken(ConditionFilterMode mode)
        {
            switch (mode)
            {
                case ConditionFilterMode.Wounded:
                    return "wounded";
                case ConditionFilterMode.Healthy:
                    return "healthy";
                default:
                    return "off";
            }
        }

        private static bool TryWrite()
        {
            try
            {
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var lines = new List<string>
                {
                    "# PartyQOL filter presets. name<TAB>USE=TCRXLNW<TAB>TYPES=...<TAB>CULTURES=...<TAB>TIER=0-6<TAB>MAXTIER=0-6<TAB>LEVEL=1-99<TAB>COUNT=1-999<TAB>COND=off|wounded|healthy"
                };
                foreach (FilterPreset preset in Items)
                {
                    string use = (preset.UseType ? "T" : "")
                                 + (preset.UseCulture ? "C" : "")
                                 + (preset.UseTier ? "R" : "")
                                 + (preset.UseMaxTier ? "X" : "")
                                 + (preset.UseLevel ? "L" : "")
                                 + (preset.UseCount ? "N" : "")
                                 + (preset.UseCondition ? "W" : "");
                    string types = string.Join(",", preset.Types.Select(t => t.Id + ":" + (t.Enabled ? "1" : "0")));
                    string cultures = string.Join(",", preset.Cultures.Select(c => c.Id + ":" + (c.Enabled ? "1" : "0")));
                    lines.Add(preset.Name
                              + "\tUSE=" + use
                              + "\tTYPES=" + types
                              + "\tCULTURES=" + cultures
                              + "\tTIER=" + preset.TierMin + "-" + preset.TierMax
                              + "\tMAXTIER=" + preset.MaxTierMin + "-" + preset.MaxTierMax
                              + "\tLEVEL=" + preset.LevelMin + "-" + preset.LevelMax
                              + "\tCOUNT=" + preset.CountMin + "-" + preset.CountMax
                              + "\tCOND=" + ConditionToken(preset.ConditionMode));
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
                Debug.Print("PartyQOL: could not write filter presets (" + e.Message + ")");
                return false;
            }
        }

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModSettings", "Global", "PartyQOL", "filter_presets.txt");
    }
}
