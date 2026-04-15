using TokuTactics.Bricks.Shared;
using System;
using TokuTactics.Bricks.Movement;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Movement
{
    public static class ConsumeMoveBudgetTests
    {
        public static void Run()
        {
            Test_ConsumesMovement();
            Test_NullBudget_DoesNotThrow();
            Console.WriteLine("ConsumeMoveBudgetTests: All passed");
        }

        private static void Test_ConsumesMovement()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            ConsumeMoveBudget.Execute(budget);

            Assert(budget.CanMove == false, "Movement should be consumed");
        }

        private static void Test_NullBudget_DoesNotThrow()
        {
            // Should not throw
            ConsumeMoveBudget.Execute(null);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
