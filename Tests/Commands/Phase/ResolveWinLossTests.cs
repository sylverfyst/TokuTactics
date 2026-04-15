using System;
using System.Collections.Generic;
using TokuTactics.Commands.Phase;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;
using TokuTactics.Core.Phase;
using TokuTactics.Systems.PhaseManagement;

namespace TokuTactics.Tests.Commands.Phase
{
    public static class ResolveWinLossTests
    {
        public static void Run()
        {
            Test_AllAlive_AllTargetsAlive_NoEnd();
            Test_RangerDead_ReturnsDefeat();
            Test_AllTargetsDead_ReturnsVictory();
            Test_LossCheckedBeforeWin();
            Test_UsesInjectedBricks();
            Console.WriteLine("ResolveWinLossTests: All passed");
        }

        private static void Test_AllAlive_AllTargetsAlive_NoEnd()
        {
            var rangers = new List<Ranger> { MakeRanger("r1") };
            var enemies = new List<Enemy> { MakeEnemy("e1") };
            var targets = new HashSet<string> { "e1" };

            var result = ResolveWinLoss.Execute(rangers, enemies, targets);

            Assert(!result.Ended, "Should not end");
        }

        private static void Test_RangerDead_ReturnsDefeat()
        {
            var dead = MakeRanger("r1");
            dead.UnmorphedHealth.TakeDamage(999f);
            var rangers = new List<Ranger> { dead };
            var enemies = new List<Enemy> { MakeEnemy("e1") };
            var targets = new HashSet<string> { "e1" };

            var result = ResolveWinLoss.Execute(rangers, enemies, targets);

            Assert(result.Ended, "Should end");
            Assert(result.EndState == MissionState.Defeat, "Should be defeat");
            Assert(result.FallenRangerId == "r1", $"Should be r1, got {result.FallenRangerId}");
        }

        private static void Test_AllTargetsDead_ReturnsVictory()
        {
            var rangers = new List<Ranger> { MakeRanger("r1") };
            var boss = MakeEnemy("boss1");
            boss.Health.TakeDamage(999f);
            var enemies = new List<Enemy> { boss };
            var targets = new HashSet<string> { "boss1" };

            var result = ResolveWinLoss.Execute(rangers, enemies, targets);

            Assert(result.Ended, "Should end");
            Assert(result.EndState == MissionState.Victory, "Should be victory");
        }

        private static void Test_LossCheckedBeforeWin()
        {
            // Both dead ranger AND all targets dead — loss should win
            var dead = MakeRanger("r1");
            dead.UnmorphedHealth.TakeDamage(999f);
            var rangers = new List<Ranger> { dead };
            var boss = MakeEnemy("boss1");
            boss.Health.TakeDamage(999f);
            var enemies = new List<Enemy> { boss };
            var targets = new HashSet<string> { "boss1" };

            var result = ResolveWinLoss.Execute(rangers, enemies, targets);

            Assert(result.EndState == MissionState.Defeat, "Loss should be checked before win");
        }

        private static void Test_UsesInjectedBricks()
        {
            var rangers = new List<Ranger> { MakeRanger("r1") };
            var enemies = new List<Enemy> { MakeEnemy("e1") };
            var targets = new HashSet<string> { "e1" };
            bool defeatCalled = false;
            bool victoryCalled = false;

            ResolveWinLoss.Execute(rangers, enemies, targets,
                checkRangerDefeat: r => { defeatCalled = true; return null; },
                checkVictoryCondition: (e, t) => { victoryCalled = true; return false; });

            Assert(defeatCalled, "Should call injected defeat brick");
            Assert(victoryCalled, "Should call injected victory brick");
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
