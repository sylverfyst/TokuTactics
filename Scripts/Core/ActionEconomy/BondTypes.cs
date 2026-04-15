namespace TokuTactics.Core.ActionEconomy
{
    /// <summary>
    /// Type: Pure data shape for the bond between two Rangers.
    /// </summary>
    public class BondState
    {
        public string RangerA { get; set; }
        public string RangerB { get; set; }
        public int Experience { get; set; }
        public int Tier { get; set; }

        public BondState(string rangerA, string rangerB)
        {
            RangerA = rangerA;
            RangerB = rangerB;
        }

        public bool Involves(string rangerId) => RangerA == rangerId || RangerB == rangerId;

        public string GetPartner(string rangerId)
        {
            if (RangerA == rangerId) return RangerB;
            if (RangerB == rangerId) return RangerA;
            return null;
        }
    }

    /// <summary>
    /// Type: Event data when a bond reaches a new tier.
    /// </summary>
    public class BondTierChange
    {
        public string RangerA { get; set; }
        public string RangerB { get; set; }
        public int OldTier { get; set; }
        public int NewTier { get; set; }
    }
}
