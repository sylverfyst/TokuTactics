namespace TokuTactics.Bricks.Assist
{
    /// <summary>
    /// Calculates the combined damage multiplier for an assist attack.
    /// Combines bond tier bonus with combo scaling from the attacker's chain position.
    /// </summary>
    public static class CalculateAssistDamageMultiplier
    {
        /// <param name="bondTier">Bond tier between attacker and assister</param>
        /// <param name="comboAssistMultiplier">From attacker's ComboScaler.AssistDamageMultiplier</param>
        /// <param name="tier1Bonus">Damage multiplier at bond tier 1</param>
        /// <param name="pairAttackMultiplier">Damage multiplier for tier 2+ pair attacks</param>
        public static float Execute(
            int bondTier,
            float comboAssistMultiplier,
            float tier1Bonus,
            float pairAttackMultiplier)
        {
            float bondMultiplier = 1.0f;

            if (bondTier >= 2)
                bondMultiplier = pairAttackMultiplier;
            else if (bondTier >= 1)
                bondMultiplier = tier1Bonus;

            return comboAssistMultiplier * bondMultiplier;
        }
    }
}
