using System.Collections.Generic;
using TokuTactics.Core.Stats;

namespace TokuTactics.Core.StatusEffect
{
    /// <summary>
    /// Trigger condition that determines when a status effect activates.
    /// Examples: OnTurnStart, OnTakeDamage, OnMove, OnFormSwitch.
    /// 
    /// Triggers with mutable state (like InstantTrigger's one-shot flag)
    /// must return a new instance from CreateFresh(). Stateless triggers return 'this'.
    /// </summary>
    public interface IEffectTrigger
    {
        string Id { get; }
        bool ShouldTrigger(EffectContext context);

        /// <summary>
        /// Create a fresh instance with reset state.
        /// Called by StatusEffectTemplate.CreateInstance() so each applied effect
        /// gets its own trigger state. Stateless triggers return 'this'.
        /// </summary>
        IEffectTrigger CreateFresh();
    }

    /// <summary>
    /// Declares what a status effect does when triggered.
    /// Behaviors are declarative — they produce EffectOutput objects
    /// that the combat system reads and applies. Behaviors never
    /// directly modify game state.
    /// </summary>
    public interface IEffectBehavior
    {
        string Id { get; }

        /// <summary>
        /// Produce the effect outputs for this tick. The combat system
        /// consumes these and applies them to the appropriate systems.
        /// </summary>
        EffectOutput GetOutput(EffectContext context);

        /// <summary>
        /// Produce cleanup outputs when the effect expires or is removed.
        /// Used for effects that modify stats — the modifier must be undone.
        /// </summary>
        EffectOutput GetRemovalOutput(EffectContext context);
    }

    /// <summary>
    /// Describes what an effect does. Consumed by the combat system.
    /// Multiple fields can be set — an effect can deal damage AND apply a stat modifier.
    /// All fields are nullable/zero by default — only set what the behavior produces.
    /// </summary>
    public class EffectOutput
    {
        /// <summary>Damage to deal to the target.</summary>
        public float Damage { get; set; }

        /// <summary>Healing to apply to the target.</summary>
        public float Healing { get; set; }

        /// <summary>Stat modifiers to apply (additive). Keyed by stat type.</summary>
        public Dictionary<StatType, float> StatModifiers { get; set; }

        /// <summary>Movement range modifier (multiplicative, 1.0 = no change).</summary>
        public float MovementMultiplier { get; set; } = 1.0f;

        /// <summary>Whether the target should skip their action this turn.</summary>
        public bool PreventsAction { get; set; }

        /// <summary>Whether this output has any actual effect.</summary>
        public bool HasEffect => Damage > 0 || Healing > 0 ||
            StatModifiers != null || MovementMultiplier != 1.0f || PreventsAction;

        public static EffectOutput None => new EffectOutput();
    }

    /// <summary>
    /// Context passed to triggers and behaviors so they can read game state
    /// without direct coupling to specific systems.
    /// </summary>
    public class EffectContext
    {
        /// <summary>The entity affected by this status effect.</summary>
        public object Target { get; set; }

        /// <summary>The entity that applied this status effect.</summary>
        public object Source { get; set; }

        /// <summary>The current turn phase when this context was created.</summary>
        public string Phase { get; set; }

        /// <summary>MAG stat of the source at time of application (scales potency).</summary>
        public float SourceMag { get; set; }

        /// <summary>Potency multiplier from status effect scaling track.</summary>
        public float PotencyMultiplier { get; set; }
    }
}
