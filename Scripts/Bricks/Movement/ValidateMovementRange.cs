using System.Collections.Generic;
using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Movement
{
    /// <summary>
    /// Brick: Validates if a destination is within a unit's movement range.
    /// </summary>
    public static class ValidateMovementRange
    {
        /// <summary>
        /// Checks if the destination is in the movement range dictionary.
        /// </summary>
        /// <param name="destination">The target grid position</param>
        /// <param name="movementRange">Dictionary of reachable positions with their movement costs</param>
        /// <returns>True if destination is valid and reachable</returns>
        public static bool Execute(
            GridPosition destination,
            Dictionary<GridPosition, int> movementRange)
        {
            if (movementRange == null)
            {
                return false;
            }

            return movementRange.ContainsKey(destination);
        }
    }
}
