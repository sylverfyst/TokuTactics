using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Grid;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.LoadoutSelection;
using TokuTactics.Systems.MissionSetup;
using TokuTactics.Systems.PhaseManagement;
using TokuTactics.Systems.SaveLoad;
using TokuTactics.Data.Content;

namespace TokuTactics.Tests.Systems.MissionSetup
{
    public class MissionContextTests
    {
        // === Helpers ===

        private CampaignData MakeCampaignData()
        {
            return new CampaignData
            {
                FormBudget = 3,
                Rangers = new List<RangerSaveData>
                {
                    new RangerSaveData { RangerId = "ranger_red", ProclivityStat = "STR" },
                    new RangerSaveData { RangerId = "ranger_blue", ProclivityStat = "CHA" },
                    new RangerSaveData { RangerId = "ranger_yellow", ProclivityStat = "SPD" },
                    new RangerSaveData { RangerId = "ranger_green", ProclivityStat = "DEF" },
                    new RangerSaveData { RangerId = "ranger_pink", ProclivityStat = "MAG" }
                },
                Bonds = new List<BondSaveData>()
            };
        }

        private MissionContext CreateTestMission()
        {
            var registry = ContentRegistry.CreateVerticalSlice();
            var episode = EpisodeCatalog.FrozenOutpostEpisode();
            var campaign = MakeCampaignData();

            return MissionContext.Create(episode, campaign, registry, new Random(42));
        }

        // === Construction ===

        public void Create_AllSystemsWired()
        {
            var ctx = CreateTestMission();

            Assert(ctx.EventBus != null, "EventBus should be created");
            Assert(ctx.Grid != null, "Grid should be created");
            Assert(ctx.TypeChart != null, "TypeChart should be created");
            Assert(ctx.DamageCalc != null, "DamageCalc should be created");
            Assert(ctx.FormPool != null, "FormPool should be created");
            Assert(ctx.BondTracker != null, "BondTracker should be created");
            Assert(ctx.AssistResolver != null, "AssistResolver should be created");
            Assert(ctx.GimmickResolver != null, "GimmickResolver should be created");
            Assert(ctx.CombatResolver != null, "CombatResolver should be created");
            Assert(ctx.PhaseManager != null, "PhaseManager should be created");
            Assert(ctx.LoadoutController != null, "LoadoutController should be created");
        }

        public void Create_RangersPlacedOnGrid()
        {
            var ctx = CreateTestMission();

            Assert(ctx.Rangers.Count == 5, "Should have 5 Rangers");

            foreach (var ranger in ctx.Rangers)
            {
                var pos = ctx.Grid.GetUnitPosition(ranger.Id);
                Assert(pos.HasValue, $"Ranger {ranger.Id} should be on grid");
            }
        }

        public void Create_EnemiesPlacedOnGrid()
        {
            var ctx = CreateTestMission();

            Assert(ctx.Enemies.Count == 7, "Should have 7 enemies");

            foreach (var enemy in ctx.Enemies)
            {
                var pos = ctx.Grid.GetUnitPosition(enemy.Id);
                Assert(pos.HasValue, $"Enemy {enemy.Id} should be on grid");
            }
        }

        public void Create_FormPoolConfigured()
        {
            var ctx = CreateTestMission();

            Assert(ctx.FormPool.Budget == 3, "Budget should be 3 from campaign data");
            Assert(!ctx.FormPool.IsLoadoutLocked, "Loadout should not be locked yet");
        }

        public void Create_ActionBudgetsForAllUnits()
        {
            var ctx = CreateTestMission();

            foreach (var ranger in ctx.Rangers)
            {
                Assert(ctx.ActionBudgets.ContainsKey(ranger.Id),
                    $"Should have action budget for {ranger.Id}");
            }

            foreach (var enemy in ctx.Enemies)
            {
                Assert(ctx.ActionBudgets.ContainsKey(enemy.Id),
                    $"Should have action budget for {enemy.Id}");
            }
        }

        public void Create_LookupTablesPopulated()
        {
            var ctx = CreateTestMission();

            Assert(ctx.RangerLookup["ranger_red"] != null, "Ranger lookup works");
            Assert(ctx.EnemyLookup["wyrm_1"] != null, "Enemy lookup works");
        }

        // === Assist State Builder ===

        public void BuildAssistStates_AllRangersIncluded()
        {
            var ctx = CreateTestMission();

            var states = ctx.BuildAssistStates();

            Assert(states.Count == 5, "Should have state for all 5 Rangers");
            foreach (var ranger in ctx.Rangers)
            {
                Assert(states.ContainsKey(ranger.Id),
                    $"Should have state for {ranger.Id}");
            }
        }

        public void BuildAssistStates_ReflectsCurrentState()
        {
            var ctx = CreateTestMission();
            var red = ctx.RangerLookup["ranger_red"];

            // Before morph — unmorphed
            var statesBefore = ctx.BuildAssistStates();
            Assert(!statesBefore["ranger_red"].IsMorphed, "Should be unmorphed initially");

            // Morph red
            ctx.LoadoutController.SubmitLoadout(new List<string> { "form_blaze", "form_torrent" });
            ctx.LoadoutController.RequestMorph(red);

            // After morph — morphed
            var statesAfter = ctx.BuildAssistStates();
            Assert(statesAfter["ranger_red"].IsMorphed, "Should be morphed after morph");
            Assert(statesAfter["ranger_red"].CurrentFormId == "form_base",
                "Should be in base form after morph");
        }

        // === Full Mission Flow ===

        public void FullFlow_StartMission_StartRound_RunPhases()
        {
            var ctx = CreateTestMission();

            // Start mission
            ctx.StartMission();
            Assert(ctx.PhaseManager.MissionState == MissionState.Active,
                "Mission should be active");

            // Start round 1
            Assert(ctx.PhaseManager.StartRound(), "Round should start");
            Assert(ctx.PhaseManager.RoundNumber == 1, "Should be round 1");

            // Player phase
            Assert(ctx.PhaseManager.StartPlayerPhase(), "Player phase should start");

            var firstTurn = ctx.PhaseManager.AdvanceTurn();
            Assert(firstTurn != null, "Should have a unit to act");

            ctx.BeginUnitTurn(firstTurn.Participant.ParticipantId);
            var budget = ctx.ActionBudgets[firstTurn.Participant.ParticipantId];
            Assert(budget.CanMove, "Should be able to move on turn start");
            Assert(budget.CanAct, "Should be able to act on turn start");

            ctx.PhaseManager.EndCurrentTurn();
        }

        public void FullFlow_MorphThroughLoadout()
        {
            var ctx = CreateTestMission();
            ctx.StartMission();
            ctx.PhaseManager.StartRound();
            ctx.PhaseManager.StartPlayerPhase();

            var red = ctx.RangerLookup["ranger_red"];

            // Request morph — should need loadout
            var result1 = ctx.LoadoutController.RequestMorph(red);
            Assert(result1 == MorphRequestResult.NeedsLoadout,
                "First morph should require loadout");

            // Submit loadout
            var loadoutResult = ctx.LoadoutController.SubmitLoadout(
                new List<string> { "form_blaze", "form_torrent", "form_frost" });
            Assert(loadoutResult == LoadoutResult.Accepted, "Loadout should be accepted");

            // Morph again — should succeed
            var result2 = ctx.LoadoutController.RequestMorph(red);
            Assert(result2 == MorphRequestResult.MorphComplete, "Morph should complete");
            Assert(red.MorphState == MorphState.Morphed, "Red should be morphed");
        }

        public void FullFlow_CombatAction()
        {
            var ctx = CreateTestMission();
            ctx.StartMission();
            ctx.PhaseManager.StartRound();
            ctx.PhaseManager.StartPlayerPhase();

            var red = ctx.RangerLookup["ranger_red"];

            // Morph red
            ctx.LoadoutController.SubmitLoadout(
                new List<string> { "form_blaze", "form_torrent", "form_frost" });
            ctx.LoadoutController.RequestMorph(red);

            // Find an adjacent enemy (move red next to putty_1)
            ctx.Grid.MoveUnit("ranger_red", new GridPosition(4, 6)); // Adjacent to putty_3 at (5,6)

            var putty = ctx.EnemyLookup["putty_3"];
            float healthBefore = putty.Health.Current;

            // Attack
            var assistStates = ctx.BuildAssistStates();
            var combatResult = ctx.CombatResolver.ResolveRangerAttack(
                red, putty,
                red.CurrentForm.Data.BasicAttackPower,
                null, assistStates);

            Assert(combatResult.PrimaryDamage != null, "Should have damage result");
            Assert(putty.Health.Current < healthBefore || combatResult.PrimaryDamage.WasDodged,
                "Putty should take damage (unless dodged)");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new MissionContextTests();

            // Construction
            t.Create_AllSystemsWired();
            t.Create_RangersPlacedOnGrid();
            t.Create_EnemiesPlacedOnGrid();
            t.Create_FormPoolConfigured();
            t.Create_ActionBudgetsForAllUnits();
            t.Create_LookupTablesPopulated();

            // Assist state builder
            t.BuildAssistStates_AllRangersIncluded();
            t.BuildAssistStates_ReflectsCurrentState();

            // Full flow
            t.FullFlow_StartMission_StartRound_RunPhases();
            t.FullFlow_MorphThroughLoadout();
            t.FullFlow_CombatAction();

            System.Console.WriteLine("MissionContextTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
