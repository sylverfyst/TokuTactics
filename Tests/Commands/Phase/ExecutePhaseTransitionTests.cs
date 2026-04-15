using System;
using System.Collections.Generic;
using TokuTactics.Commands.Phase;
using TokuTactics.Core.Events;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.FormManagement;
using TokuTactics.Systems.PhaseManagement;

namespace TokuTactics.Tests.Commands.Phase
{
    public static class ExecutePhaseTransitionTests
    {
        public static void Run()
        {
            Test_TransitionsToEnemyPhaseAndBack();
            Test_AllEnemiesAreProcessed();
            Test_EnemyTurnProcessorIsCalled();
            Test_RoundNumberIncrements();
            Test_FirstPlayerUnitIsReturned();
            Test_BeginUnitTurnIsCalledForNextUnit();
            Console.WriteLine("ExecutePhaseTransitionTests: All passed");
        }

        private static void Test_TransitionsToEnemyPhaseAndBack()
        {
            var (mgr, _, _) = SetupPlayerPhaseComplete();

            var result = ExecutePhaseTransition.Execute(mgr, _ => { });

            Assert(!result.MissionEnded, "Mission should not end");
            Assert(result.NextUnitId != null, "Should have a next unit");
            Assert(mgr.PhaseState == PhaseState.PlayerPhase, "Should be back in player phase");
        }

        private static void Test_AllEnemiesAreProcessed()
        {
            var (mgr, _, enemies) = SetupPlayerPhaseComplete();

            var result = ExecutePhaseTransition.Execute(mgr, _ => { });

            Assert(result.EnemyTurnsProcessed.Count == enemies.Count,
                $"Expected {enemies.Count} enemy turns, got {result.EnemyTurnsProcessed.Count}");
        }

        private static void Test_EnemyTurnProcessorIsCalled()
        {
            var (mgr, _, _) = SetupPlayerPhaseComplete();
            var processedIds = new List<string>();

            var result = ExecutePhaseTransition.Execute(
                mgr,
                _ => { },
                processEnemyTurn: id => processedIds.Add(id));

            Assert(processedIds.Count == 3, $"Processor should be called for each enemy, got {processedIds.Count}");
            Assert(processedIds.Contains("e1"), "Should process e1");
            Assert(processedIds.Contains("e2"), "Should process e2");
            Assert(processedIds.Contains("e3"), "Should process e3");
        }

        private static void Test_RoundNumberIncrements()
        {
            var (mgr, _, _) = SetupPlayerPhaseComplete();
            int roundBefore = mgr.RoundNumber;

            var result = ExecutePhaseTransition.Execute(mgr, _ => { });

            Assert(result.RoundNumber == roundBefore + 1,
                $"Round should increment from {roundBefore} to {roundBefore + 1}, got {result.RoundNumber}");
        }

        private static void Test_FirstPlayerUnitIsReturned()
        {
            // Rangers have different SPD: r1=10, r2=5 — r1 should go first
            var (mgr, rangers, _) = SetupPlayerPhaseComplete();

            var result = ExecutePhaseTransition.Execute(mgr, _ => { });

            Assert(result.NextUnitId == "r1",
                $"Highest SPD ranger should go first, got {result.NextUnitId}");
        }

        private static void Test_BeginUnitTurnIsCalledForNextUnit()
        {
            var (mgr, _, _) = SetupPlayerPhaseComplete();
            string begunUnitId = null;

            var result = ExecutePhaseTransition.Execute(
                mgr,
                beginUnitTurn: id => begunUnitId = id);

            Assert(begunUnitId == result.NextUnitId,
                $"BeginUnitTurn should be called for {result.NextUnitId}, got {begunUnitId}");
        }

        // === Helpers ===

        /// <summary>
        /// Sets up a PhaseManager at the end of a player phase (all rangers have acted),
        /// ready for the phase transition command.
        /// </summary>
        private static (PhaseManager mgr, List<Ranger> rangers, List<Enemy> enemies) SetupPlayerPhaseComplete()
        {
            var eventBus = new EventBus();
            var formPool = new FormPool("form_base", 3);
            var mgr = new PhaseManager(eventBus, formPool);

            var rangers = new List<Ranger>
            {
                MakeRanger("r1", spd: 10f),
                MakeRanger("r2", spd: 5f)
            };
            var enemies = new List<Enemy>
            {
                MakeEnemy("e1", spd: 8f),
                MakeEnemy("e2", spd: 4f),
                MakeEnemy("e3", spd: 2f)
            };

            mgr.StartMission(rangers, enemies);
            mgr.StartRound();
            mgr.StartPlayerPhase();

            // Exhaust all player turns
            while (!mgr.IsPhaseComplete())
            {
                var unit = mgr.AdvanceTurn();
                if (unit == null) break;
                mgr.EndCurrentTurn();
            }

            return (mgr, rangers, enemies);
        }

        private static Ranger MakeRanger(string id, float spd = 6f)
        {
            var baseForm = FormCatalog.BaseForm();
            return new Ranger(id, id, ElementalType.Blaze,
                new Proclivity(StatType.STR),
                null,
                StatBlock.Create(str: 8, def: 5, spd: spd, mag: 4),
                50f, baseForm);
        }

        private static Enemy MakeEnemy(string id, float spd = 4f)
        {
            return new Enemy(id, new EnemyData(
                id, id, EnemyTier.FootSoldier, null,
                StatBlock.Create(str: 5, def: 3, spd: spd),
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
