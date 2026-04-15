using System.Collections.Generic;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Result of a phase transition from end of player phase through enemy phase to next player turn.
    /// </summary>
    public class PhaseTransitionResult
    {
        /// <summary>Whether the mission ended during the transition (victory/defeat from status effects on round start).</summary>
        public bool MissionEnded { get; set; }

        /// <summary>The participant ID of the next unit to act (null if mission ended).</summary>
        public string NextUnitId { get; set; }

        /// <summary>The round number after transition.</summary>
        public int RoundNumber { get; set; }

        /// <summary>Enemy unit IDs that were processed during the enemy phase.</summary>
        public List<string> EnemyTurnsProcessed { get; set; } = new();

        public static PhaseTransitionResult MissionOver(int roundNumber)
        {
            return new PhaseTransitionResult
            {
                MissionEnded = true,
                RoundNumber = roundNumber
            };
        }

        public static PhaseTransitionResult NextPlayerTurn(string nextUnitId, int roundNumber, List<string> enemyTurns)
        {
            return new PhaseTransitionResult
            {
                MissionEnded = false,
                NextUnitId = nextUnitId,
                RoundNumber = roundNumber,
                EnemyTurnsProcessed = enemyTurns
            };
        }
    }
}
