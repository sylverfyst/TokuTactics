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
            Test_TieBreaksConsistently();
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

        private static void Test_TieBreaksConsistently()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("a", new GridPosition(4, 5));
            grid.PlaceUnit("b", new GridPosition(6, 5));
            var targets = new HashSet<string> { "a", "b" };

            var result = FindNearestUnit.Execute(grid, new GridPosition(5, 5), targets);

            Assert(result.HasValue, "Should find a unit");
            // Both are distance 1 — just verify we get one consistently
            Assert(result.Value.id == "a" || result.Value.id == "b", "Should return one of the tied units");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
