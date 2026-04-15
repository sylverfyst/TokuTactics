namespace TokuTactics.Bricks.Bond
{
    /// <summary>
    /// Calculates scaled bond experience from base XP and the assister's CHA multiplier.
    /// Formula: baseExp * (1.0 + chaMultiplier * chaScale)
    /// </summary>
    public static class CalculateScaledBondExp
    {
        public static int Execute(int baseExp, float chaMultiplier, float chaScale = 0.1f)
        {
            return (int)(baseExp * (1.0f + chaMultiplier * chaScale));
        }
    }
}
