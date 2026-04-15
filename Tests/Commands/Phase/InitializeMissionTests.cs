using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Commands.Phase;
using TokuTactics.Core.Stats;
using TokuTactics.Entities.Enemies;

namespace TokuTactics.Tests.Commands.Phase
{
    public static class InitializeMissionTests
    {
        public static void Run()
        {
            Test_NullTargets_DefaultsToAllEnemies();
            Test_ExplicitTargets_UsesProvided();
            Test_EmptyEnemies_EmptyTargets();
            Console.WriteLine("InitializeMissionTests: All passed");
        }

        private static void Test_NullTargets_DefaultsToAllEnemies()
        {
            var enemies = new List<Enemy> { MakeEnemy("e1"), MakeEnemy("e2") };

            var result = InitializeMission.Execute(enemies, null);

            Assert(result.DefeatTargetIds.Count == 2, $"Expected 2 targets, got {result.DefeatTargetIds.Count}");
            Assert(result.DefeatTargetIds.Contains("e1"), "Should contain e1");
            Assert(result.DefeatTargetIds.Contains("e2"), "Should contain e2");
        }

        private static void Test_ExplicitTargets_UsesProvided()
        {
            var enemies = new List<Enemy> { MakeEnemy("e1"), MakeEnemy("e2") };
            var targets = new HashSet<string> { "e1" };

            var result = InitializeMission.Execute(enemies, targets);

            Assert(result.DefeatTargetIds.Count == 1, $"Expected 1 target, got {result.DefeatTargetIds.Count}");
            Assert(result.DefeatTargetIds.Contains("e1"), "Should contain e1");
        }

        private static void Test_EmptyEnemies_EmptyTargets()
        {
            var enemies = new List<Enemy>();

            var result = InitializeMission.Execute(enemies, null);

            Assert(result.DefeatTargetIds.Count == 0, "Empty enemies should produce empty targets");
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
