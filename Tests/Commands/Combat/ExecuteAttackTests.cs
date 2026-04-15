using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Bricks.Shared;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.Grid;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.CombatResolution;

namespace TokuTactics.Tests.Commands.Combat
{
    public static class ExecuteAttackTests
    {
        public static void Run()
        {
            Test_ValidAttack_Succeeds();
            Test_OutOfRange_Fails();
            Test_NoBudget_Fails();
            Test_NullCombatResult_Fails();
            Test_ConsumesBudgetOnSuccess();
            Test_UsesInjectedBricks();
            Console.WriteLine("ExecuteAttackTests: All passed");
        }

        private static void Test_ValidAttack_Succeeds()
        {
            var budget = MakeBudget();
            var combatResult = new CombatResult { AttackerId = "r1", TargetId = "e1" };

            var result = ExecuteAttack.Execute(
                new GridPosition(5, 5), new GridPosition(5, 6), 3, budget,
                resolveCombat: () => combatResult);

            Assert(result.Success, "Valid attack should succeed");
            Assert(result.CombatResult == combatResult, "Should return combat result");
        }

        private static void Test_OutOfRange_Fails()
        {
            var budget = MakeBudget();

            var result = ExecuteAttack.Execute(
                new GridPosition(0, 0), new GridPosition(9, 9), 3, budget,
                resolveCombat: () => new CombatResult());

            Assert(!result.Success, "Out of range should fail");
            Assert(result.FailureReason.Contains("range"), $"Should mention range: {result.FailureReason}");
        }

        private static void Test_NoBudget_Fails()
        {
            var budget = MakeBudget();
            ConsumeActionBudget.Execute(budget);

            var result = ExecuteAttack.Execute(
                new GridPosition(5, 5), new GridPosition(5, 6), 3, budget,
                resolveCombat: () => new CombatResult());

            Assert(!result.Success, "No budget should fail");
            Assert(result.FailureReason.Contains("action"), $"Should mention action: {result.FailureReason}");
        }

        private static void Test_NullCombatResult_Fails()
        {
            var budget = MakeBudget();

            var result = ExecuteAttack.Execute(
                new GridPosition(5, 5), new GridPosition(5, 6), 3, budget,
                resolveCombat: () => null);

            Assert(!result.Success, "Null combat result should fail");
        }

        private static void Test_ConsumesBudgetOnSuccess()
        {
            var budget = MakeBudget();

            ExecuteAttack.Execute(
                new GridPosition(5, 5), new GridPosition(5, 6), 3, budget,
                resolveCombat: () => new CombatResult { AttackerId = "r1", TargetId = "e1" });

            Assert(!budget.CanAct, "Budget should be consumed after successful attack");
        }

        private static void Test_UsesInjectedBricks()
        {
            var budget = MakeBudget();
            bool rangeCalled = false;
            bool budgetCalled = false;

            ExecuteAttack.Execute(
                new GridPosition(5, 5), new GridPosition(5, 6), 3, budget,
                resolveCombat: () => new CombatResult { AttackerId = "r1", TargetId = "e1" },
                validateRange: (a, t, r) => { rangeCalled = true; return true; },
                checkBudget: b => { budgetCalled = true; return true; });

            Assert(rangeCalled, "Should call injected validateRange");
            Assert(budgetCalled, "Should call injected checkBudget");
        }

        private static ActionBudget MakeBudget()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            return budget;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
