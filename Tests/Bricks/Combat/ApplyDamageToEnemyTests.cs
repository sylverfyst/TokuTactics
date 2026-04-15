using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Stats;
using TokuTactics.Entities.Enemies;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class ApplyDamageToEnemyTests
    {
        public static void Run()
        {
            Test_DealsDamage();
            Test_ReturnsAggressionFlag();
            Test_ReturnsDiedFlag();
            Test_ShieldedReturnsZero();
            Console.WriteLine("ApplyDamageToEnemyTests: All passed");
        }

        private static void Test_DealsDamage()
        {
            var enemy = MakeEnemy("e1", 100f);
            var evt = ApplyDamageToEnemy.Execute(enemy, 30);
            Assert(enemy.Health.Current == 70f, $"Expected 70, got {enemy.Health.Current}");
            Assert(evt.DamageDealt == 30f, $"Expected 30 dealt, got {evt.DamageDealt}");
        }

        private static void Test_ReturnsAggressionFlag()
        {
            var enemy = MakeEnemy("e1", 100f, aggressionThreshold: 0.5f);
            var evt = ApplyDamageToEnemy.Execute(enemy, 60);
            Assert(evt.BecameAggressive, "Should trigger aggression at 40% health");
        }

        private static void Test_ReturnsDiedFlag()
        {
            var enemy = MakeEnemy("e1", 50f);
            var evt = ApplyDamageToEnemy.Execute(enemy, 999);
            Assert(evt.Died, "Should be dead");
            Assert(!enemy.IsAlive, "Enemy should not be alive");
        }

        private static void Test_ShieldedReturnsZero()
        {
            var enemy = MakeEnemy("e1", 100f);
            enemy.ActivateShield(3);
            var evt = ApplyDamageToEnemy.Execute(enemy, 50);
            Assert(evt.WasShielded, "Should be shielded");
            Assert(evt.DamageDealt == 0, "Shielded should deal 0");
            Assert(enemy.Health.Current == 100f, "Health unchanged when shielded");
        }

        private static Enemy MakeEnemy(string id, float maxHealth, float aggressionThreshold = 0f)
        {
            return new Enemy(id, new EnemyData(
                id, id, EnemyTier.FootSoldier, null,
                StatBlock.Create(str: 5, def: 3, spd: 4),
                maxHealth: maxHealth, basicAttackPower: 1.0f,
                basicAttackRange: 1, movementRange: 3,
                behaviorTreeId: "bt_grunt",
                aggressionThreshold: aggressionThreshold));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
