using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Phase;
using TokuTactics.Core.Stats;
using TokuTactics.Entities.Enemies;

namespace TokuTactics.Tests.Bricks.Phase
{
    public static class CheckVictoryConditionTests
    {
        public static void Run()
        {
            Test_AllTargetsDead_ReturnsTrue();
            Test_SomeTargetsAlive_ReturnsFalse();
            Test_NonTargetAlive_StillTrue();
            Test_EmptyTargets_ReturnsTrue();
            Console.WriteLine("CheckVictoryConditionTests: All passed");
        }

        private static void Test_AllTargetsDead_ReturnsTrue()
        {
            var e1 = MakeEnemy("boss1");
            e1.Health.TakeDamage(999f);
            var enemies = new List<Enemy> { e1, MakeEnemy("grunt1") };
            var targets = new HashSet<string> { "boss1" };

            Assert(CheckVictoryCondition.Execute(enemies, targets) == true,
                "All targets dead should return true");
        }

        private static void Test_SomeTargetsAlive_ReturnsFalse()
        {
            var enemies = new List<Enemy> { MakeEnemy("boss1"), MakeEnemy("boss2") };
            var targets = new HashSet<string> { "boss1", "boss2" };

            Assert(CheckVictoryCondition.Execute(enemies, targets) == false,
                "Targets alive should return false");
        }

        private static void Test_NonTargetAlive_StillTrue()
        {
            var boss = MakeEnemy("boss1");
            boss.Health.TakeDamage(999f);
            var enemies = new List<Enemy> { boss, MakeEnemy("grunt1") };
            var targets = new HashSet<string> { "boss1" };

            Assert(CheckVictoryCondition.Execute(enemies, targets) == true,
                "Non-target alive should not prevent victory");
        }

        private static void Test_EmptyTargets_ReturnsTrue()
        {
            var enemies = new List<Enemy> { MakeEnemy("e1") };
            var targets = new HashSet<string>();

            Assert(CheckVictoryCondition.Execute(enemies, targets) == true,
                "Empty target set should return true (vacuous truth)");
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
