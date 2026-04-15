namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Calculates status effect potency from a caster's MAG stat and a scaling factor.
    /// Formula: 1.0 + magStat * magScale
    /// </summary>
    public static class CalculateStatusPotency
    {
        public static float Execute(float magStat, float magScale)
        {
            return 1.0f + magStat * magScale;
        }
    }
}
