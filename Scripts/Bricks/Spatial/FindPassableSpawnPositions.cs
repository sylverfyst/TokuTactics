using System.Collections.Generic;
using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Spatial
{
    /// <summary>
    /// Finds open tiles near a position for spawning units.
    /// Searches expanding rings from center up to maxSearchRadius.
    /// Returns up to 'count' passable, unoccupied positions.
    /// </summary>
    public static class FindPassableSpawnPositions
    {
        public static List<GridPosition> Execute(
            BattleGrid grid,
            GridPosition center,
            int count,
            int maxSearchRadius)
        {
            var positions = new List<GridPosition>();
            var seen = new HashSet<GridPosition>();

            for (int radius = 1; positions.Count < count && radius <= maxSearchRadius; radius++)
            {
                var candidates = grid.GetTilesInRange(center, radius);
                foreach (var pos in candidates)
                {
                    if (positions.Count >= count) break;
                    if (seen.Contains(pos)) continue;
                    seen.Add(pos);

                    if (grid.IsTilePassable(pos))
                        positions.Add(pos);
                }
            }

            return positions;
        }
    }
}
