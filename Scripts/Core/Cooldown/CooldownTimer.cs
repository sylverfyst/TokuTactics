using System;

namespace TokuTactics.Core.Cooldown
{
    /// <summary>
    /// Tracks a single cooldown timer. Duration is set at construction (from form data),
    /// and modified at activation time by MAG stat.
    /// </summary>
    public class CooldownTimer : ICooldown
    {
        public int RemainingTurns { get; private set; }
        public int BaseDuration { get; }
        public bool IsOnCooldown => RemainingTurns > 0;
        public bool IsAvailable => !IsOnCooldown;

        public CooldownTimer(int baseDuration)
        {
            BaseDuration = baseDuration;
            RemainingTurns = 0;
        }

        /// <summary>
        /// Start the cooldown. Duration modifier (typically from MAG) reduces the duration.
        /// Minimum cooldown is 1 turn.
        /// </summary>
        public void Activate(int durationModifier = 0)
        {
            RemainingTurns = Math.Max(1, BaseDuration - durationModifier);
        }

        public void Tick()
        {
            if (RemainingTurns > 0)
                RemainingTurns--;
        }

        public void Reset()
        {
            RemainingTurns = 0;
        }
    }
}
