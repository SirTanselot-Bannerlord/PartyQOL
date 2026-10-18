namespace PartyQOL.Filtering
{
    internal sealed class FilterToggleItem
    {
        public string Id;
        public bool Enabled = true;

        public FilterToggleItem Clone() => new FilterToggleItem { Id = Id, Enabled = Enabled };
    }
}
