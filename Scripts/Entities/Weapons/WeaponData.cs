using TokuTactics.Core.StatusEffect;

namespace TokuTactics.Entities.Weapons
{
    /// <summary>
    /// Immutable data definition for a weapon. Each form has two of these.
    /// The weapon's stats scale with the form's level, not independently.
    /// The weapon's status effect potency scales with MAG and the form's status effect track.
    /// </summary>
    public class WeaponData
    {
        /// <summary>Unique identifier for this weapon.</summary>
        public string Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Base damage power (scaled by form level's damage track).</summary>
        public float BasePower { get; }

        /// <summary>Range in tiles. 1 = melee adjacent, 2+ = ranged.</summary>
        public int Range { get; }

        /// <summary>
        /// Template for the status effect this weapon applies.
        /// Composed from IEffectTrigger + IEffectBehavior.
        /// Potency is scaled at application time by MAG + status effect track.
        /// </summary>
        public StatusEffectTemplate StatusEffect { get; }

        public WeaponData(string id, string name, float basePower, int range, StatusEffectTemplate statusEffect)
        {
            Id = id;
            Name = name;
            BasePower = basePower;
            Range = range;
            StatusEffect = statusEffect;
        }
    }

    /// <summary>
    /// Template for creating status effect instances.
    /// Stores the composable pieces — actual instances are created at application time
    /// with MAG and potency scaling applied.
    /// </summary>
    public class StatusEffectTemplate
    {
        public string EffectId { get; }
        public IEffectTrigger Trigger { get; }
        public IEffectBehavior Behavior { get; }
        public int BaseDuration { get; }

        public StatusEffectTemplate(string effectId, IEffectTrigger trigger, IEffectBehavior behavior, int baseDuration)
        {
            EffectId = effectId;
            Trigger = trigger;
            Behavior = behavior;
            BaseDuration = baseDuration;
        }

        /// <summary>
        /// Create a live instance with potency scaling from MAG and status effect track.
        /// Each instance gets a fresh trigger via CreateFresh() to prevent shared mutable state.
        /// </summary>
        public StatusEffectInstance CreateInstance(float potencyMultiplier)
        {
            return new StatusEffectInstance(EffectId, Trigger.CreateFresh(), Behavior, BaseDuration, potencyMultiplier);
        }
    }
}
