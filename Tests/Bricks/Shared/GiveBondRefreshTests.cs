using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class GiveBondRefreshTests
    {
        public static void Run()
        {
            Test_OnlyOnce();
            Console.WriteLine("GiveBondRefreshTests: All passed");
        }

        private static void Test_OnlyOnce()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            Assert(GiveBondRefresh.Execute(budget), "First give should succeed");
            Assert(!GiveBondRefresh.Execute(budget), "Second give should fail");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
