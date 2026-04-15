using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Spatial;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Spatial
{
    public static class FindUnitsInRangeTests
    {
        public static void Run()
        {
            Test_InRange_Found();
            Test_OutOfRange_NotFound();
            Test_Range0_UsesAdjacency();
            Test_OnlyTargetIds_Filtered();
            Console.WriteLine("FindUnitsInRangeTests: All passed");
        }

        private static void Test_InRange_Found()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("r1", new GridPosition(4, 4));
            grid.PlaceUnit("e1", new GridPosition(5, 5));
            var targets = new HashSet<string> { "r1" };

            var result = FindUnitsInRange.Execute(grid, new GridPosition(5, 5), 3, targets);

            Assert(result.Count == 1, $"Expected 1, got {result.Count}");
            Assert(result[0] == "r1", "Should find r1");
        }

        private static void Test_OutOfRange_NotFound()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("r1", new GridPosition(0, 0));
            var targets = new HashSet<string> { "r1" };

            var result = FindUnitsInRange.Execute(grid, new GridPosition(9, 9), 3, targets);

            Assert(result.Count == 0, "Out of range target should not be found");
        }

        private static void Test_Range0_UsesAdjacency()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("r1", new GridPosition(5, 4)); // adjacent
            grid.PlaceUnit("r2", new GridPosition(5, 2)); // not adjacent
            var targets = new HashSet<string> { "r1", "r2" };

            var result = FindUnitsInRange.Execute(grid, new GridPosition(5, 5), 0, targets);

            Assert(result.Count == 1, $"Range 0 should only find adjacent, got {result.Count}");
            Assert(result[0] == "r1", "Should find adjacent r1");
        }

        private static void Test_OnlyTargetIds_Filtered()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("r1", new GridPosition(5, 4));
            grid.PlaceUnit("e1", new GridPosition(5, 6)); // enemy, not in targets
            var targets = new HashSet<string> { "r1" }; // only rangers

            var result = FindUnitsInRange.Execute(grid, new GridPosition(5, 5), 3, targets);

            Assert(result.Count == 1, "Should only find units in target set");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
