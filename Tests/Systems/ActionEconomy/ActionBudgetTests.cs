using TokuTactics.Bricks.Combat;
using TokuTactics.Bricks.Movement;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Systems.ActionEconomy
{
    /// <summary>
    /// Integration tests: verify bricks operate correctly on the ActionBudget type.
    /// </summary>
    public class ActionBudgetTests
    {
        // === Start Turn ===

        public void StartTurn_GrantsFullActions()
        {
            var budget = new ActionBudget();

            StartBudgetTurn.Execute(budget);

            Assert(budget.CanMove, "Should be able to move");
            Assert(budget.CanAct, "Should be able to act");
            Assert(budget.CanFormSwitch, "Should be able to form switch");
            Assert(!budget.IsTurnComplete, "Turn should not be complete");
        }

        // === Basic Actions ===

        public void ConsumeMove_Works()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            bool result = ConsumeMoveBudget.Execute(budget);

            Assert(result, "Should succeed");
            Assert(!budget.CanMove, "Should not be able to move again");
            Assert(budget.CanAct, "Should still be able to act");
        }

        public void ConsumeMove_Twice_Fails()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeMoveBudget.Execute(budget);

            bool result = ConsumeMoveBudget.Execute(budget);

            Assert(!result, "Second move should fail");
        }

        public void ConsumeAction_Works()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            bool result = ConsumeActionBudget.Execute(budget);

            Assert(result, "Should succeed");
            Assert(!budget.CanAct, "Should not be able to act again");
            Assert(budget.CanMove, "Should still be able to move");
        }

        public void ConsumeAction_Twice_Fails()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeActionBudget.Execute(budget);

            bool result = ConsumeActionBudget.Execute(budget);

            Assert(!result, "Second action should fail");
        }

        // === Morph Action ===

        public void ConsumeMorphAction_EndsTurn()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            bool result = ConsumeMorphAction.Execute(budget);

            Assert(result, "Should succeed");
            Assert(!budget.CanMove, "Should not be able to move");
            Assert(!budget.CanAct, "Should not be able to act");
            Assert(!budget.CanFormSwitch, "Should not be able to switch");
            Assert(budget.IsTurnComplete, "Turn should be complete");
        }

        public void ConsumeMorphAction_FailsIfAlreadyActed()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeActionBudget.Execute(budget);

            bool result = ConsumeMorphAction.Execute(budget);

            Assert(!result, "Should fail if action already consumed");
        }

        // === Form Switch Reset ===

        public void ResetFromFormSwitch_RestoresMovementAndAction()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeMoveBudget.Execute(budget);
            ConsumeActionBudget.Execute(budget);

            ResetBudgetFromFormSwitch.Execute(budget);

            Assert(budget.CanMove, "Should be able to move again");
            Assert(budget.CanAct, "Should be able to act again");
        }

        public void ResetFromFormSwitch_ChainableMultipleTimes()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            // Chain: act, switch, act, switch, act
            ConsumeActionBudget.Execute(budget);
            ResetBudgetFromFormSwitch.Execute(budget);
            Assert(budget.CanAct, "Should be able to act after first switch");

            ConsumeActionBudget.Execute(budget);
            ResetBudgetFromFormSwitch.Execute(budget);
            Assert(budget.CanAct, "Should be able to act after second switch");

            ConsumeActionBudget.Execute(budget);
            Assert(!budget.CanAct, "Should not be able to act without another switch");
        }

        // === Bond Refresh ===

        public void ApplyBondRefresh_GrantsAction()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeMoveBudget.Execute(budget);
            ConsumeActionBudget.Execute(budget);

            bool result = TokuTactics.Bricks.Shared.ApplyBondRefresh.Execute(budget);

            Assert(result, "Should succeed");
            Assert(budget.CanMove, "Should restore move");
            Assert(budget.CanAct, "Should restore action");
            Assert(budget.HasReceivedBondRefresh, "Should be flagged");
        }

        public void ApplyBondRefresh_OnlyOncePerRound()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            ConsumeActionBudget.Execute(budget);
            TokuTactics.Bricks.Shared.ApplyBondRefresh.Execute(budget);
            ConsumeActionBudget.Execute(budget);

            bool result = TokuTactics.Bricks.Shared.ApplyBondRefresh.Execute(budget);

            Assert(!result, "Second refresh should fail");
        }

        public void GiveBondRefresh_OnlyOncePerRound()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            bool first = GiveBondRefresh.Execute(budget);
            bool second = GiveBondRefresh.Execute(budget);

            Assert(first, "First give should succeed");
            Assert(!second, "Second give should fail");
        }

        public void BondRefresh_ResetsOnNewTurn()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);
            TokuTactics.Bricks.Shared.ApplyBondRefresh.Execute(budget);
            GiveBondRefresh.Execute(budget);

            StartBudgetTurn.Execute(budget); // New turn

            Assert(!budget.HasReceivedBondRefresh, "Received flag should reset");
            Assert(!budget.HasUsedBondRefresh, "Used flag should reset");
        }

        // === End Turn ===

        public void EndTurn_PreventsAllActions()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            EndBudgetTurn.Execute(budget);

            Assert(!budget.CanMove, "Should not move");
            Assert(!budget.CanAct, "Should not act");
            Assert(!budget.CanFormSwitch, "Should not switch");
            Assert(budget.IsTurnComplete, "Should be complete");
        }

        // === Full Chain Scenario ===

        public void FullChainScenario_MoveAttackSwitchMoveAttackRefreshAct()
        {
            var budget = new ActionBudget();
            StartBudgetTurn.Execute(budget);

            // Move and attack
            Assert(ConsumeMoveBudget.Execute(budget), "Move 1");
            Assert(ConsumeActionBudget.Execute(budget), "Act 1");

            // Form switch resets
            ResetBudgetFromFormSwitch.Execute(budget);
            Assert(ConsumeMoveBudget.Execute(budget), "Move 2");
            Assert(ConsumeActionBudget.Execute(budget), "Act 2");

            // Another switch
            ResetBudgetFromFormSwitch.Execute(budget);
            Assert(ConsumeMoveBudget.Execute(budget), "Move 3");
            Assert(ConsumeActionBudget.Execute(budget), "Act 3");

            // No more switches, but bond refresh
            Assert(TokuTactics.Bricks.Shared.ApplyBondRefresh.Execute(budget), "Bond refresh");
            Assert(ConsumeActionBudget.Execute(budget), "Act 4 (from refresh)");

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
