using System.Collections.Generic;
using System.Linq;
using TokuTactics.Bricks.Phase;
using TokuTactics.Commands.Phase;
using TokuTactics.Core.Events;
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
    /// Orchestrator: Manages the ground combat turn loop.
    ///
    /// Owns all state (mission state, phase state, turn orders, unit lists).
    /// Delegates logic to Commands and Bricks. Publishes events based on command results.
    ///
    /// Lifecycle: StartMission → (StartRound → PlayerPhase → EnemyPhase)* → Victory/Defeat
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

            var initResult = InitializeMission.Execute(enemies, defeatTargetIds);
            _defeatTargetIds = initResult.DefeatTargetIds;

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
            if (!ValidateMissionActive.Execute(MissionState)) return false;

            var result = ExecuteRoundStart.Execute(
                RoundNumber, _rangers, _enemies, _defeatTargetIds, _formPool);

            RoundNumber = result.NewRoundNumber;

            // Publish demorph/aggression events from status effect processing
            foreach (var evt in result.DemorphEvents)
            {
                _eventBus.Publish(new FormDiedEvent
                {
                    RangerId = evt.RangerId,
                    FormId = evt.LostFormId
                });
                _eventBus.Publish(new RangerDemorphedEvent
                {
                    RangerId = evt.RangerId,
                    LostFormId = evt.LostFormId
                });
            }
            foreach (var evt in result.AggressionEvents)
            {
                _eventBus.Publish(new AggressionTriggeredEvent
                {
                    EnemyId = evt.EnemyId,
                    HealthPercentage = evt.HealthPercentage
                });
            }

            if (result.MissionEnded)
            {
                ApplyEndState(result.EndState, result.FallenRangerId);
                return false;
            }

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
            if (!ValidateMissionActive.Execute(MissionState)) return false;

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
            if (!ValidateMissionActive.Execute(MissionState)) return false;

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
            if (!ValidateMissionActive.Execute(MissionState)) return null;

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
            if (!ValidateMissionActive.Execute(MissionState)) return;

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

            var result = ResolveWinLoss.Execute(_rangers, _enemies, _defeatTargetIds);

            if (!result.Ended) return false;

            ApplyEndState(result.EndState, result.FallenRangerId);
            return true;
        }

        // === Internal: State Application ===

        /// <summary>
        /// Apply a mission end state from a command result. Publishes the appropriate event.
        /// </summary>
        private void ApplyEndState(MissionState endState, string fallenRangerId)
        {
            MissionState = endState;

            if (endState == MissionState.Defeat)
            {
                FallenRangerId = fallenRangerId;
                _eventBus.Publish(new MissionDefeatEvent
                {
                    FallenRangerId = fallenRangerId,
                    RoundsElapsed = RoundNumber
                });
            }
            else if (endState == MissionState.Victory)
            {
                _eventBus.Publish(new MissionVictoryEvent
                {
                    RoundsElapsed = RoundNumber
                });
            }

            _eventBus.Dispatch();
        }
    }
}
