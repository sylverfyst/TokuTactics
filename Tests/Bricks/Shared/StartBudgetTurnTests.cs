using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class StartBudgetTurnTests
    {
        public static void Run()
        {
            Test_ResetsAllFlags();
            Console.WriteLine("StartBudgetTurnTests: All passed");
        }

        private static void Test_ResetsAllFlags()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            Assert(budget.CanMove, "CanMove should be true");
            Assert(budget.CanAct, "CanAct should be true");
            Assert(budget.CanFormSwitch, "CanFormSwitch should be true");
            Assert(!budget.HasUsedBondRefresh, "HasUsedBondRefresh should be false");
            Assert(!budget.HasReceivedBondRefresh, "HasReceivedBondRefresh should be false");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
