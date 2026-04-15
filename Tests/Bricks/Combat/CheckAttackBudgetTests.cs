using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class CheckAttackBudgetTests
    {
        public static void Run()
        {
            Test_CanAct_ReturnsTrue();
            Test_AlreadyActed_ReturnsFalse();
            Test_NullBudget_ReturnsFalse();
            Console.WriteLine("CheckAttackBudgetTests: All passed");
        }

        private static void Test_CanAct_ReturnsTrue()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            Assert(CheckAttackBudget.Execute(budget) == true, "Fresh budget should allow action");
        }

        private static void Test_AlreadyActed_ReturnsFalse()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeActionBudget.Execute(budget);
            Assert(CheckAttackBudget.Execute(budget) == false, "Used budget should not allow action");
        }

        private static void Test_NullBudget_ReturnsFalse()
        {
            Assert(CheckAttackBudget.Execute(null) == false, "Null budget should return false");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
