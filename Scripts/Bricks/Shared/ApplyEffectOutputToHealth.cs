using TokuTactics.Core.Health;
using TokuTactics.Core.StatusEffect;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Applies damage and healing from a single EffectOutput to a single health pool.
    /// </summary>
    public static class ApplyEffectOutputToHealth
    {
        public static void Execute(IHealthPool healthPool, EffectOutput output)
        {
            if (output.Damage > 0)
                healthPool.TakeDamage(output.Damage);

            if (output.Healing > 0)
                healthPool.Heal(output.Healing);
        }
    }
}
