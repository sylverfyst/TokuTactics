using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class ValidateReactiveGimmickTests
    {
        public static void Run()
        {
            Test_Dodged_ReturnsFalse();
            Test_RangerTarget_ReturnsFalse();
            Test_DeadEnemy_ReturnsFalse();
            Test_AliveEnemyNoGimmick_ReturnsTrue();
            Console.WriteLine("ValidateReactiveGimmickTests: All passed");
        }

        private static void Test_Dodged_ReturnsFalse()
        {
            var enemy = MakeEnemy("e1");
            Assert(ValidateReactiveGimmick.Execute(enemy, wasDodged: true) == false,
                "Dodged attack should not trigger gimmick");
        }

        private static void Test_RangerTarget_ReturnsFalse()
        {
            var ranger = new Ranger("r1", "r1", ElementalType.Blaze,
                new Proclivity(StatType.STR), null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4),
                50f, FormCatalog.BaseForm());
            Assert(ValidateReactiveGimmick.Execute(ranger, wasDodged: false) == false,
                "Ranger target should not trigger gimmick");
        }

        private static void Test_DeadEnemy_ReturnsFalse()
        {
            var enemy = MakeEnemy("e1");
            enemy.Health.TakeDamage(999f);
            Assert(ValidateReactiveGimmick.Execute(enemy, wasDodged: false) == false,
                "Dead enemy should not trigger gimmick");
        }

        private static void Test_AliveEnemyNoGimmick_ReturnsTrue()
        {
            // Enemy with no gimmick — IsGimmickVoluntary returns false (no gimmick trigger)
            var enemy = MakeEnemy("e1");
            Assert(ValidateReactiveGimmick.Execute(enemy, wasDodged: false) == true,
                "Alive enemy hit by attack should return true");
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
