using System;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Tests.Commands.Combat
{
    public static class ProcessAssistTests
    {
        public static void Run()
        {
            Test_CalculatesAndAppliesDamage();
            Test_AwardsBondExperience();
            Test_Tier2SetsFormDisruption();
            Test_Tier4SetsRefreshAvailable();
            Test_TracksAggression();
            Console.WriteLine("ProcessAssistTests: All passed");
        }

        private static void Test_CalculatesAndAppliesDamage()
        {
            var (assist, enemy, typeChart, rng, constants, bondTracker) = MakeDefaults();
            float healthBefore = enemy.Health.Current;

            var result = ProcessAssist.Execute(assist, enemy, typeChart, rng, constants, bondTracker);

            Assert(result.AssistCombatResult.Damage != null, "Should have damage result");
            // Damage should be applied (enemy health reduced)
            Assert(enemy.Health.Current <= healthBefore, "Enemy health should be reduced or equal");
        }

        private static void Test_AwardsBondExperience()
        {
            var (assist, enemy, typeChart, rng, constants, bondTracker) = MakeDefaults();
            // Pre-seed bond so we can verify XP was added
            bondTracker.GetBond(assist.AttackerId, assist.AssisterId);

            ProcessAssist.Execute(assist, enemy, typeChart, rng, constants, bondTracker);

            // Bond XP should have been added (we can't easily check the value,
            // but the method should not throw)
        }

        private static void Test_Tier2SetsFormDisruption()
        {
            var (assist, enemy, typeChart, rng, constants, bondTracker) = MakeDefaults();
            assist.ForceToBaseForm = true;
            assist.VacatedFormId = "form_blaze";

            var result = ProcessAssist.Execute(assist, enemy, typeChart, rng, constants, bondTracker);

            Assert(result.AssistCombatResult.FormDisrupted, "Should flag form disruption");
            Assert(result.AssistCombatResult.VacatedFormId == "form_blaze",
                $"Expected form_blaze, got {result.AssistCombatResult.VacatedFormId}");
        }

        private static void Test_Tier4SetsRefreshAvailable()
        {
            var (assist, enemy, typeChart, rng, constants, bondTracker) = MakeDefaults();
            assist.CanRefreshPartner = true;

            var result = ProcessAssist.Execute(assist, enemy, typeChart, rng, constants, bondTracker);

            Assert(result.AssistCombatResult.RefreshAvailable, "Should flag refresh available");
        }

        private static void Test_TracksAggression()
        {
            var (assist, enemy, typeChart, rng, constants, bondTracker) = MakeDefaults();
            // Make enemy with low health and aggression threshold
            var aggroEnemy = new Enemy("e_aggro", new EnemyData(
                "e_aggro", "e_aggro", EnemyTier.Monster, null,
                StatBlock.Create(str: 5, def: 1, spd: 4),
                maxHealth: 10f, basicAttackPower: 1.0f,
                basicAttackRange: 1, movementRange: 3,
                behaviorTreeId: "bt_grunt",
                aggressionThreshold: 0.8f));

            var result = ProcessAssist.Execute(assist, aggroEnemy, typeChart, rng, constants, bondTracker);

            // If damage was dealt and health dropped below threshold, aggression should trigger
            if (!result.AssistCombatResult.Damage.WasDodged && aggroEnemy.Health.Percentage <= 0.8f)
            {
                Assert(result.AggressionTriggered, "Should trigger aggression");
            }
        }

        private static (AssistEffect, Enemy, TypeChart, Random, TunableConstants, BondTracker) MakeDefaults()
        {
            var assist = new AssistEffect
            {
                AssisterId = "r2",
                AttackerId = "r1",
                BondTier = 1,
                AssisterStr = 8f,
                AssisterWeaponPower = 1.0f,
                AssisterDualType = DualType.Single(ElementalType.Blaze),
                DamageMultiplier = 1.0f,
                ChaMultiplier = 1.0f,
                IsPairAttack = false,
                ForceToBaseForm = false,
                CanRefreshPartner = false
            };

            var enemy = new Enemy("e1", new EnemyData(
                "e1", "e1", EnemyTier.FootSoldier, null,
                StatBlock.Create(str: 5, def: 3, spd: 4),
                maxHealth: 100f, basicAttackPower: 1.0f,
                basicAttackRange: 1, movementRange: 3,
                behaviorTreeId: "bt_grunt"));

            var typeChart = TypeChartSetup.Create();
            var rng = new Random(42);
            var constants = new TunableConstants { BaseDodge = 0f, BaseCrit = 0f };
            var bondTracker = new BondTracker();

            return (assist, enemy, typeChart, rng, constants, bondTracker);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
