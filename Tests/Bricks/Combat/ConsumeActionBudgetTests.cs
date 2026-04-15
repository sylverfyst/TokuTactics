using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class ConsumeActionBudgetTests
    {
        public static void Run()
        {
            Test_ConsumesAction();
            Test_NullBudget_DoesNotThrow();
            Console.WriteLine("ConsumeActionBudgetTests: All passed");
        }

        private static void Test_ConsumesAction()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            ConsumeActionBudget.Execute(budget);
            Assert(budget.CanAct == false, "Action should be consumed");
        }

        private static void Test_NullBudget_DoesNotThrow()
        {
            ConsumeActionBudget.Execute(null);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
