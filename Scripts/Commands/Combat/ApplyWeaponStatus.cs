using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Command: Calculates status potency from caster's MAG, creates the status instance,
    /// and applies it to the target's tracker. Used in both ranger and enemy attack paths.
    /// Composes CalculateStatusPotency, StatusEffectTemplate.CreateInstance, and ApplyStatusEffect bricks.
    /// </summary>
    public static class ApplyWeaponStatus
    {
        /// <summary>
        /// Apply a weapon's status effect to a target.
        /// </summary>
        /// <param name="template">The weapon's status effect template</param>
        /// <param name="targetTracker">The target's status effect tracker</param>
        /// <param name="casterMag">The caster's MAG stat</param>
        /// <param name="magScale">Scaling factor for MAG → potency</param>
        /// <param name="calculatePotency">Injectable brick for testing</param>
        /// <param name="applyEffect">Injectable brick for testing</param>
        /// <returns>The effect ID that was applied</returns>
        public static string Execute(
            StatusEffectTemplate template,
            StatusEffectTracker targetTracker,
            float casterMag,
            float magScale,
            Func<float, float, float> calculatePotency = null,
            Func<StatusEffectTracker, StatusEffectInstance, string> applyEffect = null)
        {
            calculatePotency ??= CalculateStatusPotency.Execute;
            applyEffect ??= ApplyStatusEffect.Execute;

            float potency = calculatePotency(casterMag, magScale);
            var instance = template.CreateInstance(potency);
            return applyEffect(targetTracker, instance);
        }
    }
}
