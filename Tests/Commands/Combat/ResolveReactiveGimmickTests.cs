using System;
using System.Collections.Generic;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Commands.Combat
{
    public static class ResolveReactiveGimmickTests
    {
        public static void Run()
        {
            Test_Dodged_ReturnsNull();
            Test_RangerTarget_ReturnsNull();
            Test_UsesInjectedValidator();
            Console.WriteLine("ResolveReactiveGimmickTests: All passed");
        }

        private static void Test_Dodged_ReturnsNull()
        {
            var enemy = MakeEnemy("e1");

            var result = ResolveReactiveGimmick.Execute(
                enemy, wasDodged: true, grid: null, gimmickResolver: null,
                rangerIds: new HashSet<string>());

            Assert(result == null, "Dodged attack should return null");
        }

        private static void Test_RangerTarget_ReturnsNull()
        {
            var ranger = new Ranger("r1", "r1", ElementalType.Blaze,
                new Proclivity(StatType.STR), null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4),
                50f, FormCatalog.BaseForm());

            var result = ResolveReactiveGimmick.Execute(
                ranger, wasDodged: false, grid: null, gimmickResolver: null,
                rangerIds: new HashSet<string>());

            Assert(result == null, "Ranger target should return null");
        }

        private static void Test_UsesInjectedValidator()
        {
            var enemy = MakeEnemy("e1");
            bool validatorCalled = false;

            ResolveReactiveGimmick.Execute(
                enemy, wasDodged: false, grid: null, gimmickResolver: null,
                rangerIds: new HashSet<string>(),
                validateGimmick: (t, d) => { validatorCalled = true; return false; });

            Assert(validatorCalled, "Should call injected validator");
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
