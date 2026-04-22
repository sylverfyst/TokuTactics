using System;
using TokuTactics.Bricks.Movement;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Movement
{
    public static class RestoreMoveBudgetTests
    {
        public static void Run()
        {
            Test_RestoresMovement();
            Test_AlreadyCanMove_ReturnsFalse();
            Test_NullBudget_ReturnsFalse();
            Console.WriteLine("RestoreMoveBudgetTests: All passed");
        }

        private static void Test_RestoresMovement()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeMoveBudget.Execute(budget);

            bool result = RestoreMoveBudget.Execute(budget);

            Assert(result, "Should return true when restoring consumed movement");
            Assert(budget.CanMove, "Movement should be restored");
        }

        private static void Test_AlreadyCanMove_ReturnsFalse()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            bool result = RestoreMoveBudget.Execute(budget);

            Assert(!result, "Should return false when movement already available");
            Assert(budget.CanMove, "Movement should still be true");
        }

        private static void Test_NullBudget_ReturnsFalse()
        {
            bool result = RestoreMoveBudget.Execute(null);
            Assert(!result, "Should return false for null budget");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
