using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Spatial;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Spatial
{
    public static class FindNearestUnitTests
    {
        public static void Run()
        {
            Test_FindsClosest();
            Test_EmptyTargets_ReturnsNull();
            Test_TieBreaksByLowestId();
            Test_TieBreakIndependentOfInsertOrder();
            Console.WriteLine("FindNearestUnitTests: All passed");
        }

        private static void Test_FindsClosest()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("far", new GridPosition(9, 9));
            grid.PlaceUnit("near", new GridPosition(3, 3));
            var targets = new HashSet<string> { "far", "near" };

            var result = FindNearestUnit.Execute(grid, new GridPosition(2, 2), targets);

            Assert(result.HasValue, "Should find a unit");
            Assert(result.Value.id == "near", $"Should find nearest, got {result.Value.id}");
        }

        private static void Test_EmptyTargets_ReturnsNull()
        {
            var grid = new BattleGrid(10, 10);
            var result = FindNearestUnit.Execute(grid, new GridPosition(5, 5), new HashSet<string>());
            Assert(!result.HasValue, "Empty targets should return null");
        }

        private static void Test_TieBreaksByLowestId()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("a", new GridPosition(4, 5));
            grid.PlaceUnit("b", new GridPosition(6, 5));
            var targets = new HashSet<string> { "a", "b" };

            var result = FindNearestUnit.Execute(grid, new GridPosition(5, 5), targets);

            Assert(result.HasValue, "Should find a unit");
            // Both are distance 1 — ordinal-lowest ID wins ("a")
            Assert(result.Value.id == "a", $"Tie should resolve to 'a', got {result.Value.id}");
        }

        private static void Test_TieBreakIndependentOfInsertOrder()
        {
            // Same units, different insertion order — must produce the same winner.
            var grid1 = new BattleGrid(10, 10);
            grid1.PlaceUnit("zebra", new GridPosition(4, 5));
            grid1.PlaceUnit("alpha", new GridPosition(6, 5));

            var grid2 = new BattleGrid(10, 10);
            grid2.PlaceUnit("alpha", new GridPosition(6, 5));
            grid2.PlaceUnit("zebra", new GridPosition(4, 5));

            var targets1 = new HashSet<string> { "zebra", "alpha" };
            var targets2 = new HashSet<string> { "alpha", "zebra" };

            var r1 = FindNearestUnit.Execute(grid1, new GridPosition(5, 5), targets1);
            var r2 = FindNearestUnit.Execute(grid2, new GridPosition(5, 5), targets2);

            Assert(r1.HasValue && r2.HasValue, "Both should find a unit");
            Assert(r1.Value.id == r2.Value.id,
                $"Same inputs in different order should give same result, got {r1.Value.id} vs {r2.Value.id}");
            Assert(r1.Value.id == "alpha", $"Ordinal-lowest 'alpha' should win, got {r1.Value.id}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
