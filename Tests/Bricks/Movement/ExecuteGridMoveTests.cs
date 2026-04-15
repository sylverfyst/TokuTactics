using System;
using TokuTactics.Bricks.Movement;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Movement
{
    public static class ExecuteGridMoveTests
    {
        public static void Run()
        {
            Test_ValidMove_ReturnsTrue();
            Test_UpdatesUnitPosition();
            Test_OccupiedTile_ReturnsFalse();
            Test_NullGrid_ReturnsFalse();
            Console.WriteLine("ExecuteGridMoveTests: All passed");
        }

        private static void Test_ValidMove_ReturnsTrue()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));

            Assert(ExecuteGridMove.Execute(grid, "u1", new GridPosition(1, 1)) == true,
                "Valid move should return true");
        }

        private static void Test_UpdatesUnitPosition()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));

            ExecuteGridMove.Execute(grid, "u1", new GridPosition(2, 2));

            var pos = grid.GetUnitPosition("u1");
            Assert(pos.HasValue && pos.Value == new GridPosition(2, 2),
                $"Unit should be at (2,2), got {pos}");
        }

        private static void Test_OccupiedTile_ReturnsFalse()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));
            grid.PlaceUnit("u2", new GridPosition(1, 1));

            Assert(ExecuteGridMove.Execute(grid, "u1", new GridPosition(1, 1)) == false,
                "Move to occupied tile should return false");
        }

        private static void Test_NullGrid_ReturnsFalse()
        {
            Assert(ExecuteGridMove.Execute(null, "u1", new GridPosition(0, 0)) == false,
                "Null grid should return false");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
