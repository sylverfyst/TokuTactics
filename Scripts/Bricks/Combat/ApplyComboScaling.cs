namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Applies combo multiplier to damage for chained attacks.
    /// Pure multiplication operation with no side effects.
    /// Combo multipliers typically decrease with each successive chain action.
    /// </summary>
    public static class ApplyComboScaling
    {
        /// <summary>
        /// Executes the combo scaling calculation.
        /// </summary>
        /// <param name="damage">Damage before combo scaling</param>
        /// <param name="comboMultiplier">Multiplier based on combo position (1.0 for first attack, decreasing for chains)</param>
        /// <returns>Modified damage with combo scaling applied</returns>
        public static float Execute(float damage, float comboMultiplier)
        {
            return damage * comboMultiplier;
        }
    }
}
