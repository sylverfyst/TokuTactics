namespace TokuTactics.Core.Health
{
    /// <summary>
    /// Interface for anything that has a health pool: forms, unmorphed Rangers, zords.
    /// Each form has its own IHealthPool. Switching forms switches which pool is active.
    /// </summary>
    public interface IHealthPool
    {
        float Current { get; }
        float Maximum { get; }
        bool IsAlive { get; }
        float Percentage { get; }

        /// <summary>
        /// Apply damage. Returns actual damage dealt after any modifiers.
        /// </summary>
        float TakeDamage(float amount);

        /// <summary>
        /// Apply healing. Returns actual healing done (capped at maximum).
        /// </summary>
        float Heal(float amount);

        /// <summary>
        /// Apply passive regeneration (used during cooldown ticks).
        /// </summary>
        void Regenerate(float amount);

        /// <summary>
        /// Reset to full health.
        /// </summary>
        void Reset();
    }
}
