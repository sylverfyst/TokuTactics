using System;
using TokuTactics.Bricks.Gimmick;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Gimmick
{
    public static class CalculateDisplacementTests
    {
        public static void Run()
        {
            Test_Push_MovesAway();
            Test_Pull_MovesToward();
            Test_StopsAtEdge();
            Test_StopsAtOccupied();
            Test_SamePosition_NoDisplacement();
            Console.WriteLine("CalculateDisplacementTests: All passed");
        }

        private static void Test_Push_MovesAway()
        {
            var grid = new BattleGrid(10, 10);
            // Owner at (5,5), target at (5,4) — push should move target to (5,3) or further
            var result = CalculateDisplacement.Execute(
                grid, new GridPosition(5, 5), new GridPosition(5, 4), 2, isPush: true);

            Assert(result.Row < 4, $"Push should move target away (row < 4), got row={result.Row}");
            Assert(result.Col == 5, $"Pure vertical push should keep col=5, got col={result.Col}");
        }

        private static void Test_Pull_MovesToward()
        {
            var grid = new BattleGrid(10, 10);
            // Owner at (5,5), target at (5,2) — pull should move target toward owner
            var result = CalculateDisplacement.Execute(
                grid, new GridPosition(5, 5), new GridPosition(5, 2), 2, isPush: false);

            Assert(result.Row > 2, $"Pull should move target toward owner (row > 2), got row={result.Row}");
        }

        private static void Test_StopsAtEdge()
        {
            var grid = new BattleGrid(10, 10);
            // Owner at (5,5), target at (5,1) — push toward row 0 edge with large distance
            var result = CalculateDisplacement.Execute(
                grid, new GridPosition(5, 5), new GridPosition(5, 1), 10, isPush: true);

            Assert(result.Row >= 0, "Should not go out of bounds");
            Assert(grid.IsInBounds(result), "Result should be in bounds");
        }

        private static void Test_StopsAtOccupied()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("blocker", new GridPosition(5, 2));
            // Owner at (5,5), target at (5,4) — push toward (5,2) which is occupied
            var result = CalculateDisplacement.Execute(
                grid, new GridPosition(5, 5), new GridPosition(5, 4), 5, isPush: true);

            Assert(result != new GridPosition(5, 2), "Should not land on occupied tile");
            Assert(result.Row >= 3, $"Should stop before occupied tile, got row={result.Row}");
        }

        private static void Test_SamePosition_NoDisplacement()
        {
            var grid = new BattleGrid(10, 10);
            var pos = new GridPosition(5, 5);
            var result = CalculateDisplacement.Execute(grid, pos, pos, 3, isPush: true);

            Assert(result == pos, "Same position should return same position");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
