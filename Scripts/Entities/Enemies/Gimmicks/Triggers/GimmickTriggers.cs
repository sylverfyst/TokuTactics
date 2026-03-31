namespace TokuTactics.Entities.Enemies.Gimmicks.Triggers
{
    /// <summary>
    /// Activates at the start of every turn. Voluntary action. Stateless.
    /// </summary>
    public class TurnStartGimmickTrigger : IGimmickTrigger
    {
        public string Id => "gimmick_trigger_turn_start";
        public bool IsVoluntary => true;

        public bool ShouldActivate(GimmickContext context) => true;
        public void OnActivated() { }
        public IGimmickTrigger CreateFresh() => this; // Stateless — safe to share
    }

    /// <summary>
    /// Activates every N turns. Voluntary action. Stateless — reads turn count from context.
    /// </summary>
    public class EveryNTurnsGimmickTrigger : IGimmickTrigger
    {
        public string Id => "gimmick_trigger_every_n_turns";
        public bool IsVoluntary => true;

        /// <summary>How many turns between activations.</summary>
        public int TurnInterval { get; }

        public EveryNTurnsGimmickTrigger(int turnInterval)
        {
            TurnInterval = turnInterval;
        }

        public bool ShouldActivate(GimmickContext context)
        {
            return context.TurnsSinceLastActivation >= TurnInterval;
        }

        public void OnActivated() { }
        public IGimmickTrigger CreateFresh() => this; // Stateless — reads from context
    }

    /// <summary>
    /// Activates once when health drops below a threshold. Voluntary action.
    /// STATEFUL — owns a one-shot flag. Must be cloned per enemy instance via CreateFresh().
    /// </summary>
    public class HealthThresholdGimmickTrigger : IGimmickTrigger
    {
        public string Id => "gimmick_trigger_health_threshold";
        public bool IsVoluntary => true;

        /// <summary>Health percentage (0-1) below which the gimmick fires.</summary>
        public float Threshold { get; }

        private bool _hasFired;

        public HealthThresholdGimmickTrigger(float threshold)
        {
            Threshold = threshold;
        }

        public bool ShouldActivate(GimmickContext context)
        {
            if (_hasFired) return false;
            return context.OwnerHealthPercentage <= Threshold;
        }

        public void OnActivated()
        {
            _hasFired = true;
        }

        /// <summary>Whether this trigger has already fired (for save/load).</summary>
        public bool HasFired => _hasFired;

        /// <summary>
        /// Creates a fresh instance with reset state.
        /// Critical: without this, two enemies sharing the same EnemyData
        /// would share the one-shot flag.
        /// </summary>
        public IGimmickTrigger CreateFresh() => new HealthThresholdGimmickTrigger(Threshold);
    }

    /// <summary>
    /// Activates when the enemy is hit. Reactive trigger. Stateless.
    /// </summary>
    public class OnHitGimmickTrigger : IGimmickTrigger
    {
        public string Id => "gimmick_trigger_on_hit";
        public bool IsVoluntary => false;

        public bool ShouldActivate(GimmickContext context) => context.WasJustHit;
        public void OnActivated() { }
        public IGimmickTrigger CreateFresh() => this; // Stateless
    }

    /// <summary>
    /// Activates when a Ranger is adjacent. Reactive trigger. Stateless.
    /// </summary>
    public class RangerAdjacentGimmickTrigger : IGimmickTrigger
    {
        public string Id => "gimmick_trigger_ranger_adjacent";
        public bool IsVoluntary => false;

        public bool ShouldActivate(GimmickContext context) => context.IsRangerAdjacent;
        public void OnActivated() { }
        public IGimmickTrigger CreateFresh() => this; // Stateless
    }
}
