using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class ApplyBondRefreshTests
    {
        public static void Run()
        {
            Test_GrantsActions();
            Test_OnlyOnce();
            Console.WriteLine("ApplyBondRefreshTests: All passed");
        }

        private static void Test_GrantsActions()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            budget.CanMove = false;
            budget.CanAct = false;

            bool result = ApplyBondRefresh.Execute(budget);

            Assert(result, "Should succeed");
            Assert(budget.CanMove, "Should restore move");
            Assert(budget.CanAct, "Should restore act");
            Assert(budget.HasReceivedBondRefresh, "Should be flagged");
        }

        private static void Test_OnlyOnce()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ApplyBondRefresh.Execute(budget);

            Assert(!ApplyBondRefresh.Execute(budget), "Second refresh should fail");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
