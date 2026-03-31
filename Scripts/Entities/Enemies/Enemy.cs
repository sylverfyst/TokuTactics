using System.Collections.Generic;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.Health;
using TokuTactics.Core.Cooldown;
using TokuTactics.Core.Combat;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Entities.Enemies.Gimmicks;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Entities.Enemies
{
    /// <summary>
    /// Runtime state for an enemy instance on the battlefield.
    /// Implements ICombatActor — can attack and be attacked.
    /// Implements ITurnParticipant — participates in phase turn ordering.
    /// 
    /// Enemies use single types (not dual). Their DualType is always Single(Type).
    /// Typeless enemies use Normal — purely neutral, no strengths or weaknesses.
    /// 
    /// The Enemy entity manages:
    /// - Health and alive state
    /// - Aggression threshold and behavior tree switching
    /// - Action slots per turn (1 for foot soldiers, 2 for monsters/lieutenants)
    /// - Per-turn tracking of which action types were used (prevents double-picking)
    /// - Gimmick cooldown tracking (trigger/behavior components own their own state)
    /// - Status effects
    /// - Weapon attack availability (Lieutenants only)
    /// 
    /// Gimmick triggers and behaviors are composable components following the same
    /// pattern as status effects. The trigger knows whether it's voluntary or reactive.
    /// The behavior produces a declarative GimmickOutput consumed by the combat resolver.
    /// 
    /// Dark Rangers are NOT handled by this class — they use the Ranger entity
    /// with dark form data. This class covers FootSoldier, Monster, and Lieutenant.
    /// </summary>
    public class Enemy : ICombatActor, ITurnParticipant
    {
        // === Identity ===

        /// <summary>Unique instance ID (e.g., "footsoldier_3", "monster_episode5").</summary>
        public string Id { get; }

        /// <summary>Static data definition shared across instances of this type.</summary>
        public EnemyData Data { get; }

        // === Health ===

        /// <summary>The enemy's health pool.</summary>
        public HealthPool Health { get; }

        /// <summary>Whether this enemy is alive.</summary>
        public bool IsAlive => Health.IsAlive;

        // === Status Effects ===

        /// <summary>Active status effects on this enemy.</summary>
        public StatusEffectTracker StatusEffects { get; } = new();

        // === Aggression ===

        /// <summary>
        /// Whether this enemy has crossed its aggression threshold.
        /// Once true, stays true for the rest of the fight.
        /// The AI system reads this to decide which behavior tree to use.
        /// </summary>
        public bool IsAggressive { get; private set; }

        // === Gimmick State ===

        /// <summary>
        /// Per-instance trigger created via CreateFresh() in the constructor.
        /// Stateful triggers (like HealthThreshold) need their own copy per enemy
        /// so multiple instances of the same EnemyData don't share mutable state.
        /// Null when the enemy has no gimmick.
        /// </summary>
        public IGimmickTrigger GimmickTrigger { get; }

        /// <summary>
        /// Cooldown tracker for the gimmick.
        /// Null for enemies without gimmicks or gimmicks with no cooldown.
        /// When null and the gimmick exists, the gimmick can activate every turn
        /// if its trigger allows — the null cooldown check is intentionally skipped.
        /// </summary>
        public CooldownTimer GimmickCooldown { get; }

        /// <summary>Turn counter for gimmick triggers that need it (EveryNTurns).</summary>
        public int TurnsSinceLastGimmick { get; private set; }

        /// <summary>Whether the gimmick's shield effect is currently active.</summary>
        public bool IsShielded { get; private set; }

        /// <summary>Remaining turns of shield, if active.</summary>
        public int ShieldTurnsRemaining { get; private set; }

        // === Action Slots ===

        /// <summary>How many actions this enemy has used this turn.</summary>
        public int ActionsUsedThisTurn { get; private set; }

        /// <summary>Which action types have been used this turn.</summary>
        private readonly HashSet<EnemyActionType> _actionsUsedThisTurn = new();

        /// <summary>Read-only access to which actions were used this turn.</summary>
        public IReadOnlyCollection<EnemyActionType> ActionsUsed => _actionsUsedThisTurn;

        /// <summary>Maximum actions per turn from data.</summary>
        public int MaxActionsPerTurn => Data.ActionsPerTurn;

        /// <summary>Whether this enemy has action slots remaining.</summary>
        public bool CanUseAction => ActionsUsedThisTurn < Data.ActionsPerTurn;

        /// <summary>
        /// Consume one action slot of a specific type.
        /// Records the type so GetAvailableActions excludes it next call.
        /// Returns false if no slots remain or action type already used.
        /// </summary>
        public bool ConsumeAction(EnemyActionType actionType)
        {
            if (!CanUseAction) return false;
            if (_actionsUsedThisTurn.Contains(actionType)) return false;

            ActionsUsedThisTurn++;
            _actionsUsedThisTurn.Add(actionType);
            return true;
        }

        /// <summary>
        /// Get available action types for this enemy given current state.
        /// Automatically excludes actions already used this turn.
        /// Only includes voluntary gimmick triggers — reactive triggers are
        /// handled by the combat resolver, not the AI's action selection.
        /// </summary>
        public List<EnemyActionType> GetAvailableActions()
        {
            var actions = new List<EnemyActionType>();
            if (!IsAlive) return actions;

            if (!_actionsUsedThisTurn.Contains(EnemyActionType.BasicAttack))
                actions.Add(EnemyActionType.BasicAttack);

            if (HasWeapon && !_actionsUsedThisTurn.Contains(EnemyActionType.WeaponAttack))
                actions.Add(EnemyActionType.WeaponAttack);

            if (HasGimmick && !_actionsUsedThisTurn.Contains(EnemyActionType.Gimmick)
                && GimmickTrigger.IsVoluntary
                && ShouldGimmickActivate(BuildGimmickContext()))
                actions.Add(EnemyActionType.Gimmick);

            return actions;
        }

        // === Constructor ===

        public Enemy(string id, EnemyData data)
        {
            Id = id;
            Data = data;
            Health = new HealthPool(data.MaxHealth);

            if (data.Gimmick != null)
            {
                GimmickTrigger = data.Gimmick.Trigger.CreateFresh();

                if (data.Gimmick.Cooldown > 0)
                    GimmickCooldown = new CooldownTimer(data.Gimmick.Cooldown);
            }
        }

        // === ICombatActor Implementation ===

        /// <summary>
        /// Enemies have single type. Typeless enemies use Normal — purely neutral.
        /// </summary>
        public DualType DualType => Data.Type.HasValue
            ? DualType.Single(Data.Type.Value)
            : DualType.Single(ElementalType.Normal);

        /// <summary>Elemental type for defensive calculations.</summary>
        ElementalType ICombatTarget.Type => Data.Type ?? ElementalType.Normal;

        /// <summary>Current stats. Enemies don't level — stats are fixed from data.</summary>
        public StatBlock Stats => Data.Stats;

        /// <summary>Enemies don't form-switch. Always full damage.</summary>
        public float ComboScaleMultiplier => 1.0f;

        // === ITurnParticipant Implementation ===

        string ITurnParticipant.ParticipantId => Id;
        float ITurnParticipant.Speed => Stats.Get(StatType.SPD);
        bool ITurnParticipant.CanAct => IsAlive;

        // === Damage ===

        /// <summary>
        /// Apply damage to this enemy.
        /// Returns an EnemyDamageEvent describing what happened.
        /// </summary>
        public EnemyDamageEvent TakeDamage(float amount)
        {
            if (IsShielded)
            {
                return new EnemyDamageEvent
                {
                    EnemyId = Id,
                    DamageDealt = 0,
                    WasShielded = true
                };
            }

            float actualDamage = Health.TakeDamage(amount);

            var evt = new EnemyDamageEvent
            {
                EnemyId = Id,
                DamageDealt = actualDamage,
                Died = !Health.IsAlive
            };

            // Check aggression threshold even if the enemy died from this hit
            // (for event logging and AI state tracking)
            if (!IsAggressive && Data.AggressionThreshold > 0)
            {
                if (Health.Percentage <= Data.AggressionThreshold)
                {
                    IsAggressive = true;
                    evt.BecameAggressive = true;
                }
            }

            return evt;
        }

        // === Shield ===

        /// <summary>Activate the shield effect. Only the gimmick resolver should call this.</summary>
        public void ActivateShield(int duration)
        {
            IsShielded = true;
            ShieldTurnsRemaining = duration;
        }

        // === Gimmick ===

        /// <summary>
        /// Build the gimmick context from this enemy's current state.
        /// Used for turn-based trigger checks. The combat resolver can create
        /// a richer context with spatial info for reactive triggers.
        /// </summary>
        public GimmickContext BuildGimmickContext()
        {
            return new GimmickContext
            {
                Owner = this,
                OwnerHealthPercentage = Health.Percentage,
                TurnsSinceLastActivation = TurnsSinceLastGimmick,
                OwnerMag = Stats.Get(StatType.MAG)
            };
        }

        /// <summary>
        /// Check if the gimmick should activate given a context.
        /// Checks cooldown first, then delegates to the trigger component.
        /// </summary>
        public bool ShouldGimmickActivate(GimmickContext context)
        {
            if (GimmickTrigger == null) return false;
            if (GimmickCooldown != null && GimmickCooldown.IsOnCooldown) return false;

            return GimmickTrigger.ShouldActivate(context);
        }

        /// <summary>
        /// Mark the gimmick as activated. Starts cooldown, resets turn counter,
        /// and notifies the trigger component (for one-shot triggers like HealthThreshold).
        /// </summary>
        public void OnGimmickActivated()
        {
            GimmickCooldown?.Activate();
            TurnsSinceLastGimmick = 0;
            GimmickTrigger?.OnActivated();
        }

        /// <summary>
        /// Get the gimmick's declarative output for the combat resolver.
        /// Returns null if no gimmick or gimmick shouldn't activate.
        /// </summary>
        public GimmickOutput GetGimmickOutput(GimmickContext context)
        {
            if (Data.Gimmick == null) return null;
            return Data.Gimmick.Behavior.GetOutput(context);
        }

        // === Turn Processing ===

        /// <summary>
        /// Process the start of this enemy's turn.
        /// Resets action slots, ticks gimmick cooldown, increments turn counter, ticks shield.
        /// </summary>
        public void StartTurn()
        {
            ActionsUsedThisTurn = 0;
            _actionsUsedThisTurn.Clear();
            TurnsSinceLastGimmick++;
            GimmickCooldown?.Tick();

            if (IsShielded)
            {
                ShieldTurnsRemaining--;
                if (ShieldTurnsRemaining <= 0)
                {
                    IsShielded = false;
                }
            }
        }

        // === Queries ===

        /// <summary>Whether this enemy has a weapon attack (Lieutenants).</summary>
        public bool HasWeapon => Data.Weapon != null;

        /// <summary>Whether this enemy has a gimmick (Monsters and Lieutenants).</summary>
        public bool HasGimmick => Data.Gimmick != null;

        /// <summary>Whether the gimmick is a voluntary action the AI can choose.</summary>
        public bool IsGimmickVoluntary => GimmickTrigger?.IsVoluntary ?? false;

        /// <summary>
        /// Get the behavior tree ID the AI should use for this enemy right now.
        /// </summary>
        public string ActiveBehaviorTreeId =>
            IsAggressive && Data.AggressiveBehaviorTreeId != null
                ? Data.AggressiveBehaviorTreeId
                : Data.BehaviorTreeId;
    }

    /// <summary>
    /// Action types an enemy can choose from.
    /// </summary>
    public enum EnemyActionType
    {
        BasicAttack,
        WeaponAttack,
        Gimmick
    }

    /// <summary>
    /// Event describing the result of damage dealt to an enemy.
    /// </summary>
    public class EnemyDamageEvent
    {
        public string EnemyId { get; set; }
        public float DamageDealt { get; set; }
        public bool Died { get; set; }
        public bool BecameAggressive { get; set; }
        public bool WasShielded { get; set; }
    }
}
