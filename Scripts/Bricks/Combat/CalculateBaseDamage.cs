using System;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Calculates base damage from attacker strength vs defender defense and action power.
    /// Pure calculation with no side effects or external dependencies.
    /// Formula: (attackerStr - (defenderDef * 0.5)) * actionPower
    /// </summary>
    public static class CalculateBaseDamage
    {
        /// <summary>
        /// Executes the base damage calculation.
        /// </summary>
        /// <param name="attackerStr">Attacker's strength stat</param>
        /// <param name="defenderDef">Defender's defense stat</param>
        /// <param name="actionPower">Power multiplier of the action (default 1.0)</param>
        /// <returns>Base damage as a float (can be 0 if actionPower is 0)</returns>
        public static float Execute(float attackerStr, float defenderDef, float actionPower)
        {
            float baseDamage = Math.Max(1f, attackerStr - (defenderDef * 0.5f));
            return baseDamage * actionPower;
        }
    }
}
