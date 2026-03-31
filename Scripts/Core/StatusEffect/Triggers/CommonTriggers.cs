namespace TokuTactics.Core.StatusEffect.Triggers
{
    /// <summary>Fires every turn at the start of the affected entity's turn. Stateless.</summary>
    public class TurnStartTrigger : IEffectTrigger
    {
        public string Id => "trigger_turn_start";
        public bool ShouldTrigger(EffectContext context) => context.Phase == "turn_start";
        public IEffectTrigger CreateFresh() => this; // Stateless
    }

    /// <summary>
    /// Fires immediately on application and never again.
    /// STATEFUL — owns a one-shot flag. Must be cloned via CreateFresh().
    /// </summary>
    public class InstantTrigger : IEffectTrigger
    {
        private bool _fired;
        public string Id => "trigger_instant";

        public bool ShouldTrigger(EffectContext context)
        {
            if (_fired) return false;
            _fired = true;
            return true;
        }

        /// <summary>
        /// Creates a fresh instance with reset state.
        /// Critical: without this, a StatusEffectTemplate with an InstantTrigger
        /// would only fire on its first application ever.
        /// </summary>
        public IEffectTrigger CreateFresh() => new InstantTrigger();
    }

    /// <summary>Fires when the affected entity takes damage. Stateless.</summary>
    public class OnDamageTakenTrigger : IEffectTrigger
    {
        public string Id => "trigger_on_damage";
        public bool ShouldTrigger(EffectContext context) => context.Phase == "damage_taken";
        public IEffectTrigger CreateFresh() => this; // Stateless
    }

    /// <summary>Fires when the affected entity attempts to move. Stateless.</summary>
    public class OnMoveTrigger : IEffectTrigger
    {
        public string Id => "trigger_on_move";
        public bool ShouldTrigger(EffectContext context) => context.Phase == "move";
        public IEffectTrigger CreateFresh() => this; // Stateless
    }
}
