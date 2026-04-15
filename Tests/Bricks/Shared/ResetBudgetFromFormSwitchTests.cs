using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class ResetBudgetFromFormSwitchTests
    {
        public static void Run()
        {
            Test_RestoresMoveAndAct();
            Console.WriteLine("ResetBudgetFromFormSwitchTests: All passed");
        }

        private static void Test_RestoresMoveAndAct()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            budget.CanMove = false;
            budget.CanAct = false;

            ResetBudgetFromFormSwitch.Execute(budget);

            Assert(budget.CanMove, "CanMove should be restored");
            Assert(budget.CanAct, "CanAct should be restored");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
