using System;
using System.Collections.Generic;
using TokuTactics.Systems.PhaseManagement;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Command: Transitions from end of player phase through enemy phase to the start
    /// of the next player turn. Enemy turn processing is injected so the orchestrator
    /// can swap auto-skip for real AI.
    /// </summary>
    public static class ExecutePhaseTransition
    {
        /// <summary>
        /// Delegate for processing a single enemy turn.
        /// Receives the enemy's participant ID. Called between AdvanceTurn and EndCurrentTurn.
        /// </summary>
        public delegate void EnemyTurnProcessor(string enemyId);

        /// <summary>
        /// Default enemy turn processor — does nothing (auto-skip).
        /// </summary>
        public static void AutoSkipEnemy(string enemyId) { }

        /// <summary>
        /// Execute the full phase transition: end player phase → run enemy phase → start next round → start next player phase.
        /// </summary>
        /// <param name="phaseManager">The phase manager owning turn/phase state</param>
        /// <param name="beginUnitTurn">Callback to initialize a unit's ActionBudget at turn start</param>
        /// <param name="processEnemyTurn">Injectable processor for each enemy turn (default: auto-skip)</param>
        public static PhaseTransitionResult Execute(
            PhaseManager phaseManager,
            Action<string> beginUnitTurn,
            EnemyTurnProcessor processEnemyTurn = null)
        {
            processEnemyTurn ??= AutoSkipEnemy;

            // End player phase
            phaseManager.EndPhase();

            // Start and run enemy phase
            phaseManager.StartEnemyPhase();
            var enemyTurns = new List<string>();

            while (!phaseManager.IsPhaseComplete())
            {
                var enemy = phaseManager.AdvanceTurn();
                if (enemy == null) break;

                string enemyId = enemy.Participant.ParticipantId;
                processEnemyTurn(enemyId);
                enemyTurns.Add(enemyId);
                phaseManager.EndCurrentTurn();
            }

            // End enemy phase
            phaseManager.EndPhase();

            // Start new round (ticks cooldowns, status effects, checks win/loss)
            if (!phaseManager.StartRound())
            {
                return PhaseTransitionResult.MissionOver(phaseManager.RoundNumber);
            }

            // Start new player phase
            phaseManager.StartPlayerPhase();
            var firstUnit = phaseManager.AdvanceTurn();
            if (firstUnit == null)
            {
                return PhaseTransitionResult.MissionOver(phaseManager.RoundNumber);
            }

            // Initialize the first unit's turn
            beginUnitTurn(firstUnit.Participant.ParticipantId);

            return PhaseTransitionResult.NextPlayerTurn(
                firstUnit.Participant.ParticipantId,
                phaseManager.RoundNumber,
                enemyTurns);
        }
    }
}
