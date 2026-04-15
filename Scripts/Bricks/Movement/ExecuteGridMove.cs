using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Movement
{
    /// <summary>
    /// Brick: Executes a unit movement on the battle grid.
    /// </summary>
    public static class ExecuteGridMove
    {
        /// <summary>
        /// Moves a unit to a new position on the grid.
        /// </summary>
        /// <param name="grid">The battle grid</param>
        /// <param name="unitId">ID of the unit to move</param>
        /// <param name="destination">Target grid position</param>
        /// <returns>True if the move was successful</returns>
        public static bool Execute(
            BattleGrid grid,
            string unitId,
            GridPosition destination)
        {
            if (grid == null || string.IsNullOrEmpty(unitId))
            {
                return false;
            }

            return grid.MoveUnit(unitId, destination);
        }
    }
}
