using TokuTactics.Bricks.Shared;
using System;
using TokuTactics.Bricks.Movement;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Movement
{
    public static class CheckActionBudgetTests
    {
        public static void Run()
        {
            Test_CanMove_ReturnsTrue();
            Test_AlreadyMoved_ReturnsFalse();
            Test_NullBudget_ReturnsFalse();
            Console.WriteLine("CheckActionBudgetTests: All passed");
        }

        private static void Test_CanMove_ReturnsTrue()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            Assert(CheckActionBudget.Execute(budget) == true,
                "Fresh budget should allow movement");
        }

        private static void Test_AlreadyMoved_ReturnsFalse()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeMoveBudget.Execute(budget);

            Assert(CheckActionBudget.Execute(budget) == false,
                "Used budget should not allow movement");
        }

        private static void Test_NullBudget_ReturnsFalse()
        {
            Assert(CheckActionBudget.Execute(null) == false,
                "Null budget should return false");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
