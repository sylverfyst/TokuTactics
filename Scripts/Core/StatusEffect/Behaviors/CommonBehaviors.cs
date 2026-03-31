using System.Collections.Generic;
using TokuTactics.Core.Stats;

namespace TokuTactics.Core.StatusEffect.Behaviors
{
    /// <summary>
    /// Modifies a stat while active. Produces a stat modifier output on apply,
    /// and an inverse modifier on removal to undo the effect.
    /// Example: DefenseBreak = StatModifierBehavior(DEF, -5f) = -5 DEF while active.
    /// </summary>
    public class StatModifierBehavior : IEffectBehavior
    {
        public string Id { get; }
        public StatType TargetStat { get; }
        public float Modifier { get; }

        public StatModifierBehavior(StatType targetStat, float modifier)
        {
            TargetStat = targetStat;
            Modifier = modifier;
            Id = $"behavior_stat_mod_{targetStat}_{modifier}";
        }

        public EffectOutput GetOutput(EffectContext context)
        {
            return new EffectOutput
            {
                StatModifiers = new Dictionary<StatType, float>
                {
                    { TargetStat, Modifier * context.PotencyMultiplier }
                }
            };
        }

        public EffectOutput GetRemovalOutput(EffectContext context)
        {
            // Undo the modifier
            return new EffectOutput
            {
                StatModifiers = new Dictionary<StatType, float>
                {
                    { TargetStat, -Modifier * context.PotencyMultiplier }
                }
            };
        }
    }

    /// <summary>
    /// Deals damage each time it triggers (typically on turn start = DoT).
    /// Damage scales with source MAG and potency multiplier.
    /// </summary>
    public class DamageOverTimeBehavior : IEffectBehavior
    {
        public string Id => "behavior_dot";
        public float BaseDamage { get; }

        public DamageOverTimeBehavior(float baseDamage)
        {
            BaseDamage = baseDamage;
        }

        public EffectOutput GetOutput(EffectContext context)
        {
            float scaledDamage = BaseDamage * context.PotencyMultiplier * (1 + context.SourceMag * 0.01f);
            return new EffectOutput { Damage = scaledDamage };
        }

        public EffectOutput GetRemovalOutput(EffectContext context) => EffectOutput.None;
    }

    /// <summary>
    /// Reduces movement range while active.
    /// </summary>
    public class MovementReductionBehavior : IEffectBehavior
    {
        public string Id => "behavior_slow";
        public float ReductionMultiplier { get; }

        /// <param name="reductionMultiplier">
        /// Multiplicative modifier. 0.5 = 50% movement. Potency scales toward 0.
        /// </param>
        public MovementReductionBehavior(float reductionMultiplier)
        {
            ReductionMultiplier = reductionMultiplier;
        }

        public EffectOutput GetOutput(EffectContext context)
        {
            // Potency makes the slow stronger (closer to 0)
            float scaledMultiplier = 1.0f - ((1.0f - ReductionMultiplier) * context.PotencyMultiplier);
            return new EffectOutput { MovementMultiplier = scaledMultiplier };
        }

        public EffectOutput GetRemovalOutput(EffectContext context) => EffectOutput.None;
    }

    /// <summary>
    /// Prevents the target from acting on their next turn.
    /// </summary>
    public class StunBehavior : IEffectBehavior
    {
        public string Id => "behavior_stun";

        public EffectOutput GetOutput(EffectContext context)
        {
            return new EffectOutput { PreventsAction = true };
        }

        public EffectOutput GetRemovalOutput(EffectContext context) => EffectOutput.None;
    }

    /// <summary>
    /// Heals the target each time it triggers.
    /// </summary>
    public class HealOverTimeBehavior : IEffectBehavior
    {
        public string Id => "behavior_hot";
        public float BaseHealing { get; }

        public HealOverTimeBehavior(float baseHealing)
        {
            BaseHealing = baseHealing;
        }

        public EffectOutput GetOutput(EffectContext context)
        {
            float scaledHealing = BaseHealing * context.PotencyMultiplier * (1 + context.SourceMag * 0.01f);
            return new EffectOutput { Healing = scaledHealing };
        }

        public EffectOutput GetRemovalOutput(EffectContext context) => EffectOutput.None;
    }
}
