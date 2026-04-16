using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Spatial;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Spatial
{
    public static class FindBestMoveTowardTests
    {
        public static void Run()
        {
            Test_FindsCloserTile();
            Test_SkipsOccupied();
            Test_NoImprovement_ReturnsNull();
            Test_EmptyRange_ReturnsNull();
            Console.WriteLine("FindBestMoveTowardTests: All passed");
        }

        private static void Test_FindsCloserTile()
        {
            var grid = new BattleGrid(10, 10);
            var target = new GridPosition(5, 0);
            var current = new GridPosition(5, 5);
            var range = new Dictionary<GridPosition, int>
            {
                { new GridPosition(5, 4), 1 },
                { new GridPosition(5, 3), 2 },
                { new GridPosition(5, 5), 0 }
            };

            var result = FindBestMoveToward.Execute(grid, target, range, current);

            Assert(result.HasValue, "Should find a tile");
            Assert(result.Value == new GridPosition(5, 3), $"Should pick closest to target, got ({result.Value.Col},{result.Value.Row})");
        }

        private static void Test_SkipsOccupied()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("blocker", new GridPosition(5, 3));
            var target = new GridPosition(5, 0);
            var current = new GridPosition(5, 5);
            var range = new Dictionary<GridPosition, int>
            {
                { new GridPosition(5, 4), 1 },
                { new GridPosition(5, 3), 2 },
                { new GridPosition(5, 5), 0 }
            };

            var result = FindBestMoveToward.Execute(grid, target, range, current);

            Assert(result.HasValue, "Should find a tile");
            Assert(result.Value == new GridPosition(5, 4), "Should skip occupied (5,3) and pick (5,4)");
        }

        private static void Test_NoImprovement_ReturnsNull()
        {
            var grid = new BattleGrid(10, 10);
            var target = new GridPosition(5, 5);
            var current = new GridPosition(5, 5); // Already at target
            var range = new Dictionary<GridPosition, int>
            {
                { new GridPosition(5, 6), 1 },
                { new GridPosition(5, 5), 0 }
            };

            var result = FindBestMoveToward.Execute(grid, target, range, current);

            Assert(!result.HasValue, "No improvement possible — should return null");
        }

        private static void Test_EmptyRange_ReturnsNull()
        {
            var grid = new BattleGrid(10, 10);
            var result = FindBestMoveToward.Execute(grid, new GridPosition(5, 0),
                new Dictionary<GridPosition, int>(), new GridPosition(5, 5));
            Assert(!result.HasValue, "Empty range should return null");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
