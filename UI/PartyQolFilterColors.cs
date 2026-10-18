using TaleWorlds.Library;

namespace PartyQOL.UI
{
    /// <summary>
    /// Font colors for filter-active counts. Uses Color.FromUint AARRGGBB
    /// (Gauntlet Brush.FontColor), not #RRGGBBAA XML brush literals.
    /// </summary>
    internal static class PartyQolFilterColors
    {
        /// <summary>Vanilla-ish parchment (AARRGGBB).</summary>
        public static readonly Color Normal = Color.FromUint(0xFFFAEBD4);

        /// <summary>Deficit / filter-active red (AARRGGBB).</summary>
        public static readonly Color Filtered = Color.FromUint(0xFFE85D5D);

        public static Color ForActive(bool filterActive) => filterActive ? Filtered : Normal;

        public static bool EqualsUint(Color a, Color b) =>
            a.ToUnsignedInteger() == b.ToUnsignedInteger();
    }
}
