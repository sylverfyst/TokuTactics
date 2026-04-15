using System;

namespace TokuTactics.Tests
{
    /// <summary>
    /// Master test runner. Execute this to run all test suites.
    /// 
    /// In Rider, you can also run these with xUnit/NUnit by adding the test framework
    /// dependency and adding [Fact]/[Test] attributes. The current setup runs
    /// without any external dependencies.
    /// 
    /// Usage: instantiate and call RunAll(), or call individual suite RunAll() methods.
    /// </summary>
    public static class TestRunner
    {
        public static void RunAll()
        {
            Console.WriteLine("=== Toku Tactics — Running All Tests ===\n");

            int passed = 0;
            int failed = 0;

            // Core layer
            RunSuite("StatBlock", Core.Stats.StatBlockTests.RunAll, ref passed, ref failed);
            RunSuite("TypeSystem", Core.Types.TypeSystemTests.RunAll, ref passed, ref failed);
            RunSuite("HealthPool", Core.Health.HealthPoolTests.RunAll, ref passed, ref failed);
            RunSuite("CooldownTimer", Core.Cooldown.CooldownTimerTests.RunAll, ref passed, ref failed);
            RunSuite("StatusEffect", Core.StatusEffect.StatusEffectTests.RunAll, ref passed, ref failed);
            RunSuite("ComboScaler", Core.Combat.ComboScalerTests.RunAll, ref passed, ref failed);
            RunSuite("DamageCalculator", Core.Combat.DamageCalculatorTests.RunAll, ref passed, ref failed);
            RunSuite("EventBus", Core.Events.EventBusTests.RunAll, ref passed, ref failed);
            RunSuite("Grid", Core.Grid.GridTests.RunAll, ref passed, ref failed);

            // Entity layer
            RunSuite("FormInstance", Entities.Forms.FormInstanceTests.RunAll, ref passed, ref failed);
            RunSuite("Proclivity", Entities.Rangers.ProclivityTests.RunAll, ref passed, ref failed);
            RunSuite("Ranger", Entities.Rangers.RangerTests.RunAll, ref passed, ref failed);
            RunSuite("Battleizer", Entities.Rangers.BattleizerTests.RunAll, ref passed, ref failed);
            RunSuite("Megazord", Entities.Zords.MegazordTests.RunAll, ref passed, ref failed);
            RunSuite("Enemy", Entities.Enemies.EnemyTests.RunAll, ref passed, ref failed);

            // Systems layer
            RunSuite("FormPool", Systems.FormManagement.FormPoolTests.RunAll, ref passed, ref failed);
            RunSuite("ActionBudget", Systems.ActionEconomy.ActionBudgetTests.RunAll, ref passed, ref failed);
            RunSuite("BondTracker", Systems.ActionEconomy.BondTrackerTests.RunAll, ref passed, ref failed);
            RunSuite("TurnOrder", Systems.ActionEconomy.TurnOrderTests.RunAll, ref passed, ref failed);
            RunSuite("GimmickResolver", Systems.GimmickResolution.GimmickResolverTests.RunAll, ref passed, ref failed);
            RunSuite("AssistResolver", Systems.AssistResolution.AssistResolverTests.RunAll, ref passed, ref failed);
            RunSuite("CombatResolver", Systems.CombatResolution.CombatResolverTests.RunAll, ref passed, ref failed);
            RunSuite("PhaseManager", Systems.PhaseManagement.PhaseManagerTests.RunAll, ref passed, ref failed);
            RunSuite("LoadoutController", Systems.LoadoutSelection.LoadoutControllerTests.RunAll, ref passed, ref failed);
            RunSuite("SaveManager", Systems.SaveLoad.SaveManagerTests.RunAll, ref passed, ref failed);
            RunSuite("MissionContext", Systems.MissionSetup.MissionContextTests.RunAll, ref passed, ref failed);

            // Content
            RunSuite("Content", Data.ContentTests.RunAll, ref passed, ref failed);

            // BCO Bricks - Bond
            RunSuite("CalculateScaledBondExp", Bricks.Bond.CalculateScaledBondExpTests.Run, ref passed, ref failed);
            RunSuite("ResolveBondTier", Bricks.Bond.ResolveBondTierTests.Run, ref passed, ref failed);

            // BCO Bricks - Loadout
            RunSuite("ValidateMorphRequest", Bricks.Loadout.ValidateMorphRequestTests.Run, ref passed, ref failed);
            RunSuite("ValidateLoadoutSubmission", Bricks.Loadout.ValidateLoadoutSubmissionTests.Run, ref passed, ref failed);

            // BCO Bricks - Form
            RunSuite("CheckFormAvailability", Bricks.Form.CheckFormAvailabilityTests.Run, ref passed, ref failed);
            RunSuite("ValidateFormEquip", Bricks.Form.ValidateFormEquipTests.Run, ref passed, ref failed);
            RunSuite("TickCooldownWithRegen", Bricks.Form.TickCooldownWithRegenTests.Run, ref passed, ref failed);

            // BCO Bricks - Spatial
            RunSuite("FindUnitsInRange", Bricks.Spatial.FindUnitsInRangeTests.Run, ref passed, ref failed);
            RunSuite("CalculateDisplacement", Bricks.Spatial.CalculateDisplacementTests.Run, ref passed, ref failed);
            RunSuite("FindPassableSpawnPositions", Bricks.Spatial.FindPassableSpawnPositionsTests.Run, ref passed, ref failed);

            // BCO Bricks - Shared
            RunSuite("ValidateMissionActive", Bricks.Shared.ValidateMissionActiveTests.Run, ref passed, ref failed);
            RunSuite("ApplyEffectOutputToHealth", Bricks.Shared.ApplyEffectOutputToHealthTests.Run, ref passed, ref failed);
            RunSuite("GetTargetHealthPool", Bricks.Shared.GetTargetHealthPoolTests.Run, ref passed, ref failed);
            RunSuite("CheckFormDeath", Bricks.Shared.CheckFormDeathTests.Run, ref passed, ref failed);

            // BCO Bricks - Assist
            RunSuite("CheckAssistEligibility", Bricks.Assist.CheckAssistEligibilityTests.Run, ref passed, ref failed);
            RunSuite("CalculateAssistDamageMultiplier", Bricks.Assist.CalculateAssistDamageMultiplierTests.Run, ref passed, ref failed);
            RunSuite("ResolveTier2Disruption", Bricks.Assist.ResolveTier2DisruptionTests.Run, ref passed, ref failed);
            RunSuite("CheckTier4RefreshEligibility", Bricks.Assist.CheckTier4RefreshEligibilityTests.Run, ref passed, ref failed);

            // BCO Bricks - Movement
            RunSuite("ValidateMovementRange", Bricks.Movement.ValidateMovementRangeTests.Run, ref passed, ref failed);
            RunSuite("CheckActionBudget", Bricks.Movement.CheckActionBudgetTests.Run, ref passed, ref failed);
            RunSuite("ExecuteGridMove", Bricks.Movement.ExecuteGridMoveTests.Run, ref passed, ref failed);
            RunSuite("ConsumeMoveBudget", Bricks.Movement.ConsumeMoveBudgetTests.Run, ref passed, ref failed);

            // BCO Bricks - Phase
            RunSuite("CheckRangerDefeat", Bricks.Phase.CheckRangerDefeatTests.Run, ref passed, ref failed);
            RunSuite("CheckVictoryCondition", Bricks.Phase.CheckVictoryConditionTests.Run, ref passed, ref failed);
            RunSuite("BuildTurnOrder", Bricks.Phase.BuildTurnOrderTests.Run, ref passed, ref failed);

            // BCO Bricks - Combat
            RunSuite("CalculateBaseDamage", Bricks.Combat.CalculateBaseDamageTests.Run, ref passed, ref failed);
            RunSuite("RollDodge", Bricks.Combat.RollDodgeTests.Run, ref passed, ref failed);
            RunSuite("ApplyTypeMatchup", Bricks.Combat.ApplyTypeMatchupTests.Run, ref passed, ref failed);
            RunSuite("CalculateSameTypeBonus", Bricks.Combat.CalculateSameTypeBonusTests.Run, ref passed, ref failed);
            RunSuite("RollCrit", Bricks.Combat.RollCritTests.Run, ref passed, ref failed);
            RunSuite("ApplyComboScaling", Bricks.Combat.ApplyComboScalingTests.Run, ref passed, ref failed);
            RunSuite("ApplyDamageToEnemy", Bricks.Combat.ApplyDamageToEnemyTests.Run, ref passed, ref failed);
            RunSuite("ApplyDamageToRanger", Bricks.Combat.ApplyDamageToRangerTests.Run, ref passed, ref failed);
            RunSuite("ApplyStatusEffect", Bricks.Combat.ApplyStatusEffectTests.Run, ref passed, ref failed);
            RunSuite("CalculateStatusPotency", Bricks.Combat.CalculateStatusPotencyTests.Run, ref passed, ref failed);
            RunSuite("ValidateReactiveGimmick", Bricks.Combat.ValidateReactiveGimmickTests.Run, ref passed, ref failed);
            RunSuite("CheckAttackBudget", Bricks.Combat.CheckAttackBudgetTests.Run, ref passed, ref failed);
            RunSuite("ConsumeActionBudget", Bricks.Combat.ConsumeActionBudgetTests.Run, ref passed, ref failed);
            RunSuite("ValidateAttackRange", Bricks.Combat.ValidateAttackRangeTests.Run, ref passed, ref failed);

            // BCO Commands - Combat
            RunSuite("ResolveDamageRoll", Commands.Combat.ResolveDamageRollTests.Run, ref passed, ref failed);
            RunSuite("ApplyWeaponStatus", Commands.Combat.ApplyWeaponStatusTests.Run, ref passed, ref failed);
            RunSuite("ResolveTargetDeath", Commands.Combat.ResolveTargetDeathTests.Run, ref passed, ref failed);
            RunSuite("ResolveReactiveGimmick", Commands.Combat.ResolveReactiveGimmickTests.Run, ref passed, ref failed);
            RunSuite("ProcessAssist", Commands.Combat.ProcessAssistTests.Run, ref passed, ref failed);
            RunSuite("ExecuteAttack", Commands.Combat.ExecuteAttackTests.Run, ref passed, ref failed);

            // BCO Commands - Loadout
            RunSuite("ExecuteLoadoutSubmission", Commands.Loadout.ExecuteLoadoutSubmissionTests.Run, ref passed, ref failed);

            // BCO Commands - Form
            RunSuite("ProcessFormPoolTurn", Commands.Form.ProcessFormPoolTurnTests.Run, ref passed, ref failed);

            // BCO Commands - Gimmick
            RunSuite("ResolveGimmickEffects", Commands.Gimmick.ResolveGimmickEffectsTests.Run, ref passed, ref failed);

            // BCO Commands - Assist
            RunSuite("ResolveAssistEffect", Commands.Assist.ResolveAssistEffectTests.Run, ref passed, ref failed);

            // BCO Commands - Movement
            RunSuite("ExecuteMovement", Commands.Movement.ExecuteMovementTests.Run, ref passed, ref failed);

            // BCO Commands - Phase
            RunSuite("ResolveWinLoss", Commands.Phase.ResolveWinLossTests.Run, ref passed, ref failed);
            RunSuite("ProcessRoundStatusEffects", Commands.Phase.ProcessRoundStatusEffectsTests.Run, ref passed, ref failed);
            RunSuite("ExecuteRoundStart", Commands.Phase.ExecuteRoundStartTests.Run, ref passed, ref failed);
            RunSuite("InitializeMission", Commands.Phase.InitializeMissionTests.Run, ref passed, ref failed);
            RunSuite("ExecutePhaseTransition", Commands.Phase.ExecutePhaseTransitionTests.Run, ref passed, ref failed);

            Console.WriteLine($"\n=== Results: {passed} suites passed, {failed} suites failed ===");

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("SOME TESTS FAILED");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ALL TESTS PASSED");
                Console.ResetColor();
            }
        }

        private static void RunSuite(string name, Action suiteRunner, ref int passed, ref int failed)
        {
            try
            {
                suiteRunner();
                passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  FAILED: {name} — {ex.Message}");
                Console.ResetColor();
                failed++;
            }
        }
    }
}
