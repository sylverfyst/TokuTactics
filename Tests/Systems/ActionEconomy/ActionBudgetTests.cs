using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Systems.ActionEconomy
{
    public class ActionBudgetTests
    {
        // === Start Turn ===

        public void StartTurn_GrantsFullActions()
        {
            var budget = new ActionBudget();

            budget.StartTurn();

            Assert(budget.CanMove, "Should be able to move");
            Assert(budget.CanAct, "Should be able to act");
            Assert(budget.CanFormSwitch, "Should be able to form switch");
            Assert(!budget.IsTurnComplete, "Turn should not be complete");
        }

        // === Basic Actions ===

        public void ConsumeMove_Works()
        {
            var budget = new ActionBudget();
            budget.StartTurn();

            bool result = budget.ConsumeMove();

            Assert(result, "Should succeed");
            Assert(!budget.CanMove, "Should not be able to move again");
            Assert(budget.CanAct, "Should still be able to act");
        }

        public void ConsumeMove_Twice_Fails()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            budget.ConsumeMove();

            bool result = budget.ConsumeMove();

            Assert(!result, "Second move should fail");
        }

        public void ConsumeAction_Works()
        {
            var budget = new ActionBudget();
            budget.StartTurn();

            bool result = budget.ConsumeAction();

            Assert(result, "Should succeed");
            Assert(!budget.CanAct, "Should not be able to act again");
            Assert(budget.CanMove, "Should still be able to move");
        }

        public void ConsumeAction_Twice_Fails()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            budget.ConsumeAction();

            bool result = budget.ConsumeAction();

            Assert(!result, "Second action should fail");
        }

        // === Morph Action ===

        public void ConsumeMorphAction_EndsTurn()
        {
            var budget = new ActionBudget();
            budget.StartTurn();

            bool result = budget.ConsumeMorphAction();

            Assert(result, "Should succeed");
            Assert(!budget.CanMove, "Should not be able to move");
            Assert(!budget.CanAct, "Should not be able to act");
            Assert(!budget.CanFormSwitch, "Should not be able to switch");
            Assert(budget.IsTurnComplete, "Turn should be complete");
        }

        public void ConsumeMorphAction_FailsIfAlreadyActed()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            budget.ConsumeAction();

            bool result = budget.ConsumeMorphAction();

            Assert(!result, "Should fail if action already consumed");
        }

        // === Form Switch Reset ===

        public void ResetFromFormSwitch_RestoresMovementAndAction()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            budget.ConsumeMove();
            budget.ConsumeAction();

            budget.ResetFromFormSwitch();

            Assert(budget.CanMove, "Should be able to move again");
            Assert(budget.CanAct, "Should be able to act again");
        }

        public void ResetFromFormSwitch_ChainableMultipleTimes()
        {
            var budget = new ActionBudget();
            budget.StartTurn();

            // Chain: act, switch, act, switch, act
            budget.ConsumeAction();
            budget.ResetFromFormSwitch();
            Assert(budget.CanAct, "Should be able to act after first switch");

            budget.ConsumeAction();
            budget.ResetFromFormSwitch();
            Assert(budget.CanAct, "Should be able to act after second switch");

            budget.ConsumeAction();
            Assert(!budget.CanAct, "Should not be able to act without another switch");
        }

        // === Bond Refresh ===

        public void ApplyBondRefresh_GrantsAction()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            budget.ConsumeMove();
            budget.ConsumeAction();

            bool result = budget.ApplyBondRefresh();

            Assert(result, "Should succeed");
            Assert(budget.CanMove, "Should restore move");
            Assert(budget.CanAct, "Should restore action");
            Assert(budget.HasReceivedBondRefresh, "Should be flagged");
        }

        public void ApplyBondRefresh_OnlyOncePerRound()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            budget.ConsumeAction();
            budget.ApplyBondRefresh();
            budget.ConsumeAction();

            bool result = budget.ApplyBondRefresh();

            Assert(!result, "Second refresh should fail");
        }

        public void GiveBondRefresh_OnlyOncePerRound()
        {
            var budget = new ActionBudget();
            budget.StartTurn();

            bool first = budget.GiveBondRefresh();
            bool second = budget.GiveBondRefresh();

            Assert(first, "First give should succeed");
            Assert(!second, "Second give should fail");
        }

        public void BondRefresh_ResetsOnNewTurn()
        {
            var budget = new ActionBudget();
            budget.StartTurn();
            budget.ApplyBondRefresh();
            budget.GiveBondRefresh();

            budget.StartTurn(); // New turn

            Assert(!budget.HasReceivedBondRefresh, "Received flag should reset");
            Assert(!budget.HasUsedBondRefresh, "Used flag should reset");
        }

        // === End Turn ===

        public void EndTurn_PreventsAllActions()
        {
            var budget = new ActionBudget();
            budget.StartTurn();

            budget.EndTurn();

            Assert(!budget.CanMove, "Should not move");
            Assert(!budget.CanAct, "Should not act");
            Assert(!budget.CanFormSwitch, "Should not switch");
            Assert(budget.IsTurnComplete, "Should be complete");
        }

        // === Full Chain Scenario ===

        public void FullChainScenario_MoveAttackSwitchMoveAttackRefreshAct()
        {
            var budget = new ActionBudget();
            budget.StartTurn();

            // Move and attack
            Assert(budget.ConsumeMove(), "Move 1");
            Assert(budget.ConsumeAction(), "Act 1");

            // Form switch resets
            budget.ResetFromFormSwitch();
            Assert(budget.ConsumeMove(), "Move 2");
            Assert(budget.ConsumeAction(), "Act 2");

            // Another switch
            budget.ResetFromFormSwitch();
            Assert(budget.ConsumeMove(), "Move 3");
            Assert(budget.ConsumeAction(), "Act 3");

            // No more switches, but bond refresh
            Assert(budget.ApplyBondRefresh(), "Bond refresh");
            Assert(budget.ConsumeAction(), "Act 4 (from refresh)");

            // Now truly done
            Assert(!budget.CanAct, "Should be out of actions");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new ActionBudgetTests();
            tests.StartTurn_GrantsFullActions();
            tests.ConsumeMove_Works();
            tests.ConsumeMove_Twice_Fails();
            tests.ConsumeAction_Works();
            tests.ConsumeAction_Twice_Fails();
            tests.ConsumeMorphAction_EndsTurn();
            tests.ConsumeMorphAction_FailsIfAlreadyActed();
            tests.ResetFromFormSwitch_RestoresMovementAndAction();
            tests.ResetFromFormSwitch_ChainableMultipleTimes();
            tests.ApplyBondRefresh_GrantsAction();
            tests.ApplyBondRefresh_OnlyOncePerRound();
            tests.GiveBondRefresh_OnlyOncePerRound();
            tests.BondRefresh_ResetsOnNewTurn();
            tests.EndTurn_PreventsAllActions();
            tests.FullChainScenario_MoveAttackSwitchMoveAttackRefreshAct();
            System.Console.WriteLine("ActionBudgetTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
