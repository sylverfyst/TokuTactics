using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class EndBudgetTurnTests
    {
        public static void Run()
        {
            Test_ClearsAll();
            Console.WriteLine("EndBudgetTurnTests: All passed");
        }

        private static void Test_ClearsAll()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            EndBudgetTurn.Execute(budget);

            Assert(!budget.CanMove, "CanMove should be false");
            Assert(!budget.CanAct, "CanAct should be false");
            Assert(!budget.CanFormSwitch, "CanFormSwitch should be false");
            Assert(budget.IsTurnComplete, "Should be complete");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
