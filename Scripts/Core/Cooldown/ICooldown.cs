namespace TokuTactics.Core.Cooldown
{
    /// <summary>
    /// Interface for anything that tracks a cooldown: forms, base form after demorph,
    /// Battleizer, Megazord combination.
    /// </summary>
    public interface ICooldown
    {
        /// <summary>Remaining turns until available.</summary>
        int RemainingTurns { get; }

        /// <summary>The base duration before any modifiers (e.g., MAG reduction).</summary>
        int BaseDuration { get; }

        /// <summary>Whether this cooldown is currently active (not available).</summary>
        bool IsOnCooldown { get; }

        /// <summary>Whether this is available for use.</summary>
        bool IsAvailable { get; }

        /// <summary>Start the cooldown with optional modifier (e.g., from MAG stat).</summary>
        void Activate(int durationModifier = 0);

        /// <summary>Advance the cooldown by one turn.</summary>
        void Tick();

        /// <summary>Force reset to available.</summary>
        void Reset();
    }
}
