using System;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Commands.Combat
{
    public static class ResolveTargetDeathTests
    {
        public static void Run()
        {
            Test_AliveEnemy_NoDeath();
            Test_DeadEnemy_ReportsEnemyDeath();
            Test_AliveRanger_NoDeath();
            Test_MorphedFormDead_ReportsFormDeath();
            Test_UnmorphedDead_ReportsMissionLost();
            Console.WriteLine("ResolveTargetDeathTests: All passed");
        }

        private static void Test_AliveEnemy_NoDeath()
        {
            var enemy = MakeEnemy("e1");
            var result = ResolveTargetDeath.Execute(enemy);
            Assert(!result.TargetDied, "Alive enemy should not be dead");
        }

        private static void Test_DeadEnemy_ReportsEnemyDeath()
        {
            var enemy = MakeEnemy("e1");
            enemy.Health.TakeDamage(999f);

            var result = ResolveTargetDeath.Execute(enemy);

            Assert(result.TargetDied, "Dead enemy should report death");
            Assert(result.EnemyTypeId == "e1", $"Expected e1, got {result.EnemyTypeId}");
            Assert(result.EnemyType == ElementalType.Normal, "Should report enemy type");
            Assert(!result.MissionLost, "Enemy death should not lose mission");
        }

        private static void Test_AliveRanger_NoDeath()
        {
            var ranger = MakeRanger("r1");
            var result = ResolveTargetDeath.Execute(ranger);
            Assert(!result.TargetDied, "Alive ranger should not be dead");
            Assert(!result.FormDied, "No form death");
        }

        private static void Test_MorphedFormDead_ReportsFormDeath()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph();
            ranger.CurrentForm.Health.TakeDamage(999f);

            var result = ResolveTargetDeath.Execute(ranger);

            Assert(result.FormDied, "Should report form death");
            Assert(result.LostFormId != null, "Should have form ID");
            Assert(!result.MissionLost, "Form death should not lose mission");
            Assert(!result.TargetDied, "Form death is not target death");
        }

        private static void Test_UnmorphedDead_ReportsMissionLost()
        {
            var ranger = MakeRanger("r1");
            ranger.UnmorphedHealth.TakeDamage(999f);

            var result = ResolveTargetDeath.Execute(ranger);

            Assert(result.TargetDied, "Unmorphed death is target death");
            Assert(result.MissionLost, "Unmorphed death should lose mission");
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
