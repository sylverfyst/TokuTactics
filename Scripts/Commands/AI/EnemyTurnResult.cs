using TokuTactics.Core.Grid;

namespace TokuTactics.Commands.AI
{
    /// <summary>
    /// Declarative result of an enemy's turn decision.
    /// The orchestrator reads this and performs the actual move/attack.
    /// </summary>
    public class EnemyTurnResult
    {
        /// <summary>Tile to move to before attacking. Null if no move.</summary>
        public GridPosition? MoveDestination { get; set; }

        /// <summary>ID of the ranger to attack. Null if no attack.</summary>
        public string AttackTargetId { get; set; }

        /// <summary>Position of the attack target (for range validation by orchestrator).</summary>
        public GridPosition? AttackTargetPos { get; set; }

        /// <summary>True if the enemy couldn't do anything useful.</summary>
        public bool DidNothing { get; set; }

        public static EnemyTurnResult Nothing() => new EnemyTurnResult { DidNothing = true };
    }
}
