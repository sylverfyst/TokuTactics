using System;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Calculates base damage from attacker strength vs defender defense and action power.
    /// Pure calculation with no side effects or external dependencies.
    /// Formula: (attackerStr / defenderDef) * actionPower with minimum of 1.
    /// </summary>
    public static class CalculateBaseDamage
    {
        /// <summary>
        /// Executes the base damage calculation.
        /// </summary>
        /// <param name="attackerStr">Attacker's strength stat</param>
        /// <param name="defenderDef">Defender's defense stat</param>
        /// <param name="actionPower">Power multiplier of the action (default 1.0)</param>
        /// <returns>Base damage as a float, minimum 1</returns>
        public static float Execute(float attackerStr, float defenderDef, float actionPower)
        {
            if (defenderDef <= 0f)
                return Math.Max(1f, attackerStr * actionPower);

            float baseDamage = (attackerStr / defenderDef) * actionPower;
            return Math.Max(1f, baseDamage);
        }
    }
}
