namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Applies same-type attack bonus (STAB) multiplier to damage.
    /// Pure multiplication operation with no side effects.
    /// Used when attacker's innate type matches their form type.
    /// </summary>
    public static class CalculateSameTypeBonus
    {
        /// <summary>
        /// Executes the same-type bonus calculation.
        /// </summary>
        /// <param name="damage">Damage before same-type bonus</param>
        /// <param name="sameTypeBonus">Multiplier for same-type attacks (typically 1.25)</param>
        /// <returns>Modified damage with same-type bonus applied</returns>
        public static float Execute(float damage, float sameTypeBonus)
        {
            return damage * sameTypeBonus;
        }
    }
}
