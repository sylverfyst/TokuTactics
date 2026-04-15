using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class ActionBudgetBricksTests
    {
        public static void Run()
        {
            Test_StartBudgetTurn_ResetsAll();
            Test_ConsumeMorphAction_EndsTurn();
            Test_ConsumeMorphAction_FailsIfNoAction();
            Test_ResetBudgetFromFormSwitch_RestoresMoveAndAct();
            Test_ApplyBondRefresh_GrantsActions();
            Test_ApplyBondRefresh_OnlyOnce();
            Test_GiveBondRefresh_OnlyOnce();
            Test_EndBudgetTurn_ClearsAll();
            Console.WriteLine("ActionBudgetBricksTests: All passed");
        }

        private static void Test_StartBudgetTurn_ResetsAll()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            Assert(budget.CanMove, "CanMove should be true");
            Assert(budget.CanAct, "CanAct should be true");
            Assert(budget.CanFormSwitch, "CanFormSwitch should be true");
            Assert(!budget.HasUsedBondRefresh, "HasUsedBondRefresh should be false");
            Assert(!budget.HasReceivedBondRefresh, "HasReceivedBondRefresh should be false");
        }

        private static void Test_ConsumeMorphAction_EndsTurn()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            bool result = ConsumeMorphAction.Execute(budget);

            Assert(result, "Should succeed");
            Assert(!budget.CanMove, "CanMove should be false");
            Assert(!budget.CanAct, "CanAct should be false");
            Assert(!budget.CanFormSwitch, "CanFormSwitch should be false");
        }

        private static void Test_ConsumeMorphAction_FailsIfNoAction()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            budget.CanAct = false;

            Assert(!ConsumeMorphAction.Execute(budget), "Should fail if cannot act");
        }

        private static void Test_ResetBudgetFromFormSwitch_RestoresMoveAndAct()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            budget.CanMove = false;
            budget.CanAct = false;

            ResetBudgetFromFormSwitch.Execute(budget);

            Assert(budget.CanMove, "CanMove should be restored");
            Assert(budget.CanAct, "CanAct should be restored");
        }

        private static void Test_ApplyBondRefresh_GrantsActions()
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

        private static void Test_ApplyBondRefresh_OnlyOnce()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ApplyBondRefresh.Execute(budget);

            Assert(!ApplyBondRefresh.Execute(budget), "Second refresh should fail");
        }

        private static void Test_GiveBondRefresh_OnlyOnce()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            Assert(GiveBondRefresh.Execute(budget), "First give should succeed");
            Assert(!GiveBondRefresh.Execute(budget), "Second give should fail");
        }

        private static void Test_EndBudgetTurn_ClearsAll()
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
