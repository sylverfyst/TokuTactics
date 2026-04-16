using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Spatial;
using TokuTactics.Core.Grid;

namespace TokuTactics.Commands.AI
{
    /// <summary>
    /// Command: Determines what an enemy should do on their turn.
    /// Finds nearest ranger, decides whether to move and/or attack.
    /// Returns a declarative result — does NOT mutate state.
    /// </summary>
    public static class ResolveEnemyTurn
    {
        public static EnemyTurnResult Execute(
            BattleGrid grid,
            string enemyId,
            int movementRange,
            int attackRange,
            HashSet<string> rangerIds,
            Func<BattleGrid, GridPosition, HashSet<string>, (string id, GridPosition position)?> findNearest = null,
            Func<BattleGrid, GridPosition, Dictionary<GridPosition, int>, GridPosition, GridPosition?> findBestMove = null)
        {
            findNearest ??= FindNearestUnit.Execute;
            findBestMove ??= FindBestMoveToward.Execute;

            var enemyPos = grid.GetUnitPosition(enemyId);
            if (!enemyPos.HasValue) return EnemyTurnResult.Nothing();

            // Find nearest ranger
            var nearest = findNearest(grid, enemyPos.Value, rangerIds);
            if (nearest == null) return EnemyTurnResult.Nothing();

            var (targetId, targetPos) = nearest.Value;

            // Check if already in attack range
            if (enemyPos.Value.ManhattanDistance(targetPos) <= attackRange)
            {
                return new EnemyTurnResult
                {
                    AttackTargetId = targetId,
                    AttackTargetPos = targetPos
                };
            }

            // Not in range — try to move closer
            var reachable = grid.GetMovementRange(enemyPos.Value, movementRange);
            var bestMove = findBestMove(grid, targetPos, reachable, enemyPos.Value);

            if (bestMove == null) return EnemyTurnResult.Nothing();

            var result = new EnemyTurnResult
            {
                MoveDestination = bestMove.Value
            };

            // After moving, check if we'd be in attack range
            if (bestMove.Value.ManhattanDistance(targetPos) <= attackRange)
            {
                result.AttackTargetId = targetId;
                result.AttackTargetPos = targetPos;
            }

            return result;
        }
    }
}
