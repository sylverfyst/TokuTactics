using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Movement;
using TokuTactics.Bricks.Shared;
using TokuTactics.Commands.Movement;
using TokuTactics.Core.Grid;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Commands.Movement
{
    public static class ExecuteMovementTests
    {
        public static void Run()
        {
            Test_ValidMove_Succeeds();
            Test_OutOfRange_Fails();
            Test_NoBudget_Fails();
            Test_GridMoveFails_ReportsFailure();
            Test_ConsumesBudgetOnSuccess();
            Test_UsesInjectedBricks();
            Console.WriteLine("ExecuteMovementTests: All passed");
        }

        private static void Test_ValidMove_Succeeds()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));
            var budget = MakeBudget();
            var range = new Dictionary<GridPosition, int> { { new GridPosition(1, 1), 1 } };

            var result = ExecuteMovement.Execute("u1", new GridPosition(1, 1), range, grid, budget);

            Assert(result.Success, "Valid move should succeed");
            Assert(result.NewPosition == new GridPosition(1, 1), "Should report new position");
        }

        private static void Test_OutOfRange_Fails()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));
            var budget = MakeBudget();
            var range = new Dictionary<GridPosition, int> { { new GridPosition(1, 1), 1 } };

            var result = ExecuteMovement.Execute("u1", new GridPosition(4, 4), range, grid, budget);

            Assert(!result.Success, "Out of range should fail");
            Assert(result.FailureReason.Contains("range"), $"Should mention range: {result.FailureReason}");
        }

        private static void Test_NoBudget_Fails()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));
            var budget = MakeBudget();
            ConsumeMoveBudget.Execute(budget);
            var range = new Dictionary<GridPosition, int> { { new GridPosition(1, 1), 1 } };

            var result = ExecuteMovement.Execute("u1", new GridPosition(1, 1), range, grid, budget);

            Assert(!result.Success, "No budget should fail");
            Assert(result.FailureReason.Contains("movement"), $"Should mention movement: {result.FailureReason}");
        }

        private static void Test_GridMoveFails_ReportsFailure()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));
            grid.PlaceUnit("u2", new GridPosition(1, 1)); // Block destination
            var budget = MakeBudget();
            var range = new Dictionary<GridPosition, int> { { new GridPosition(1, 1), 1 } };

            var result = ExecuteMovement.Execute("u1", new GridPosition(1, 1), range, grid, budget);

            Assert(!result.Success, "Blocked move should fail");
        }

        private static void Test_ConsumesBudgetOnSuccess()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));
            var budget = MakeBudget();
            var range = new Dictionary<GridPosition, int> { { new GridPosition(1, 1), 1 } };

            ExecuteMovement.Execute("u1", new GridPosition(1, 1), range, grid, budget);

            Assert(!budget.CanMove, "Budget should be consumed after successful move");
        }

        private static void Test_UsesInjectedBricks()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("u1", new GridPosition(0, 0));
            var budget = MakeBudget();
            var range = new Dictionary<GridPosition, int> { { new GridPosition(1, 1), 1 } };
            bool validateCalled = false;
            bool checkCalled = false;

            ExecuteMovement.Execute("u1", new GridPosition(1, 1), range, grid, budget,
                validateRange: (dest, r) => { validateCalled = true; return true; },
                checkBudget: b => { checkCalled = true; return true; });

            Assert(validateCalled, "Should call injected validateRange");
            Assert(checkCalled, "Should call injected checkBudget");
        }

        private static ActionBudget MakeBudget()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            return budget;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
