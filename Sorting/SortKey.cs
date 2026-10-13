namespace PartyQOL.Sorting
{
    /// <summary>One sort criterion and its direction.</summary>
    public readonly struct SortKey
    {
        public SortKey(TroopSortMode mode, bool ascending)
        {
            Mode = mode;
            Ascending = ascending;
        }

        public TroopSortMode Mode { get; }

        public bool Ascending { get; }

        public SortKey WithAscending(bool ascending) => new SortKey(Mode, ascending);
    }
}
