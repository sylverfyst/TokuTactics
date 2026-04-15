using System;
using System.Collections.Generic;
using TokuTactics.Commands.Gimmick;
using TokuTactics.Core.Grid;
using TokuTactics.Entities.Enemies.Gimmicks;

namespace TokuTactics.Tests.Commands.Gimmick
{
    public static class ResolveGimmickEffectsTests
    {
        public static void Run()
        {
            Test_NullOutput_ReturnsEmpty();
            Test_Damage_CreatesEffectsForTargetsInRange();
            Test_Healing_SetsOwnerHealing();
            Test_Shield_SetsShieldFields();
            Test_Displacement_CreatesEffects();
            Test_Spawn_CreatesEffects();
            Test_UsesInjectedBricks();
            Console.WriteLine("ResolveGimmickEffectsTests: All passed");
        }

        private static void Test_NullOutput_ReturnsEmpty()
        {
            var grid = new BattleGrid(10, 10);
            var result = ResolveGimmickEffects.Execute(
                grid, new GridPosition(5, 5), null, 3, new HashSet<string>());

            Assert(!result.HasEffects, "Null output should produce empty resolution");
        }

        private static void Test_Damage_CreatesEffectsForTargetsInRange()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("r1", new GridPosition(5, 4));
            grid.PlaceUnit("r2", new GridPosition(5, 6));
            var targets = new HashSet<string> { "r1", "r2" };
            var output = new GimmickOutput { Damage = 20f };

            var result = ResolveGimmickEffects.Execute(
                grid, new GridPosition(5, 5), output, 3, targets);

            Assert(result.DamageEffects.Count == 2,
                $"Expected 2 damage effects, got {result.DamageEffects.Count}");
            Assert(result.DamageEffects[0].Damage == 20f, "Damage should be 20");
        }

        private static void Test_Healing_SetsOwnerHealing()
        {
            var grid = new BattleGrid(10, 10);
            var output = new GimmickOutput { Healing = 15f };

            var result = ResolveGimmickEffects.Execute(
                grid, new GridPosition(5, 5), output, 0, new HashSet<string>());

            Assert(result.OwnerHealing == 15f, $"Expected 15 healing, got {result.OwnerHealing}");
        }

        private static void Test_Shield_SetsShieldFields()
        {
            var grid = new BattleGrid(10, 10);
            var output = new GimmickOutput { ActivateShield = true, ShieldDuration = 3 };

            var result = ResolveGimmickEffects.Execute(
                grid, new GridPosition(5, 5), output, 0, new HashSet<string>());

            Assert(result.ActivateShield, "Should activate shield");
            Assert(result.ShieldDuration == 3, $"Shield duration should be 3, got {result.ShieldDuration}");
        }

        private static void Test_Displacement_CreatesEffects()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("r1", new GridPosition(5, 4));
            var targets = new HashSet<string> { "r1" };
            var output = new GimmickOutput { DisplacementDistance = 2, DisplacementPush = true };

            var result = ResolveGimmickEffects.Execute(
                grid, new GridPosition(5, 5), output, 3, targets);

            Assert(result.Displacements.Count == 1,
                $"Expected 1 displacement, got {result.Displacements.Count}");
            Assert(result.Displacements[0].TargetId == "r1", "Should displace r1");
            Assert(result.Displacements[0].From == new GridPosition(5, 4), "From should be original pos");
            Assert(result.Displacements[0].To != result.Displacements[0].From, "Should move to new position");
        }

        private static void Test_Spawn_CreatesEffects()
        {
            var grid = new BattleGrid(10, 10);
            var output = new GimmickOutput
            {
                SpawnCount = 2,
                SpawnSearchRadius = 3,
                SpawnEnemyDataId = "putty"
            };

            var result = ResolveGimmickEffects.Execute(
                grid, new GridPosition(5, 5), output, 0, new HashSet<string>());

            Assert(result.Spawns.Count == 2, $"Expected 2 spawns, got {result.Spawns.Count}");
            Assert(result.Spawns[0].EnemyDataId == "putty", "Should spawn putty");
        }

        private static void Test_UsesInjectedBricks()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("r1", new GridPosition(5, 4));
            var targets = new HashSet<string> { "r1" };
            var output = new GimmickOutput { Damage = 10f, DisplacementDistance = 1, DisplacementPush = true };
            bool findCalled = false;
            bool displaceCalled = false;

            ResolveGimmickEffects.Execute(
                grid, new GridPosition(5, 5), output, 3, targets,
                findUnitsInRange: (g, p, r, t) =>
                {
                    findCalled = true;
                    return new List<string> { "r1" };
                },
                calculateDisplacement: (g, o, t, d, push) =>
                {
                    displaceCalled = true;
                    return new GridPosition(5, 3);
                });

            Assert(findCalled, "Should call injected findUnitsInRange");
            Assert(displaceCalled, "Should call injected calculateDisplacement");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
