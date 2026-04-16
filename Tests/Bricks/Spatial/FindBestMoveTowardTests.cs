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
            Test_TieBreaksByGridPosition();
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

        private static void Test_TieBreaksByGridPosition()
        {
            // Two reachable tiles equidistant from the target — must always pick
            // the same one regardless of Dictionary iteration order.
            // GridPosition.CompareTo orders by Row then Col (lowest first).
            var grid = new BattleGrid(10, 10);
            var target = new GridPosition(5, 0);
            var current = new GridPosition(5, 5);
            // Both tiles are distance 4 from target (current is distance 5, so both improve).
            var tileA = new GridPosition(4, 3);  // CompareTo wins: same row, lower col
            var tileB = new GridPosition(6, 3);

            var range1 = new Dictionary<GridPosition, int> { { tileB, 3 }, { tileA, 3 }, { current, 0 } };
            var range2 = new Dictionary<GridPosition, int> { { tileA, 3 }, { tileB, 3 }, { current, 0 } };

            var r1 = FindBestMoveToward.Execute(grid, target, range1, current);
            var r2 = FindBestMoveToward.Execute(grid, target, range2, current);

            Assert(r1.HasValue && r2.HasValue, "Both should find a tile");
            Assert(r1.Value == r2.Value,
                $"Tied tiles should resolve identically regardless of insert order, got {r1.Value} vs {r2.Value}");
            Assert(r1.Value == tileA,
                $"Lower-CompareTo tile {tileA} should win, got {r1.Value}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
