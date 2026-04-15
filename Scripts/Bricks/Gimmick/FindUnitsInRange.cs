using System.Collections.Generic;
using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Gimmick
{
    /// <summary>
    /// Finds target units within range of a position on the grid.
    /// Range 0 uses adjacency (for aura effects). Range 1+ uses Manhattan distance.
    /// </summary>
    public static class FindUnitsInRange
    {
        public static List<string> Execute(
            BattleGrid grid,
            GridPosition ownerPosition,
            int range,
            HashSet<string> targetUnitIds)
        {
            var inRange = new List<string>();

            if (range <= 0)
            {
                // Range 0 = aura — hit adjacent targets only
                var adjacent = grid.GetAdjacentUnits(ownerPosition);
                foreach (var id in adjacent)
                {
                    if (targetUnitIds.Contains(id))
                        inRange.Add(id);
                }
            }
            else
            {
                foreach (var targetId in targetUnitIds)
                {
                    var pos = grid.GetUnitPosition(targetId);
                    if (!pos.HasValue) continue;

                    if (ownerPosition.ManhattanDistance(pos.Value) <= range)
                        inRange.Add(targetId);
                }
            }

            return inRange;
        }
    }
}
