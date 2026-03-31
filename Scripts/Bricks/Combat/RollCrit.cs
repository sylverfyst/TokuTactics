using System;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Determines if an attack is a critical hit based on attacker luck stat.
    /// Pure probability check with injected RNG for testability.
    /// Crit chance = baseCritChance + (attackerLck * lckCritScale).
    /// </summary>
    public static class RollCrit
    {
        /// <summary>
        /// Executes the critical hit roll check.
        /// </summary>
        /// <param name="attackerLck">Attacker's luck stat</param>
        /// <param name="baseCritChance">Base crit chance before luck modifier (0-1)</param>
        /// <param name="lckCritScale">How much each point of luck contributes to crit chance</param>
        /// <param name="rng">Random number generator for the roll</param>
        /// <returns>True if the attack was a critical hit, false otherwise</returns>
        public static bool Execute(float attackerLck, float baseCritChance, float lckCritScale, Random rng)
        {
            float critChance = baseCritChance + (attackerLck * lckCritScale);
            return rng.NextDouble() < critChance;
        }
    }
}
