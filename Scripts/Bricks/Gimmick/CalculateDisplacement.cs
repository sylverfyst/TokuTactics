using System;
using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Gimmick
{
    /// <summary>
    /// Calculates where a unit ends up after being displaced (push/pull).
    /// Uses Bresenham-style cardinal stepping — each step is to an adjacent tile.
    /// Stops at the last valid position if blocked by wall, grid edge, or occupied tile.
    /// </summary>
    public static class CalculateDisplacement
    {
        public static GridPosition Execute(
            BattleGrid grid,
            GridPosition ownerPos,
            GridPosition targetPos,
            int distance,
            bool isPush)
        {
            int rawDc = isPush ? (targetPos.Col - ownerPos.Col) : (ownerPos.Col - targetPos.Col);
            int rawDr = isPush ? (targetPos.Row - ownerPos.Row) : (ownerPos.Row - targetPos.Row);

            if (rawDc == 0 && rawDr == 0) return targetPos;

            int absDc = Math.Abs(rawDc);
            int absDr = Math.Abs(rawDr);
            int signDc = Math.Sign(rawDc);
            int signDr = Math.Sign(rawDr);

            var current = targetPos;
            int stepsRemaining = distance;
            int error = absDc - absDr;

            while (stepsRemaining > 0)
            {
                GridPosition next;

                if (absDc == 0)
                    next = new GridPosition(current.Col, current.Row + signDr);
                else if (absDr == 0)
                    next = new GridPosition(current.Col + signDc, current.Row);
                else if (error > 0 || (error == 0 && absDc >= absDr))
                {
                    next = new GridPosition(current.Col + signDc, current.Row);
                    error -= absDr;
                }
                else
                {
                    next = new GridPosition(current.Col, current.Row + signDr);
                    error += absDc;
                }

                if (!grid.IsInBounds(next)) break;
                if (grid.IsTileBlocking(next)) break;

                var tile = grid.GetTile(next);
                if (tile != null && tile.IsOccupied) break;

                current = next;
                stepsRemaining--;
            }

            return current;
        }
    }
}
