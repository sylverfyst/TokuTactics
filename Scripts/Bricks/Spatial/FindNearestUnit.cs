using System.Collections.Generic;
using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Spatial
{
    /// <summary>
    /// Finds the nearest unit from a set of target IDs, by Manhattan distance from a position.
    /// Ties broken by ordinal unit ID (lowest wins) so results are deterministic
    /// regardless of HashSet iteration order.
    /// Returns the ID and position of the closest unit, or null if none found.
    /// </summary>
    public static class FindNearestUnit
    {
        public static (string id, GridPosition position)? Execute(
            BattleGrid grid,
            GridPosition fromPosition,
            HashSet<string> targetUnitIds)
        {
            string nearestId = null;
            GridPosition nearestPos = default;
            int nearestDistance = int.MaxValue;

            foreach (var unitId in targetUnitIds)
            {
                var pos = grid.GetUnitPosition(unitId);
                if (!pos.HasValue) continue;

                int distance = fromPosition.ManhattanDistance(pos.Value);

                bool wins = distance < nearestDistance
                    || (distance == nearestDistance
                        && string.CompareOrdinal(unitId, nearestId) < 0);

                if (wins)
                {
                    nearestDistance = distance;
                    nearestId = unitId;
                    nearestPos = pos.Value;
                }
            }

            if (nearestId == null) return null;
            return (nearestId, nearestPos);
        }
    }
}
