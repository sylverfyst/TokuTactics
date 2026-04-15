namespace TokuTactics.Bricks.Bond
{
    /// <summary>
    /// Resolves the bond tier from accumulated experience and tier thresholds.
    /// Scans thresholds from highest to lowest, returns the tier (1-indexed) of the
    /// highest threshold met, or 0 if none are met.
    /// </summary>
    public static class ResolveBondTier
    {
        public static int Execute(int experience, int[] thresholds)
        {
            for (int i = thresholds.Length - 1; i >= 0; i--)
            {
                if (experience >= thresholds[i])
                    return i + 1;
            }
            return 0;
        }
    }
}
