using System;

namespace TokuTactics.Core.Combat
{
    /// <summary>
    /// Tracks form switch chains within a single turn and provides the combo damage scalar.
    /// First action is full damage (1.0). Each subsequent action in a chain scales down.
    /// Status effect potency is NOT affected — always 1.0.
    /// Resets at the start of each new turn.
    /// 
    /// The scaling curve values are tunable. The shape matters more than the numbers:
    /// - First 1-2 actions should feel strong
    /// - Actions 3-4 should feel noticeably weaker
    /// - Actions 5+ should be utility-only (status effect delivery, not damage)
    /// </summary>
    public class ComboScaler
    {
        private int _chainCount;

        /// <summary>Current position in the chain (0 = first action).</summary>
        public int ChainCount => _chainCount;

        /// <summary>
        /// The damage multiplier for the current chain position.
        /// Does NOT apply to status effects.
        /// </summary>
        public float DamageMultiplier => GetScaleForChainCount(_chainCount);

        /// <summary>
        /// Assist damage multiplier — higher than the actor's scaled damage
        /// but still subject to scaling. Tunable ratio.
        /// </summary>
        public float AssistDamageMultiplier => Math.Min(1.0f, DamageMultiplier * AssistScaleBonus);

        /// <summary>
        /// How much better assists scale compared to the chain actor.
        /// At 1.3, an assist at chain position 3 does 30% more than the actor's scaled damage.
        /// </summary>
        public float AssistScaleBonus { get; set; } = 1.3f;

        /// <summary>
        /// Status effect potency is always full regardless of chain position.
        /// This is what makes long chains valuable for utility.
        /// </summary>
        public float StatusEffectMultiplier => 1.0f;

        /// <summary>Advance the chain counter. Call on each form switch.</summary>
        public void AdvanceChain()
        {
            _chainCount++;
        }

        /// <summary>Reset at the start of a new turn.</summary>
        public void ResetChain()
        {
            _chainCount = 0;
        }

        /// <summary>
        /// Get the damage scale for a given chain position.
        /// Override this method to change the scaling curve.
        /// Default: 1.0, 0.8, 0.6, 0.4, 0.25, 0.15, 0.1...
        /// </summary>
        protected virtual float GetScaleForChainCount(int count)
        {
            return count switch
            {
                0 => 1.0f,
                1 => 0.8f,
                2 => 0.6f,
                3 => 0.4f,
                4 => 0.25f,
                5 => 0.15f,
                _ => 0.1f
            };
        }
    }
}
