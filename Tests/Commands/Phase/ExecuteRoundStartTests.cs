using System;
using System.Collections.Generic;
using TokuTactics.Commands.Phase;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.FormManagement;
using TokuTactics.Systems.PhaseManagement;

namespace TokuTactics.Tests.Commands.Phase
{
    public static class ExecuteRoundStartTests
    {
        public static void Run()
        {
            Test_IncrementsRoundNumber();
            Test_NoDeaths_MissionContinues();
            Test_RangerDeath_MissionEnds();
            Test_VictoryCondition_MissionEnds();
            Test_ResetsComboChains();
            Test_UsesInjectedCommands();
            Console.WriteLine("ExecuteRoundStartTests: All passed");
        }

        private static void Test_IncrementsRoundNumber()
        {
            var (rangers, enemies, targets, pool) = MakeDefaults();

            var result = ExecuteRoundStart.Execute(1, rangers, enemies, targets, pool);

            Assert(result.NewRoundNumber == 2, $"Expected round 2, got {result.NewRoundNumber}");
        }

        private static void Test_NoDeaths_MissionContinues()
        {
            var (rangers, enemies, targets, pool) = MakeDefaults();

            var result = ExecuteRoundStart.Execute(1, rangers, enemies, targets, pool);

            Assert(!result.MissionEnded, "Mission should continue");
        }

        private static void Test_RangerDeath_MissionEnds()
        {
            var (rangers, enemies, targets, pool) = MakeDefaults();
            rangers[0].UnmorphedHealth.TakeDamage(999f);

            var result = ExecuteRoundStart.Execute(1, rangers, enemies, targets, pool);

            Assert(result.MissionEnded, "Mission should end");
            Assert(result.EndState == MissionState.Defeat, "Should be defeat");
            Assert(result.FallenRangerId == "r1", $"Fallen ranger should be r1, got {result.FallenRangerId}");
        }

        private static void Test_VictoryCondition_MissionEnds()
        {
            var (rangers, enemies, targets, pool) = MakeDefaults();
            enemies[0].Health.TakeDamage(999f);

            var result = ExecuteRoundStart.Execute(1, rangers, enemies, targets, pool);

            Assert(result.MissionEnded, "Mission should end");
            Assert(result.EndState == MissionState.Victory, "Should be victory");
        }

        private static void Test_ResetsComboChains()
        {
            var (rangers, enemies, targets, pool) = MakeDefaults();
            rangers[0].ComboScaler.AdvanceChain();
            int comboBefore = rangers[0].ComboScaler.ChainCount;

            ExecuteRoundStart.Execute(1, rangers, enemies, targets, pool);

            Assert(rangers[0].ComboScaler.ChainCount == 0,
                $"Combo should be reset to 0, was {comboBefore} before, now {rangers[0].ComboScaler.ChainCount}");
        }

        private static void Test_UsesInjectedCommands()
        {
            var (rangers, enemies, targets, pool) = MakeDefaults();
            bool statusEffectsCalled = false;
            bool winLossCalled = false;

            ExecuteRoundStart.Execute(1, rangers, enemies, targets, pool,
                processStatusEffects: (r, e) =>
                {
                    statusEffectsCalled = true;
                    return new StatusEffectRoundResult();
                },
                resolveWinLoss: (r, e, t) =>
                {
                    winLossCalled = true;
                    return WinLossResult.NoEnd();
                });

            Assert(statusEffectsCalled, "Should call injected processStatusEffects");
            Assert(winLossCalled, "Should call injected resolveWinLoss");
        }

        private static (List<Ranger> rangers, List<Enemy> enemies, HashSet<string> targets, FormPool pool) MakeDefaults()
        {
            var rangers = new List<Ranger> { MakeRanger("r1") };
            var enemies = new List<Enemy> { MakeEnemy("e1") };
            var targets = new HashSet<string> { "e1" };
            var pool = new FormPool("form_base", 3);
            return (rangers, enemies, targets, pool);
        }

        private static Ranger MakeRanger(string id)
        {
            return new Ranger(id, id, ElementalType.Blaze,
                new Proclivity(StatType.STR), null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4),
                50f, FormCatalog.BaseForm());
        }

        private static Enemy MakeEnemy(string id)
        {
            return new Enemy(id, new EnemyData(
                id, id, EnemyTier.FootSoldier, null,
                StatBlock.Create(str: 5, def: 3, spd: 4),
                maxHealth: 25f, basicAttackPower: 1.0f,
                basicAttackRange: 1, movementRange: 3,
                behaviorTreeId: "bt_grunt"));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
