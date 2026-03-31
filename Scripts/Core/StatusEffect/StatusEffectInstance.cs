namespace TokuTactics.Core.StatusEffect
{
    /// <summary>
    /// A complete status effect composed from modular components.
    /// New effects are created by combining different triggers and behaviors — not new code.
    /// 
    /// Behaviors are declarative — Process() returns an EffectOutput describing
    /// what should happen. The combat system reads the output and applies it.
    /// 
    /// Example compositions:
    /// - Poison: TurnStartTrigger + DamageOverTimeBehavior, duration 3
    /// - Stun: InstantTrigger + StunBehavior, duration 1
    /// - DefenseBreak: InstantTrigger + StatModifierBehavior(DEF, -5), duration 2
    /// </summary>
    public class StatusEffectInstance
    {
        public string Id { get; }
        public IEffectTrigger Trigger { get; }
        public IEffectBehavior Behavior { get; }
        public int RemainingDuration { get; private set; }
        public int BaseDuration { get; }
        public float Potency { get; }
        public bool IsExpired => RemainingDuration <= 0;

        public StatusEffectInstance(
            string id,
            IEffectTrigger trigger,
            IEffectBehavior behavior,
            int baseDuration,
            float potency = 1.0f)
        {
            Id = id;
            Trigger = trigger;
            Behavior = behavior;
            BaseDuration = baseDuration;
            RemainingDuration = baseDuration;
            Potency = potency;
        }

        /// <summary>
        /// Check the trigger and produce an EffectOutput if conditions are met.
        /// Returns null if the trigger didn't fire or the effect is expired.
        /// The combat system consumes the returned output.
        /// </summary>
        public EffectOutput Process(EffectContext context)
        {
            if (IsExpired || !Trigger.ShouldTrigger(context))
                return null;

            context.PotencyMultiplier = Potency;
            return Behavior.GetOutput(context);
        }

        /// <summary>
        /// Tick duration down by one turn.
        /// </summary>
        public void Tick()
        {
            if (RemainingDuration > 0)
                RemainingDuration--;
        }

        /// <summary>
        /// Produce the removal output when effect expires or is removed.
        /// The combat system uses this to undo stat modifiers, etc.
        /// </summary>
        public EffectOutput GetRemovalOutput(EffectContext context)
        {
            context.PotencyMultiplier = Potency;
            return Behavior.GetRemovalOutput(context);
        }
    }
}
