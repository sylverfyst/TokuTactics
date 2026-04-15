using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Grid;
using TokuTactics.Core.Types;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Events;
using TokuTactics.Bricks.Assist;
using TokuTactics.Bricks.Shared;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Core.Assist;
using TokuTactics.Systems.AssistResolution;
using TokuTactics.Systems.CombatResolution;
using TokuTactics.Core.Form;
using TokuTactics.Systems.FormManagement;
using TokuTactics.Systems.GimmickResolution;
using TokuTactics.Systems.LoadoutSelection;
using TokuTactics.Core.Phase;
using TokuTactics.Systems.PhaseManagement;
using TokuTactics.Systems.SaveLoad;
using TokuTactics.Data.Content;

namespace TokuTactics.Systems.MissionSetup
{
    /// <summary>
    /// Holds all runtime systems for an active mission.
    /// 
    /// This is the single object the Godot scene needs. It contains
    /// every system, entity, and piece of state for the mission.
    /// The game layer reads from it and calls methods on it — no need
    /// to wire dependencies manually.
    /// 
    /// Created by MissionContext.Create() — one call builds the entire
    /// dependency graph from an episode definition and campaign save.
    /// </summary>
    public class MissionContext
    {
        // === Core Systems ===

        public EventBus EventBus { get; }
        public BattleGrid Grid { get; }
        public TypeChart TypeChart { get; }

        // === Entity Collections ===

        public List<Ranger> Rangers { get; }
        public List<Enemy> Enemies { get; }
        public Dictionary<string, Ranger> RangerLookup { get; }
        public Dictionary<string, Enemy> EnemyLookup { get; }

        // === Game Systems ===

        public FormPool FormPool { get; }
        public BondTracker BondTracker { get; }
        public AssistResolver AssistResolver { get; }
        public GimmickResolver GimmickResolver { get; }
        public CombatResolver CombatResolver { get; }
        public PhaseManager PhaseManager { get; }
        public LoadoutController LoadoutController { get; }

        // === Per-Unit Action Tracking ===

        /// <summary>Action budgets keyed by unit ID. Created fresh each mission.</summary>
        public Dictionary<string, ActionBudget> ActionBudgets { get; }

        // === Content ===

        public ContentRegistry ContentRegistry { get; }
        public EpisodeDefinition Episode { get; }
        public MapDefinition Map { get; }

        private MissionContext(
            EventBus eventBus, BattleGrid grid, TypeChart typeChart,
            List<Ranger> rangers, List<Enemy> enemies,
            FormPool formPool, BondTracker bondTracker,
            AssistResolver assistResolver, GimmickResolver gimmickResolver,
            CombatResolver combatResolver, PhaseManager phaseManager,
            LoadoutController loadoutController,
            Dictionary<string, ActionBudget> actionBudgets,
            ContentRegistry contentRegistry, EpisodeDefinition episode,
            MapDefinition map)
        {
            EventBus = eventBus;
            Grid = grid;
            TypeChart = typeChart;
            Rangers = rangers;
            Enemies = enemies;
            RangerLookup = rangers.ToDictionary(r => r.Id);
            EnemyLookup = enemies.ToDictionary(e => e.Id);
            FormPool = formPool;
            BondTracker = bondTracker;
            AssistResolver = assistResolver;
            GimmickResolver = gimmickResolver;
            CombatResolver = combatResolver;
            PhaseManager = phaseManager;
            LoadoutController = loadoutController;
            ActionBudgets = actionBudgets;
            ContentRegistry = contentRegistry;
            Episode = episode;
            Map = map;
        }

        // === Factory ===

        /// <summary>
        /// Create a fully wired MissionContext from an episode definition and campaign data.
        /// This is the single entry point for mission initialization.
        /// 
        /// Steps:
        /// 1. Build grid from map definition
        /// 2. Create Rangers from campaign data + content registry
        /// 3. Create Enemies from episode spawns + content registry
        /// 4. Place all units on grid
        /// 5. Initialize FormPool (budget, register forms, register instances)
        /// 6. Initialize BondTracker from campaign data
        /// 7. Construct all systems with proper dependencies
        /// 8. Return the complete MissionContext
        /// </summary>
        public static MissionContext Create(
            EpisodeDefinition episode,
            CampaignData campaignData,
            ContentRegistry registry,
            Random rng = null)
        {
            // === Step 1: Build Grid ===
            var map = registry.GetMap(episode.MapId);
            var grid = map.BuildGrid();

            // === Step 2: Create Rangers ===
            var rangers = new List<Ranger>();
            foreach (var rangerId in episode.AvailableRangerIds)
            {
                var rangerDef = registry.GetRanger(rangerId);
                if (rangerDef == null) continue;

                var rangerSave = campaignData.Rangers
                    .FirstOrDefault(r => r.RangerId == rangerId);

                var proclivity = new Proclivity(
                    rangerSave != null
                        ? ParseStatType(rangerSave.ProclivityStat)
                        : rangerDef.DefaultProclivity);

                var ranger = new Ranger(
                    rangerDef.Id, rangerDef.Name, rangerDef.IntrinsicType,
                    proclivity, rangerDef.PersonalAbility,
                    rangerDef.UnmorphedStats, rangerDef.UnmorphedMaxHealth,
                    rangerDef.BaseFormData);

                // Restore form levels from save
                if (rangerSave != null)
                {
                    foreach (var formLevel in rangerSave.FormLevels)
                    {
                        var formData = registry.GetForm(formLevel.FormId);
                        if (formData == null) continue;
                        var instance = ranger.GetOrCreateFormInstance(formData);
                        for (int i = 1; i < formLevel.Level; i++)
                            instance.AddExperience((int)instance.Data.BaseHealth); // Pump to level
                    }
                }

                rangers.Add(ranger);
            }

            // === Step 3: Create Enemies ===
            var enemies = new List<Enemy>();
            var groundPhase = episode.Phases.FirstOrDefault(p => p.Type == PhaseType.Ground);
            if (groundPhase != null)
            {
                foreach (var spawn in groundPhase.EnemySpawns)
                {
                    if (spawn.SpawnTurn > 0) continue; // Delayed spawns handled later

                    var enemyData = registry.GetEnemy(spawn.EnemyDataId);
                    if (enemyData == null) continue;

                    enemies.Add(new Enemy(spawn.InstanceId, enemyData));
                }
            }

            // === Step 4: Place Units on Grid ===
            for (int i = 0; i < rangers.Count && i < map.RangerSpawns.Count; i++)
            {
                grid.PlaceUnit(rangers[i].Id, map.RangerSpawns[i]);
            }

            if (groundPhase != null)
            {
                foreach (var spawn in groundPhase.EnemySpawns)
                {
                    if (spawn.SpawnTurn > 0) continue;
                    grid.PlaceUnit(spawn.InstanceId, spawn.Position);
                }
            }

            // === Step 5: Initialize FormPool ===
            // Base form ID derived from the first Ranger's base form data —
            // all Rangers share the same base form template.
            string baseFormId = rangers.Count > 0
                ? rangers[0].BaseForm.Data.Id
                : "form_base"; // Fallback for empty Ranger list

            var formPool = new FormPool(baseFormId, campaignData.FormBudget);

            // Register all forms from registry
            foreach (var formData in registry.AllForms.Values)
            {
                formPool.RegisterForm(formData);
            }

            // Register Ranger form instances for cooldown health regen
            foreach (var ranger in rangers)
            {
                foreach (var instance in ranger.FormInstances.Values)
                {
                    formPool.RegisterFormInstance(instance);
                }
            }

            // === Step 6: Initialize BondTracker ===
            var bondTracker = new BondTracker();
            foreach (var bondSave in campaignData.Bonds)
            {
                var bond = bondTracker.GetBond(bondSave.RangerAId, bondSave.RangerBId);
                bond.Experience = bondSave.Experience;
                bond.Tier = Bricks.Bond.ResolveBondTier.Execute(bond.Experience, bondTracker.TierThresholds);
            }

            // === Step 7: Construct Systems ===
            var eventBus = new EventBus();
            var typeChart = TypeChartSetup.Create();
            var rngInstance = rng ?? new Random();
            var tunableConstants = new Commands.Combat.TunableConstants();
            var assistResolver = new AssistResolver(grid, bondTracker);
            var gimmickResolver = new GimmickResolver(grid);
            var combatResolver = new CombatResolver(
                grid, typeChart, rngInstance, tunableConstants, assistResolver, gimmickResolver, bondTracker, eventBus);
            var phaseManager = new PhaseManager(eventBus, formPool);
            var loadoutController = new LoadoutController(formPool, eventBus);

            // === Action Budgets ===
            var actionBudgets = new Dictionary<string, ActionBudget>();
            foreach (var ranger in rangers)
                actionBudgets[ranger.Id] = new ActionBudget();
            foreach (var enemy in enemies)
                actionBudgets[enemy.Id] = new ActionBudget();

            return new MissionContext(
                eventBus, grid, typeChart,
                rangers, enemies, formPool, bondTracker,
                assistResolver, gimmickResolver, combatResolver,
                phaseManager, loadoutController, actionBudgets,
                registry, episode, map);
        }

        // === Helpers for the Game Layer ===

        /// <summary>
        /// Build AssistCandidateState dictionary from current Ranger state.
        /// Call this before every CombatResolver.ResolveRangerAttack.
        /// </summary>
        public Dictionary<string, AssistCandidateState> BuildAssistStates()
        {
            var states = new Dictionary<string, AssistCandidateState>();

            foreach (var ranger in Rangers)
            {
                ActionBudgets.TryGetValue(ranger.Id, out var budget);
                states[ranger.Id] = MapRangerToAssistState.Execute(ranger, budget);
            }

            return states;
        }

        /// <summary>
        /// Start a unit's turn: reset action budget, notify systems.
        /// Call when PhaseManager.AdvanceTurn() returns a new unit.
        /// </summary>
        public void BeginUnitTurn(string unitId)
        {
            if (ActionBudgets.ContainsKey(unitId))
            {
                StartBudgetTurn.Execute(ActionBudgets[unitId]);
            }
        }

        /// <summary>
        /// Start the mission on the PhaseManager with current entities and defeat targets.
        /// </summary>
        public void StartMission()
        {
            var defeatTargets = Episode.DefeatTargetIds.Count > 0
                ? new HashSet<string>(Episode.DefeatTargetIds)
                : null;

            PhaseManager.StartMission(Rangers, Enemies, defeatTargets);
        }

        // === Internal ===

        private static StatType ParseStatType(string s)
        {
            return s switch
            {
                "STR" => StatType.STR,
                "DEF" => StatType.DEF,
                "SPD" => StatType.SPD,
                "MAG" => StatType.MAG,
                "CHA" => StatType.CHA,
                "LCK" => StatType.LCK,
                _ => StatType.STR
            };
        }
    }
}
