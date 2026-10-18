using System;
using System.IO;
using TaleWorlds.Library;

namespace PartyQOL
{
    /// <summary>
    /// Lightweight options (file-backed). MCM can mirror these later without changing call sites.
    /// </summary>
    internal static class PartyQolSettings
    {
        private const string AutoSortKey = "AutoSortOnTransfer";

        /// <summary>
        /// When true, re-apply the active sort preset after every troop transfer.
        /// Default false — manual sort / dropdown / settings apply still sort.
        /// </summary>
        public static bool AutoSortOnTransfer { get; set; }

        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            AutoSortOnTransfer = false;
            try
            {
                string path = FilePath();
                if (!File.Exists(path))
                {
                    return;
                }

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw?.Trim();
                    if (string.IsNullOrEmpty(line) || line[0] == '#')
                    {
                        continue;
                    }

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    if (string.Equals(key, AutoSortKey, StringComparison.OrdinalIgnoreCase))
                    {
                        AutoSortOnTransfer = IsTruthy(value);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("PartyQOL: could not read settings (" + e.Message + ")");
            }
        }

        public static void Save()
        {
            try
            {
                string path = FilePath();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllLines(
                    path,
                    new[]
                    {
                        "# PartyQOL settings",
                        AutoSortKey + "=" + (AutoSortOnTransfer ? "1" : "0")
                    });
            }
            catch (Exception e)
            {
                Debug.Print("PartyQOL: could not write settings (" + e.Message + ")");
            }
        }

        private static bool IsTruthy(string value)
        {
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static string FilePath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModSettings", "Global", "PartyQOL", "settings.txt");
    }
}
