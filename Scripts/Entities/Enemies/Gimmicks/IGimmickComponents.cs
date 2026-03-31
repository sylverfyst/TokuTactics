namespace TokuTactics.Entities.Enemies.Gimmicks
{
    /// <summary>
    /// Trigger condition that determines when a gimmick activates.
    /// Each implementation is a small, single-responsibility component
    /// with strongly typed parameters.
    /// 
    /// Triggers are either voluntary (the AI chooses to activate as a turn action)
    /// or reactive (the combat resolver fires them in response to events).
    /// The IsVoluntary property tells the action system which category applies.
    /// </summary>
    public interface IGimmickTrigger
    {
        string Id { get; }

        /// <summary>
        /// Whether this trigger represents a voluntary action the AI can choose.
        /// True: appears in GetAvailableActions, AI picks it via utility scoring.
        /// False: fired reactively by the combat resolver (on hit, on adjacency, etc.).
        /// </summary>
        bool IsVoluntary { get; }

        /// <summary>
        /// Check if the trigger condition is met given the current context.
        /// For voluntary triggers, called by GetAvailableActions to determine eligibility.
        /// For reactive triggers, called by the combat resolver when the event occurs.
        /// </summary>
        bool ShouldActivate(GimmickContext context);

        /// <summary>
        /// Called after the gimmick fires. Resets any internal state
        /// (e.g., turn counters, one-shot flags).
        /// </summary>
        void OnActivated();

        /// <summary>
        /// Create a fresh instance of this trigger with reset state.
        /// Called by the Enemy constructor so each enemy instance gets its own
        /// trigger state. Without this, triggers with mutable state (like
        /// HealthThreshold's one-shot flag) would be shared across all enemies
        /// using the same EnemyData.
        /// 
        /// Stateless triggers can return 'this'.
        /// </summary>
        IGimmickTrigger CreateFresh();
    }

    /// <summary>
    /// What a gimmick does when it activates.
    /// Each implementation is a small, single-responsibility component
    /// with strongly typed parameters.
    /// 
    /// Behaviors produce a GimmickOutput that the combat resolver consumes,
    /// following the same declarative pattern as status effect EffectOutput.
    /// </summary>
    public interface IGimmickBehavior
    {
        string Id { get; }

        /// <summary>
        /// Produce the output describing what this gimmick does.
        /// The combat resolver reads the output and applies effects to the game state.
        /// </summary>
        GimmickOutput GetOutput(GimmickContext context);

        /// <summary>Range of the effect. 0 = self/aura, 1+ = targeted area.</summary>
        int Range { get; }
    }

    /// <summary>
    /// Context passed to gimmick triggers and behaviors.
    /// Provides the information needed to evaluate conditions and produce outputs
    /// without direct coupling to game systems.
    /// </summary>
    public class GimmickContext
    {
        /// <summary>The enemy that owns this gimmick.</summary>
        public object Owner { get; set; }

        /// <summary>Current health percentage of the owner (0-1).</summary>
        public float OwnerHealthPercentage { get; set; }

        /// <summary>Number of turns since the gimmick last activated.</summary>
        public int TurnsSinceLastActivation { get; set; }

        /// <summary>Whether a Ranger is adjacent (for adjacency triggers).</summary>
        public bool IsRangerAdjacent { get; set; }

        /// <summary>Whether the owner was just hit (for on-hit triggers).</summary>
        public bool WasJustHit { get; set; }

        /// <summary>MAG stat of the owner for scaling.</summary>
        public float OwnerMag { get; set; }
    }

    /// <summary>
    /// Declarative output describing what a gimmick does.
    /// The combat resolver reads this and applies effects.
    /// Multiple fields can be set — a gimmick can deal damage AND modify terrain.
    /// </summary>
    public class GimmickOutput
    {
        /// <summary>Damage to deal to targets in range.</summary>
        public float Damage { get; set; }

        /// <summary>Healing to apply to the owner.</summary>
        public float Healing { get; set; }

        /// <summary>Number of foot soldiers to spawn.</summary>
        public int SpawnCount { get; set; }

        /// <summary>Enemy data ID for spawned foot soldiers.</summary>
        public string SpawnEnemyDataId { get; set; }

        /// <summary>
        /// Maximum search radius for finding spawn positions.
        /// Spawns search expanding rings from the owner up to this radius.
        /// </summary>
        public int SpawnSearchRadius { get; set; } = 5;

        /// <summary>Status effect to apply to targets in range.</summary>
        public Weapons.StatusEffectTemplate StatusEffect { get; set; }

        /// <summary>Whether to activate a damage shield on the owner.</summary>
        public bool ActivateShield { get; set; }

        /// <summary>Duration of the shield in turns.</summary>
        public int ShieldDuration { get; set; }

        /// <summary>Whether to modify terrain around the owner.</summary>
        public bool ModifyTerrain { get; set; }

        /// <summary>Terrain type to set tiles to.</summary>
        public Core.Grid.TerrainType TargetTerrain { get; set; }

        /// <summary>Radius of terrain modification.</summary>
        public int TerrainRadius { get; set; }

        /// <summary>Shape of the terrain modification area.</summary>
        public Core.Grid.AreaShape TerrainShape { get; set; } = Core.Grid.AreaShape.Diamond;

        /// <summary>Displacement distance for push/pull effects.</summary>
        public int DisplacementDistance { get; set; }

        /// <summary>Whether displacement pushes (true) or pulls (false).</summary>
        public bool DisplacementPush { get; set; }

        /// <summary>Whether this output has any actual effect.</summary>
        public bool HasEffect => Damage > 0 || Healing > 0 || SpawnCount > 0
            || StatusEffect != null || ActivateShield || ModifyTerrain
            || DisplacementDistance > 0;

        public static GimmickOutput None => new GimmickOutput();
    }
}
