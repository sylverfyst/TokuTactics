using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Entities.Enemies
{
    /// <summary>
    /// Enemy tier determines capabilities, action economy, and AI complexity.
    /// Each tier is a strict superset in threat level:
    /// 
    /// FootSoldier: basic attack (1 action), behavior tree, dangerous in numbers.
    /// Monster: basic attack + gimmick (2 actions, independent budgets), unique per episode.
    /// Lieutenant: basic attack + weapon + rotating gimmick (chooses 2 of 3), utility scoring.
    /// DarkRanger: full player mirror — dark forms, weapon system, full utility AI.
    /// </summary>
    public enum EnemyTier
    {
        FootSoldier,
        Monster,
        Lieutenant,
        DarkRanger
    }

    /// <summary>
    /// Static data definition for an enemy. Shared across all instances of this enemy type.
    /// 
    /// The tier determines which fields are relevant and the action economy:
    /// - FootSoldier: basic attack only. 1 action per turn.
    /// - Monster: basic attack + gimmick. 2 actions per turn (independent budgets).
    /// - Lieutenant: basic attack + weapon + rotating gimmick. Chooses 2 of 3 per turn
    ///   via utility scoring. The weapon is fixed (the lieutenant's identity), the gimmick
    ///   changes each encounter (the lieutenant's scheme).
    /// - DarkRanger: uses separate Ranger entity with dark form data (not this class).
    /// </summary>
    public class EnemyData
    {
        /// <summary>Unique identifier for this enemy type.</summary>
        public string Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Which tier of enemy this is.</summary>
        public EnemyTier Tier { get; }

        /// <summary>
        /// Elemental type. Can be null for typeless foot soldiers.
        /// When null, type matchups are always Neutral.
        /// </summary>
        public ElementalType? Type { get; }

        /// <summary>Base stats for this enemy.</summary>
        public StatBlock Stats { get; }

        /// <summary>Max health.</summary>
        public float MaxHealth { get; }

        /// <summary>Basic attack damage power.</summary>
        public float BasicAttackPower { get; }

        /// <summary>Basic attack range in tiles.</summary>
        public int BasicAttackRange { get; }

        /// <summary>Movement range in tiles.</summary>
        public int MovementRange { get; }

        // === Tier-Specific Fields ===

        /// <summary>
        /// Weapon for Lieutenants. One weapon with a status effect — fixed across encounters.
        /// This is the lieutenant's identity (their dark blade, chain whip, etc.).
        /// Null for FootSoldiers and Monsters.
        /// </summary>
        public WeaponData Weapon { get; }

        /// <summary>
        /// Gimmick for Monsters and Lieutenants. Composable trigger + behavior.
        /// For Monsters: defines the episode boss's unique mechanic.
        /// For Lieutenants: rotates each encounter — the player knows the weapon
        /// but can't predict the gimmick until they see it.
        /// Null for FootSoldiers.
        /// </summary>
        public GimmickData Gimmick { get; }

        /// <summary>
        /// Maximum actions this enemy can take per turn (excluding movement).
        /// FootSoldier: 1 (basic attack).
        /// Monster: 2 (basic attack + gimmick, independent budgets).
        /// Lieutenant: 2 (chooses 2 of 3: basic/weapon/gimmick, via utility scoring).
        /// This cap exists because lieutenants have 3 options but only 2 slots.
        /// </summary>
        public int ActionsPerTurn { get; }

        // === AI Configuration ===

        /// <summary>
        /// Behavior tree ID for normal behavior.
        /// All enemy tiers use behavior trees.
        /// </summary>
        public string BehaviorTreeId { get; }

        /// <summary>
        /// Behavior tree ID for aggressive behavior.
        /// Swapped in when health drops below AggressionThreshold.
        /// </summary>
        public string AggressiveBehaviorTreeId { get; }

        /// <summary>
        /// Health percentage (0-1) at which the enemy switches to aggressive behavior.
        /// Fixed per enemy type, not per instance. 0 = never switches.
        /// </summary>
        public float AggressionThreshold { get; }

        /// <summary>
        /// Whether this enemy uses utility scoring (Lieutenants, Dark Rangers).
        /// FootSoldiers and Monsters use pure behavior trees.
        /// </summary>
        public bool UsesUtilityScoring { get; }

        public EnemyData(
            string id, string name, EnemyTier tier,
            ElementalType? type, StatBlock stats, float maxHealth,
            float basicAttackPower, int basicAttackRange, int movementRange,
            string behaviorTreeId, string aggressiveBehaviorTreeId = null,
            float aggressionThreshold = 0.3f, bool usesUtilityScoring = false,
            int actionsPerTurn = 0,
            WeaponData weapon = null, GimmickData gimmick = null)
        {
            Id = id;
            Name = name;
            Tier = tier;
            Type = type;
            Stats = stats;
            MaxHealth = maxHealth;
            BasicAttackPower = basicAttackPower;
            BasicAttackRange = basicAttackRange;
            MovementRange = movementRange;
            BehaviorTreeId = behaviorTreeId;
            AggressiveBehaviorTreeId = aggressiveBehaviorTreeId;
            AggressionThreshold = aggressionThreshold;
            UsesUtilityScoring = usesUtilityScoring;
            Weapon = weapon;
            Gimmick = gimmick;

            // Default actions per turn based on tier if not specified
            ActionsPerTurn = actionsPerTurn > 0 ? actionsPerTurn : tier switch
            {
                EnemyTier.FootSoldier => 1,
                EnemyTier.Monster => 2,
                EnemyTier.Lieutenant => 2,
                _ => 1
            };
        }
    }

    /// <summary>
    /// Gimmick definition for Monsters and Lieutenants.
    /// Composed from an IGimmickTrigger (when it fires) and an IGimmickBehavior (what it does).
    /// 
    /// This follows the same composable pattern as status effects:
    /// small, single-responsibility components with strongly typed parameters.
    /// No more generic float params — each trigger and behavior class owns its own data.
    /// 
    /// For Monsters: defines the episode boss's unique mechanic.
    /// For Lieutenants: rotates each encounter — the player knows the weapon
    /// (fixed identity) but not the gimmick (rotating scheme).
    /// 
    /// The gimmick has its own action budget — the enemy can basic attack AND
    /// activate its gimmick in the same turn.
    /// 
    /// Examples:
    /// - new GimmickData("poison_aura", "Poison Aura",
    ///     new TurnStartGimmickTrigger(),
    ///     new StatusEffectGimmickBehavior(poisonTemplate, range: 1),
    ///     cooldown: 2)
    /// - new GimmickData("shield_phase", "Shield Phase",
    ///     new HealthThresholdGimmickTrigger(0.5f),
    ///     new ShieldGimmickBehavior(duration: 3))
    /// - new GimmickData("summon_wave", "Summon Wave",
    ///     new EveryNTurnsGimmickTrigger(3),
    ///     new SpawnGimmickBehavior(count: 4, "foot_basic"))
    /// </summary>
    public class GimmickData
    {
        /// <summary>Unique identifier for this gimmick.</summary>
        public string Id { get; }

        /// <summary>Display name shown to the player.</summary>
        public string Name { get; }

        /// <summary>
        /// Trigger component — determines when the gimmick activates.
        /// Knows whether it's voluntary (AI-chosen) or reactive (event-driven).
        /// </summary>
        public Gimmicks.IGimmickTrigger Trigger { get; }

        /// <summary>
        /// Behavior component — determines what the gimmick does.
        /// Produces a GimmickOutput consumed by the combat resolver.
        /// </summary>
        public Gimmicks.IGimmickBehavior Behavior { get; }

        /// <summary>
        /// Cooldown turns between gimmick activations.
        /// 0 = can activate every turn if trigger allows.
        /// </summary>
        public int Cooldown { get; }

        public GimmickData(
            string id, string name,
            Gimmicks.IGimmickTrigger trigger,
            Gimmicks.IGimmickBehavior behavior,
            int cooldown = 0)
        {
            Id = id;
            Name = name;
            Trigger = trigger;
            Behavior = behavior;
            Cooldown = cooldown;
        }
    }
}
