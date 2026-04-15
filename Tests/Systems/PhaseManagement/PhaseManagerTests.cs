using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Events;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Entities.Weapons;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Core.Form;
using TokuTactics.Systems.FormManagement;
using TokuTactics.Core.Phase;
using TokuTactics.Systems.PhaseManagement;
using TokuTactics.Data.Content;

namespace TokuTactics.Tests.Systems.PhaseManagement
{
    public class PhaseManagerTests
    {
        // === Helpers ===

        private PhaseManager MakeManager(FormPool formPool = null)
        {
            var eventBus = new EventBus();
            formPool ??= new FormPool("form_base", 3);
            return new PhaseManager(eventBus, formPool);
        }

        private Ranger MakeRanger(string id, float spd = 6f)
        {
            var baseForm = FormCatalog.BaseForm();
            return new Ranger(id, id, ElementalType.Blaze,
                new Proclivity(StatType.STR),
                null,
                StatBlock.Create(str: 8, def: 5, spd: spd, mag: 4),
                50f, baseForm);
        }

        private Enemy MakeEnemy(string id, float spd = 4f, float maxHealth = 25f)
        {
            return new Enemy(id, new EnemyData(
                id, id, EnemyTier.FootSoldier, null,
                StatBlock.Create(str: 5, def: 3, spd: spd),
                maxHealth: maxHealth, basicAttackPower: 1.0f,
                basicAttackRange: 1, movementRange: 3,
                behaviorTreeId: "bt_grunt"));
        }

        // === Mission Lifecycle ===

        public void StartMission_SetsActive()
        {
            var mgr = MakeManager();
            var rangers = new List<Ranger> { MakeRanger("r1") };
            var enemies = new List<Enemy> { MakeEnemy("e1") };

            mgr.StartMission(rangers, enemies);

            Assert(mgr.MissionState == MissionState.Active, "Should be active");
            Assert(mgr.RoundNumber == 0, "Round should be 0 before first StartRound");
        }

        public void StartRound_IncrementsRound()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();

            Assert(mgr.RoundNumber == 1, "Should be round 1");
        }

        public void MultipleRounds_Increment()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();
            mgr.StartPlayerPhase();
            mgr.EndPhase();
            mgr.StartEnemyPhase();
            mgr.EndPhase();

            mgr.StartRound();

            Assert(mgr.RoundNumber == 2, "Should be round 2");
        }

        // === Phase Transitions ===

        public void PlayerPhase_BuildsTurnOrder()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1", spd: 8), MakeRanger("r2", spd: 5) },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();
            mgr.StartPlayerPhase();

            Assert(mgr.PhaseState == PhaseState.PlayerPhase, "Should be player phase");
            Assert(mgr.PlayerTurnOrder.Entries.Count == 2, "Should have 2 entries");
            Assert(mgr.PlayerTurnOrder.Entries[0].Participant.ParticipantId == "r1",
                "Fastest Ranger (SPD 8) should go first");
        }

        public void EnemyPhase_BuildsTurnOrder()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1", spd: 6), MakeEnemy("e2", spd: 3) });

            mgr.StartRound();
            mgr.StartEnemyPhase();

            Assert(mgr.PhaseState == PhaseState.EnemyPhase, "Should be enemy phase");
            Assert(mgr.EnemyTurnOrder.Entries.Count == 2, "Should have 2 entries");
            Assert(mgr.EnemyTurnOrder.Entries[0].Participant.ParticipantId == "e1",
                "Fastest enemy (SPD 6) should go first");
        }

        public void EndPhase_ReturnsToIdle()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();
            mgr.StartPlayerPhase();
            mgr.EndPhase();

            Assert(mgr.PhaseState == PhaseState.Idle, "Should return to idle");
        }

        // === Turn Management ===

        public void AdvanceTurn_ReturnsUnitsInOrder()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger>
                {
                    MakeRanger("r_fast", spd: 10),
                    MakeRanger("r_slow", spd: 3)
                },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();
            mgr.StartPlayerPhase();

            var first = mgr.AdvanceTurn();
            Assert(first.Participant.ParticipantId == "r_fast", "Fast Ranger goes first");

            mgr.EndCurrentTurn();

            var second = mgr.AdvanceTurn();
            Assert(second.Participant.ParticipantId == "r_slow", "Slow Ranger goes second");

            mgr.EndCurrentTurn();

            var third = mgr.AdvanceTurn();
            Assert(third == null, "Phase should be complete");
            Assert(mgr.IsPhaseComplete(), "IsPhaseComplete should be true");
        }

        public void ActiveUnit_SetDuringTurn()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();
            mgr.StartPlayerPhase();
            mgr.AdvanceTurn();

            Assert(mgr.ActiveUnit != null, "Should have active unit during turn");
            Assert(mgr.ActiveUnit.Participant.ParticipantId == "r1", "Should be r1");

            mgr.EndCurrentTurn();

            Assert(mgr.ActiveUnit == null, "Should be null after ending turn");
        }

        // === Win Condition ===

        public void Victory_AllDefeatTargetsKilled()
        {
            var mgr = MakeManager();
            var enemy = MakeEnemy("boss_1", maxHealth: 1);

            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { enemy },
                new HashSet<string> { "boss_1" });

            mgr.StartRound();

            // Kill the boss
            enemy.TakeDamage(100);

            bool ended = mgr.CheckWinLoss();

            Assert(ended, "Mission should end");
            Assert(mgr.MissionState == MissionState.Victory, "Should be victory");
        }

        public void Victory_OnlyDefeatTargetsMatter()
        {
            var mgr = MakeManager();
            var boss = MakeEnemy("boss_1", maxHealth: 1);
            var grunt = MakeEnemy("grunt_1", maxHealth: 100);

            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { boss, grunt },
                new HashSet<string> { "boss_1" }); // Only boss matters

            mgr.StartRound();
            boss.TakeDamage(100);

            bool ended = mgr.CheckWinLoss();

            Assert(ended, "Should end when boss dies");
            Assert(mgr.MissionState == MissionState.Victory,
                "Grunt still alive but boss dead = victory");
        }

        public void NoVictory_DefeatTargetStillAlive()
        {
            var mgr = MakeManager();
            var enemy = MakeEnemy("boss_1", maxHealth: 100);

            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { enemy },
                new HashSet<string> { "boss_1" });

            mgr.StartRound();
            enemy.TakeDamage(10); // Damaged but alive

            bool ended = mgr.CheckWinLoss();

            Assert(!ended, "Mission should not end");
            Assert(mgr.MissionState == MissionState.Active, "Should still be active");
        }

        // === Loss Condition ===

        public void Defeat_RangerDied()
        {
            var mgr = MakeManager();
            var ranger = MakeRanger("r1");

            mgr.StartMission(
                new List<Ranger> { ranger },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();

            // Kill the Ranger (unmorphed, 50 HP)
            ranger.UnmorphedHealth.TakeDamage(100);

            bool ended = mgr.CheckWinLoss();

            Assert(ended, "Mission should end");
            Assert(mgr.MissionState == MissionState.Defeat, "Should be defeat");
            Assert(mgr.FallenRangerId == "r1", "Should record fallen Ranger");
        }

        public void NotifyMissionLost_SetsDefeat()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.StartRound();
            mgr.NotifyMissionLost("r1");

            Assert(mgr.MissionState == MissionState.Defeat, "Should be defeat");
            Assert(mgr.FallenRangerId == "r1", "Should record fallen Ranger");
        }

        // === Round Processing ===

        public void StartRound_ResetsComboChains()
        {
            var mgr = MakeManager();
            var ranger = MakeRanger("r1");
            ranger.Morph();
            ranger.ComboScaler.AdvanceChain();
            ranger.ComboScaler.AdvanceChain();

            mgr.StartMission(
                new List<Ranger> { ranger },
                new List<Enemy> { MakeEnemy("e1") });

            Assert(ranger.ComboScaler.ChainCount == 2, "Chain should be 2 before round start");

            mgr.StartRound();

            Assert(ranger.ComboScaler.ChainCount == 0, "Chain should reset at round start");
        }

        public void StartRound_TicksFormCooldowns()
        {
            var formPool = new FormPool("form_base", 3);
            var mgr = MakeManager(formPool);

            var blazeForm = FormCatalog.BlazeForm();
            formPool.RegisterForm(blazeForm);
            formPool.EquipForm(blazeForm.Id);
            // Occupy then vacate to start the cooldown
            formPool.OccupyForm(blazeForm.Id, "ranger_red");
            formPool.VacateForm(blazeForm.Id, "ranger_red", magModifier: 0); // 3 turn cooldown

            Assert(formPool.CheckAvailability(blazeForm.Id, "ranger_red")
                == FormAvailability.OnCooldown, "Should be on cooldown before round");

            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            // Round 1: tick from 3 → 2
            mgr.StartRound();
            Assert(formPool.CheckAvailability(blazeForm.Id, "ranger_red")
                == FormAvailability.OnCooldown, "Should still be on cooldown after 1 tick");

            // Round 2: tick from 2 → 1
            mgr.StartRound();
            Assert(formPool.CheckAvailability(blazeForm.Id, "ranger_red")
                == FormAvailability.OnCooldown, "Should still be on cooldown after 2 ticks");

            // Round 3: tick from 1 → 0
            mgr.StartRound();
            Assert(formPool.CheckAvailability(blazeForm.Id, "ranger_red")
                == FormAvailability.Available, "Should be available after 3 ticks");
        }

        public void DeadEnemies_ExcludedFromTurnOrder()
        {
            var mgr = MakeManager();
            var e1 = MakeEnemy("e1", spd: 6, maxHealth: 100);
            var e2 = MakeEnemy("e2", spd: 4, maxHealth: 1);

            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { e1, e2 },
                new HashSet<string> { "e1" }); // Only e1 matters for victory

            mgr.StartRound();

            // Kill e2 during the player phase
            e2.TakeDamage(100);

            mgr.StartEnemyPhase();

            Assert(mgr.EnemyTurnOrder.Entries.Count == 1,
                "Dead enemy should be excluded from turn order");
            Assert(mgr.EnemyTurnOrder.Entries[0].Participant.ParticipantId == "e1",
                "Only living enemy should be in turn order");
        }

        public void StartRound_ReturnsFalse_WhenMissionOver()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            mgr.NotifyMissionLost("r1");

            bool result = mgr.StartRound();
            Assert(!result, "StartRound should return false when mission is over");
        }

        // === Status Effect DoT During Round Start ===

        public void DoT_KillsForm_TriggersDemorphAtRoundStart()
        {
            var mgr = MakeManager();
            var ranger = MakeRanger("r1");
            ranger.Morph(); // Base form has 60 HP

            // Apply a massive DoT that will kill the form on tick
            var dot = new StatusEffectTemplate(
                "eff_lethal_dot", new TurnStartTrigger(),
                new DamageOverTimeBehavior(100f), 3);
            ranger.StatusEffects.Apply(dot.CreateInstance(1.0f));

            mgr.StartMission(
                new List<Ranger> { ranger },
                new List<Enemy> { MakeEnemy("e1") });

            Assert(ranger.MorphState == MorphState.Morphed, "Should be morphed before round");

            mgr.StartRound();

            Assert(ranger.MorphState == MorphState.Demorphed,
                "DoT should have killed form and triggered demorph");
            Assert(ranger.IsAlive, "Ranger should still be alive (unmorphed health intact)");
        }

        public void DoT_KillsUnmorphedRanger_MissionLost()
        {
            var mgr = MakeManager();
            var ranger = MakeRanger("r1"); // Unmorphed, 50 HP

            var dot = new StatusEffectTemplate(
                "eff_lethal_dot", new TurnStartTrigger(),
                new DamageOverTimeBehavior(100f), 3);
            ranger.StatusEffects.Apply(dot.CreateInstance(1.0f));

            mgr.StartMission(
                new List<Ranger> { ranger },
                new List<Enemy> { MakeEnemy("e1") });

            bool roundStarted = mgr.StartRound();

            Assert(!roundStarted, "StartRound should return false — mission ended");
            Assert(mgr.MissionState == MissionState.Defeat, "Should be defeat");
        }

        public void DoT_KillsEnemy_VictoryAtRoundStart()
        {
            var mgr = MakeManager();
            var enemy = MakeEnemy("boss_1", maxHealth: 5);

            var dot = new StatusEffectTemplate(
                "eff_lethal_dot", new TurnStartTrigger(),
                new DamageOverTimeBehavior(100f), 3);
            enemy.StatusEffects.Apply(dot.CreateInstance(1.0f));

            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { enemy },
                new HashSet<string> { "boss_1" });

            bool roundStarted = mgr.StartRound();

            Assert(!roundStarted, "StartRound should return false — mission ended");
            Assert(mgr.MissionState == MissionState.Victory,
                "DoT killed the boss — should be victory");
        }

        public void DoT_TriggersEnemyAggression()
        {
            var mgr = MakeManager();
            var enemy = MakeEnemy("e1", maxHealth: 100); // Threshold 0.3 = 30 HP

            // Apply DoT that does 80 damage — crosses threshold
            var dot = new StatusEffectTemplate(
                "eff_heavy_dot", new TurnStartTrigger(),
                new DamageOverTimeBehavior(80f), 3);
            enemy.StatusEffects.Apply(dot.CreateInstance(1.0f));

            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { enemy });

            Assert(!enemy.IsAggressive, "Should not be aggressive before round");

            mgr.StartRound();

            Assert(enemy.IsAggressive,
                "DoT crossing threshold should trigger aggression");
        }

        // === Full Round Flow ===

        public void FullRoundFlow_PlayerThenEnemy()
        {
            var mgr = MakeManager();
            mgr.StartMission(
                new List<Ranger> { MakeRanger("r1") },
                new List<Enemy> { MakeEnemy("e1") });

            // Round 1
            Assert(mgr.StartRound(), "Round should start");

            // Player phase
            Assert(mgr.StartPlayerPhase(), "Player phase should start");
            var playerTurn = mgr.AdvanceTurn();
            Assert(playerTurn != null, "Should have a player turn");
            mgr.EndCurrentTurn();
            Assert(mgr.IsPhaseComplete(), "Player phase should be complete");
            mgr.EndPhase();

            // Enemy phase
            Assert(mgr.StartEnemyPhase(), "Enemy phase should start");
            var enemyTurn = mgr.AdvanceTurn();
            Assert(enemyTurn != null, "Should have an enemy turn");
            mgr.EndCurrentTurn();
            Assert(mgr.IsPhaseComplete(), "Enemy phase should be complete");
            mgr.EndPhase();

            // Round 2
            Assert(mgr.StartRound(), "Next round should start");
            Assert(mgr.RoundNumber == 2, "Should be round 2");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new PhaseManagerTests();

            // Mission lifecycle
            t.StartMission_SetsActive();
            t.StartRound_IncrementsRound();
            t.MultipleRounds_Increment();

            // Phase transitions
            t.PlayerPhase_BuildsTurnOrder();
            t.EnemyPhase_BuildsTurnOrder();
            t.EndPhase_ReturnsToIdle();

            // Turn management
            t.AdvanceTurn_ReturnsUnitsInOrder();
            t.ActiveUnit_SetDuringTurn();

            // Win condition
            t.Victory_AllDefeatTargetsKilled();
            t.Victory_OnlyDefeatTargetsMatter();
            t.NoVictory_DefeatTargetStillAlive();

            // Loss condition
            t.Defeat_RangerDied();
            t.NotifyMissionLost_SetsDefeat();

            // Round processing
            t.StartRound_ResetsComboChains();
            t.StartRound_TicksFormCooldowns();
            t.DeadEnemies_ExcludedFromTurnOrder();
            t.StartRound_ReturnsFalse_WhenMissionOver();

            // Status effect DoT during round start
            t.DoT_KillsForm_TriggersDemorphAtRoundStart();
            t.DoT_KillsUnmorphedRanger_MissionLost();
            t.DoT_KillsEnemy_VictoryAtRoundStart();
            t.DoT_TriggersEnemyAggression();

            // Full flow
            t.FullRoundFlow_PlayerThenEnemy();

            System.Console.WriteLine("PhaseManagerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
