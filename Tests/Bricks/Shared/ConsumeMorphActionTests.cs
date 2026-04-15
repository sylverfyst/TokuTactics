using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class ConsumeMorphActionTests
    {
        public static void Run()
        {
            Test_EndsTurn();
            Test_FailsIfNoAction();
            Console.WriteLine("ConsumeMorphActionTests: All passed");
        }

        private static void Test_EndsTurn()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            bool result = ConsumeMorphAction.Execute(budget);

            Assert(result, "Should succeed");
            Assert(!budget.CanMove, "CanMove should be false");
            Assert(!budget.CanAct, "CanAct should be false");
            Assert(!budget.CanFormSwitch, "CanFormSwitch should be false");
        }

        private static void Test_FailsIfNoAction()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            budget.CanAct = false;

            Assert(!ConsumeMorphAction.Execute(budget), "Should fail if cannot act");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
