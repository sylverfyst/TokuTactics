using TokuTactics.Core.StatusEffect;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Applies a status effect instance to a StatusEffectTracker.
    /// Returns the effect ID that was applied.
    /// </summary>
    public static class ApplyStatusEffect
    {
        public static string Execute(StatusEffectTracker tracker, StatusEffectInstance effect)
        {
            tracker.Apply(effect);
            return effect.Id;
        }
    }
}
