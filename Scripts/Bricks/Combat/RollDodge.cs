using System;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Determines if an attack is dodged based on defender luck stat.
    /// Pure probability check with injected RNG for testability.
    /// Dodge chance = baseDodgeChance + (defenderLck * lckDodgeScale).
    /// </summary>
    public static class RollDodge
    {
        /// <summary>
        /// Executes the dodge roll check.
        /// </summary>
        /// <param name="defenderLck">Defender's luck stat</param>
        /// <param name="baseDodgeChance">Base dodge chance before luck modifier (0-1)</param>
        /// <param name="lckDodgeScale">How much each point of luck contributes to dodge chance</param>
        /// <param name="rng">Random number generator for the roll</param>
        /// <returns>True if the attack was dodged, false otherwise</returns>
        public static bool Execute(float defenderLck, float baseDodgeChance, float lckDodgeScale, Random rng)
        {
            float dodgeChance = baseDodgeChance + (defenderLck * lckDodgeScale);
            return rng.NextDouble() < dodgeChance;
        }
    }
}
