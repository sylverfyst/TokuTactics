using System.Collections.Generic;
using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Spatial
{
    /// <summary>
    /// Given a movement range dictionary and a target position, returns the reachable
    /// tile closest to the target (by Manhattan distance). Excludes occupied tiles.
    /// Ties broken by GridPosition.CompareTo (row then col, lowest wins) so results
    /// are deterministic regardless of Dictionary iteration order.
    /// Returns null if no valid move exists.
    /// </summary>
    public static class FindBestMoveToward
    {
        public static GridPosition? Execute(
            BattleGrid grid,
            GridPosition targetPosition,
            Dictionary<GridPosition, int> movementRange,
            GridPosition currentPosition)
        {
            if (movementRange == null || movementRange.Count == 0) return null;

            GridPosition? bestTile = null;
            int bestDistance = currentPosition.ManhattanDistance(targetPosition);

            foreach (var kvp in movementRange)
            {
                var pos = kvp.Key;
                if (pos == currentPosition) continue;

                // Can't move to occupied tiles
                var tile = grid.GetTile(pos);
                if (tile != null && tile.IsOccupied) continue;

                int distance = pos.ManhattanDistance(targetPosition);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTile = pos;
                }
                else if (distance == bestDistance && bestTile.HasValue
                    && pos.CompareTo(bestTile.Value) < 0)
                {
                    bestTile = pos;
                }
            }

            return bestTile;
        }
    }
}
