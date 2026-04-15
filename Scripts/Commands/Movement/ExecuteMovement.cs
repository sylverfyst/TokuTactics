using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Movement;
using TokuTactics.Core.Grid;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Commands.Movement
{
    /// <summary>
    /// Command: Orchestrates movement validation and execution.
    /// Pure static orchestrator - no state mutation beyond controlled side effects.
    /// </summary>
    public static class ExecuteMovement
    {
        /// <summary>
        /// Executes a unit movement with full validation.
        /// </summary>
        /// <param name="unitId">ID of the unit to move</param>
        /// <param name="destination">Target grid position</param>
        /// <param name="movementRange">Dictionary of reachable positions</param>
        /// <param name="grid">The battle grid</param>
        /// <param name="actionBudget">The unit's action budget</param>
        /// <param name="validateRange">Optional brick injection for testing</param>
        /// <param name="checkBudget">Optional brick injection for testing</param>
        /// <param name="executeGridMove">Optional brick injection for testing</param>
        /// <param name="consumeMove">Optional brick injection for testing</param>
        /// <returns>Result indicating success or failure with reason</returns>
        public static MoveResult Execute(
            string unitId,
            GridPosition destination,
            Dictionary<GridPosition, int> movementRange,
            BattleGrid grid,
            ActionBudget actionBudget,
            // Optional brick injections for testing
            Func<GridPosition, Dictionary<GridPosition, int>, bool> validateRange = null,
            Func<ActionBudget, bool> checkBudget = null,
            Func<BattleGrid, string, GridPosition, bool> executeGridMove = null,
            Func<ActionBudget, bool> consumeMove = null)
        {
            // Initialize defaults if not provided
            validateRange ??= ValidateMovementRange.Execute;
            checkBudget ??= CheckActionBudget.Execute;
            executeGridMove ??= ExecuteGridMove.Execute;
            consumeMove ??= ConsumeMoveBudget.Execute;

            // Validate destination is in movement range
            if (!validateRange(destination, movementRange))
            {
                return MoveResult.CreateFailure("Destination not in movement range");
            }

            // Check action budget
            if (!checkBudget(actionBudget))
            {
                return MoveResult.CreateFailure("Unit has already used their movement");
            }

            // Execute the move in the grid
            bool moveSucceeded = executeGridMove(grid, unitId, destination);
            if (!moveSucceeded)
            {
                return MoveResult.CreateFailure("Failed to move unit in grid");
            }

            // Consume movement budget
            consumeMove(actionBudget);

            return MoveResult.CreateSuccess(destination);
        }
    }
}
