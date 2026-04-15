using System;
using TokuTactics.Bricks.Spatial;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Spatial
{
    public static class FindPassableSpawnPositionsTests
    {
        public static void Run()
        {
            Test_FindsPositions();
            Test_LimitedByCount();
            Test_SkipsOccupied();
            Console.WriteLine("FindPassableSpawnPositionsTests: All passed");
        }

        private static void Test_FindsPositions()
        {
            var grid = new BattleGrid(10, 10);
            var result = FindPassableSpawnPositions.Execute(
                grid, new GridPosition(5, 5), 3, 5);

            Assert(result.Count == 3, $"Should find 3 positions, got {result.Count}");
        }

        private static void Test_LimitedByCount()
        {
            var grid = new BattleGrid(10, 10);
            var result = FindPassableSpawnPositions.Execute(
                grid, new GridPosition(5, 5), 1, 5);

            Assert(result.Count == 1, $"Should be limited to 1, got {result.Count}");
        }

        private static void Test_SkipsOccupied()
        {
            var grid = new BattleGrid(10, 10);
            // Fill adjacent tiles
            grid.PlaceUnit("u1", new GridPosition(5, 4));
            grid.PlaceUnit("u2", new GridPosition(5, 6));
            grid.PlaceUnit("u3", new GridPosition(4, 5));
            grid.PlaceUnit("u4", new GridPosition(6, 5));

            var result = FindPassableSpawnPositions.Execute(
                grid, new GridPosition(5, 5), 2, 3);

            Assert(result.Count == 2, $"Should find 2 non-occupied positions, got {result.Count}");
            foreach (var pos in result)
            {
                var tile = grid.GetTile(pos);
                Assert(tile != null && !tile.IsOccupied, $"Position ({pos.Col},{pos.Row}) should not be occupied");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
