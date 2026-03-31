using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Events;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.FormManagement;

namespace TokuTactics.Systems.PhaseManagement
{
    /// <summary>
    /// Mission state machine tracking the overall lifecycle.
    /// </summary>
    public enum MissionState
    {
        /// <summary>Mission not yet started.</summary>
        NotStarted,

        /// <summary>Ground combat is active.</summary>
        Active,

        /// <summary>All required enemies defeated.</summary>
        Victory,

        /// <summary>An unmorphed Ranger was killed.</summary>
        Defeat
    }

    /// <summary>
    /// Where we are within a round.
    /// </summary>
    public enum PhaseState
    {
        /// <summary>Between rounds or before first round.</summary>
        Idle,

        /// <summary>Player units are acting in SPD order.</summary>
        PlayerPhase,

        /// <summary>Enemy units are acting in SPD order.</summary>
        EnemyPhase
    }

    /// <summary>
    /// Orchestrates the ground combat turn loop.
    /// 
    /// Lifecycle: StartMission → (StartRound → PlayerPhase → EnemyPhase)* → Victory/Defeat
    /// 
    /// Within each phase, units act in SPD order via TurnOrder.
    /// The phase manager does NOT execute combat — it manages flow and tells
    /// the game layer whose turn it is. The game layer calls the CombatResolver
    /// when the active unit attacks.
    /// 
    /// Round start processing:
    /// - Tick form cooldowns (FormPool.ProcessTurn)
    /// - Tick status effects on all units
    /// - Reset combo chains
    /// - Reset bond refresh flags (ActionBudget.StartTurn handles this)
    /// 
    /// Win/loss:
    /// - Victory: all enemies in the defeat list are dead (bosses/monsters)
    /// - Defeat: any unmorphed Ranger killed (flagged by CombatResolver.MissionLost)
    /// 
    /// The phase manager publishes events at each transition point so the
    /// presentation layer can play phase banners, music changes, etc.
    /// </summary>
    public class PhaseManager
    {
        private readonly EventBus _eventBus;
        private readonly FormPool _formPool;
        private readonly TurnOrder _playerTurnOrder = new();
        private readonly TurnOrder _enemyTurnOrder = new();

        private List<Ranger> _rangers = new();
        private List<Enemy> _enemies = new();
        private HashSet<string> _defeatTargetIds = new();

        /// <summary>Current mission state.</summary>
        public MissionState MissionState { get; private set; } = MissionState.NotStarted;

        /// <summary>Current phase within a round.</summary>
        public PhaseState PhaseState { get; private set; } = PhaseState.Idle;

        /// <summary>Current round number (1-indexed).</summary>
        public int RoundNumber { get; private set; }

        /// <summary>The unit whose turn it currently is (null between turns).</summary>
        public TurnEntry ActiveUnit { get; private set; }

        /// <summary>Player phase turn order for the current round.</summary>
        public TurnOrder PlayerTurnOrder => _playerTurnOrder;

        /// <summary>Enemy phase turn order for the current round.</summary>
        public TurnOrder EnemyTurnOrder => _enemyTurnOrder;

        /// <summary>ID of the Ranger who died if mission was lost.</summary>
        public string FallenRangerId { get; private set; }

        public PhaseManager(EventBus eventBus, FormPool formPool)
        {
            _eventBus = eventBus;
            _formPool = formPool;
        }

        // === Mission Lifecycle ===

        /// <summary>
        /// Initialize the mission with participating units.
        /// 
        /// defeatTargetIds: enemy IDs that must be killed to win (bosses/monsters).
        /// If empty, ALL enemies must be killed to win.
        /// </summary>
        public void StartMission(
            List<Ranger> rangers,
            List<Enemy> enemies,
            HashSet<string> defeatTargetIds = null)
        {
            _rangers = rangers;
            _enemies = enemies;
            _defeatTargetIds = defeatTargetIds ?? new HashSet<string>(
                enemies.Select(e => e.Id));

            MissionState = MissionState.Active;
            PhaseState = PhaseState.Idle;
            RoundNumber = 0;
        }

        /// <summary>
        /// Start a new round. Ticks cooldowns, status effects, resets chains.
        /// Call this to begin round 1 and at the start of each subsequent round.
        /// Returns false if the mission is already over.
        /// </summary>
        public bool StartRound()
        {
            if (MissionState != MissionState.Active) return false;

            RoundNumber++;

            // Tick form cooldowns and passive health regen
            _formPool.ProcessTurn();

            // Tick status effects on all units
            ProcessStatusEffects();

            // Reset combo chains for all Rangers
            foreach (var ranger in _rangers)
            {
                if (ranger.IsAlive)
                    ranger.ComboScaler.ResetChain();
            }

            // Check if status effect ticks killed anyone
            if (CheckWinLoss()) return false;

            _eventBus.Publish(new RoundStartedEvent { RoundNumber = RoundNumber });
            _eventBus.Dispatch();

            return true;
        }

        // === Phase Transitions ===

        /// <summary>
        /// Start the player phase. Builds turn order from alive Rangers sorted by SPD.
        /// Returns false if the mission is over.
        /// </summary>
        public bool StartPlayerPhase()
        {
            if (MissionState != MissionState.Active) return false;

            PhaseState = PhaseState.PlayerPhase;

            _playerTurnOrder.Build(_rangers.Cast<ITurnParticipant>());

            _eventBus.Publish(new PlayerPhaseStartedEvent { RoundNumber = RoundNumber });
            _eventBus.Dispatch();

            return true;
        }

        /// <summary>
        /// Start the enemy phase. Builds turn order from alive enemies sorted by SPD.
        /// Returns false if the mission is over.
        /// </summary>
        public bool StartEnemyPhase()
        {
            if (MissionState != MissionState.Active) return false;

            PhaseState = PhaseState.EnemyPhase;

            _enemyTurnOrder.Build(_enemies.Cast<ITurnParticipant>());

            _eventBus.Publish(new EnemyPhaseStartedEvent { RoundNumber = RoundNumber });
            _eventBus.Dispatch();

            return true;
        }

        // === Turn Management ===

        /// <summary>
        /// Advance to the next unit in the current phase.
        /// Returns the TurnEntry for the next unit, or null if the phase is complete.
        /// </summary>
        public TurnEntry AdvanceTurn()
        {
            if (MissionState != MissionState.Active) return null;

            var turnOrder = PhaseState == PhaseState.PlayerPhase
                ? _playerTurnOrder
                : _enemyTurnOrder;

            ActiveUnit = turnOrder.Advance();

            if (ActiveUnit != null)
            {
                _eventBus.Publish(new UnitTurnStartedEvent
                {
                    UnitId = ActiveUnit.Participant.ParticipantId,
                    RoundNumber = RoundNumber
                });
                _eventBus.Dispatch();
            }

            return ActiveUnit;
        }

        /// <summary>
        /// End the current unit's turn. Call after the unit has finished all actions.
        /// </summary>
        public void EndCurrentTurn()
        {
            if (ActiveUnit == null) return;

            ActiveUnit.HasActed = true;

            _eventBus.Publish(new UnitTurnEndedEvent
            {
                UnitId = ActiveUnit.Participant.ParticipantId
            });
            _eventBus.Dispatch();

            ActiveUnit = null;
        }

        /// <summary>
        /// Check if the current phase is complete (all units have acted or advanced past).
        /// </summary>
        public bool IsPhaseComplete()
        {
            var turnOrder = PhaseState == PhaseState.PlayerPhase
                ? _playerTurnOrder
                : _enemyTurnOrder;

            return turnOrder.IsPhaseComplete;
        }

        /// <summary>
        /// End the current phase and return to idle.
        /// </summary>
        public void EndPhase()
        {
            PhaseState = PhaseState.Idle;
            ActiveUnit = null;
        }

        // === Win/Loss ===

        /// <summary>
        /// Notify the phase manager that a mission-ending event occurred.
        /// Call this after CombatResult.MissionLost is true.
        /// </summary>
        public void NotifyMissionLost(string fallenRangerId)
        {
            if (MissionState != MissionState.Active) return;

            FallenRangerId = fallenRangerId;
            MissionState = MissionState.Defeat;

            _eventBus.Publish(new MissionDefeatEvent
            {
                FallenRangerId = fallenRangerId,
                RoundsElapsed = RoundNumber
            });
            _eventBus.Dispatch();
        }

        /// <summary>
        /// Check win/loss conditions. Called after combat actions and status effect ticks.
        /// Returns true if the mission ended.
        /// </summary>
        public bool CheckWinLoss()
        {
            if (MissionState != MissionState.Active) return true;

            // Loss: any Ranger dead (unmorphed death)
            foreach (var ranger in _rangers)
            {
                if (!ranger.IsAlive)
                {
                    NotifyMissionLost(ranger.Id);
                    return true;
                }
            }

            // Win: all defeat targets are dead
            bool allTargetsDefeated = _defeatTargetIds.All(
                id => _enemies.Any(e => e.Id == id && !e.IsAlive));

            if (allTargetsDefeated)
            {
                MissionState = MissionState.Victory;

                _eventBus.Publish(new MissionVictoryEvent
                {
                    RoundsElapsed = RoundNumber
                });
                _eventBus.Dispatch();

                return true;
            }

            return false;
        }

        // === Internal: Status Effect Processing ===

        /// <summary>
        /// Process and tick status effects on all units.
        /// 
        /// Two-phase processing per unit:
        /// 1. Process() — fire triggers (TurnStart), get per-tick outputs (DoT, HoT, stun)
        /// 2. TickAndClean() — decrement durations, remove expired effects, get removal outputs
        /// 
        /// Currently applies: Damage (DoT), Healing (HoT).
        /// NOT YET applied: StatModifiers (needs stat modifier layer),
        /// PreventsAction/stun (needs action prevention check in AdvanceTurn),
        /// MovementMultiplier (needs movement system integration).
        /// These are vertical slice gaps — the EffectOutput fields exist,
        /// the processing doesn't consume them yet.
        /// </summary>
        private void ProcessStatusEffects()
        {
            var context = new EffectContext { Phase = "turn_start" };

            foreach (var ranger in _rangers)
            {
                if (!ranger.IsAlive) continue;

                // Phase 1: Fire per-tick effects (DoT, HoT from TurnStartTrigger)
                var tickOutputs = ranger.StatusEffects.Process(context);
                ApplyEffectOutputsToRanger(ranger, tickOutputs);

                // Phase 2: Tick durations and remove expired effects
                var removalOutputs = ranger.StatusEffects.TickAndClean(context);
                // Removal outputs undo stat mods etc. — not yet consumed.

                // Check if DoT killed the current form — trigger demorph
                if (ranger.MorphState == MorphState.Morphed
                    && ranger.CurrentForm != null
                    && !ranger.CurrentForm.Health.IsAlive)
                {
                    var lostForm = ranger.Demorph();

                    _eventBus.Publish(new FormDiedEvent
                    {
                        RangerId = ranger.Id,
                        FormId = lostForm?.Data.Id
                    });
                    _eventBus.Publish(new RangerDemorphedEvent
                    {
                        RangerId = ranger.Id,
                        LostFormId = lostForm?.Data.Id
                    });
                }
            }

            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive) continue;

                // Phase 1: Fire per-tick effects
                var tickOutputs = enemy.StatusEffects.Process(context);
                ApplyEffectOutputsToEnemy(enemy, tickOutputs);

                // Phase 2: Tick durations and remove expired
                var removalOutputs = enemy.StatusEffects.TickAndClean(context);
            }
        }

        private void ApplyEffectOutputsToRanger(Ranger ranger, List<EffectOutput> outputs)
        {
            foreach (var effect in outputs)
            {
                if (effect.Damage > 0)
                {
                    if (ranger.MorphState == MorphState.Morphed && ranger.CurrentForm != null)
                        ranger.CurrentForm.Health.TakeDamage(effect.Damage);
                    else
                        ranger.UnmorphedHealth.TakeDamage(effect.Damage);
                }
                if (effect.Healing > 0)
                {
                    if (ranger.MorphState == MorphState.Morphed && ranger.CurrentForm != null)
                        ranger.CurrentForm.Health.Heal(effect.Healing);
                    else
                        ranger.UnmorphedHealth.Heal(effect.Healing);
                }
            }
        }

        private void ApplyEffectOutputsToEnemy(Enemy enemy, List<EffectOutput> outputs)
        {
            foreach (var effect in outputs)
            {
                if (effect.Damage > 0)
                {
                    // Use Enemy.TakeDamage to respect aggression threshold
                    var damageEvt = enemy.TakeDamage(effect.Damage);
                    if (damageEvt.BecameAggressive)
                    {
                        _eventBus.Publish(new AggressionTriggeredEvent
                        {
                            EnemyId = enemy.Id,
                            HealthPercentage = enemy.Health.Percentage
                        });
                    }
                }
                if (effect.Healing > 0)
                    enemy.Health.Heal(effect.Healing);
            }
        }
    }
}
