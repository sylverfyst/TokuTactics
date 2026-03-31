using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Grid;
using TokuTactics.Core.Types;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Events;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Entities.Weapons;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.AssistResolution;
using TokuTactics.Systems.GimmickResolution;
using TokuTactics.Systems.CombatResolution;
using TokuTactics.Data.Content;

namespace TokuTactics.Tests.Systems.CombatResolution
{
    public class CombatResolverTests
    {
        // === Setup Helpers ===

        /// <summary>Deterministic RNG: seeded so crits/dodges are predictable.</summary>
        /// <remarks>
        /// Seed 42 with our DamageCalculator: first few rolls are above dodge/crit thresholds,
        /// so default attacks hit without crit. Use seed 0 for specific crit/dodge tests.
        /// </remarks>
        private Random DeterministicRng() => new Random(42);

        private TypeChart MakeTypeChart() => TypeChartSetup.Create();

        private CombatResolver MakeResolver(
            BattleGrid grid, BondTracker bondTracker = null,
            Random rng = null)
        {
            var typeChart = MakeTypeChart();
            var damageCalc = new DamageCalculator(typeChart, rng ?? DeterministicRng());
            bondTracker ??= new BondTracker();
            var assistResolver = new AssistResolver(grid, bondTracker);
            var gimmickResolver = new GimmickResolver(grid);
            var eventBus = new EventBus();

            return new CombatResolver(
                grid, damageCalc, assistResolver, gimmickResolver, bondTracker, eventBus);
        }

        private Ranger MakeRanger(string id, ElementalType type = ElementalType.Blaze)
        {
            var baseForm = FormCatalog.BaseForm();
            return new Ranger(
                id, id, type,
                new Proclivity(StatType.STR),
                null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4),
                50f, baseForm);
        }

        private void MorphRanger(Ranger ranger)
        {
            ranger.Morph(); // Into base form
        }

        private void MorphIntoForm(Ranger ranger, FormData formData)
        {
            if (ranger.MorphState == MorphState.Unmorphed)
                ranger.Morph();
            var instance = ranger.GetOrCreateFormInstance(formData);
            ranger.SwitchForm(instance);
        }

        private Enemy MakeEnemy(string id, EnemyData data = null)
        {
            data ??= EnemyCatalog.Putty();
            return new Enemy(id, data);
        }

        private Dictionary<string, AssistCandidateState> NoAssistStates()
        {
            return new Dictionary<string, AssistCandidateState>();
        }

        private Dictionary<string, AssistCandidateState> MakeAssistStates(
            params (string id, AssistCandidateState state)[] entries)
        {
            var dict = new Dictionary<string, AssistCandidateState>();
            foreach (var (id, state) in entries)
                dict[id] = state;
            return dict;
        }

        // === Basic Attack: Ranger → Enemy ===

        public void RangerAttacksEnemy_DealsDamage()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            var result = resolver.ResolveRangerAttack(
                ranger, enemy, 1.0f, null, NoAssistStates());

            Assert(result.PrimaryDamage != null, "Should have damage result");
            Assert(result.PrimaryDamage.FinalDamage > 0, "Should deal damage");
            Assert(enemy.Health.Current < enemy.Health.Maximum, "Enemy should be damaged");
        }

        public void RangerAttacksEnemy_KillsEnemy()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            // High action power to guarantee kill
            var result = resolver.ResolveRangerAttack(
                ranger, enemy, 50.0f, null, NoAssistStates());

            Assert(result.TargetDied, "Enemy should be dead");
            Assert(!enemy.IsAlive, "Enemy entity should report dead");
        }

        public void RangerAttacksEnemy_AppliesWeaponStatus()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            var burnEffect = FormCatalog.BlazeWeaponB().StatusEffect;

            var result = resolver.ResolveRangerAttack(
                ranger, enemy, 1.0f, burnEffect, NoAssistStates());

            Assert(result.StatusEffectsApplied.Contains("eff_burn"),
                "Should apply burn from weapon");
            Assert(enemy.StatusEffects.HasEffect("eff_burn"),
                "Enemy should have burn effect");
        }

        public void RangerAttacksEnemy_TriggersAggression()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1"); // 25 HP, threshold 0.3 = 7.5
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            // Deal enough to cross threshold but not kill (need >17.5 damage to cross threshold)
            // Using high power to account for damage calculation with stats/type matchup
            var result = resolver.ResolveRangerAttack(
                ranger, enemy, 50.0f, null, NoAssistStates());

            Assert(result.AggressionTriggered, "Should trigger aggression");
            Assert(enemy.IsAggressive, "Enemy should be aggressive");
        }

        // === Basic Attack: Enemy → Ranger ===

        public void EnemyAttacksRanger_DamagesForm()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            float formHealthBefore = ranger.CurrentForm.Health.Current;

            var result = resolver.ResolveEnemyAttack(enemy, ranger, 1.0f, null);

            Assert(ranger.CurrentForm.Health.Current < formHealthBefore,
                "Form health should decrease");
        }

        public void EnemyAttacksRanger_FormDeath_Demorphs()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            // Massive damage to kill the form
            var result = resolver.ResolveEnemyAttack(enemy, ranger, 200.0f, null);

            Assert(result.FormDied, "Form should die");
            Assert(result.LostFormId == "form_base", "Should lose base form");
            Assert(ranger.MorphState == MorphState.Demorphed, "Should be demorphed");
        }

        public void EnemyAttacksUnmorphedRanger_MissionLost()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            // Stay unmorphed
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            var result = resolver.ResolveEnemyAttack(enemy, ranger, 200.0f, null);

            Assert(result.TargetDied, "Ranger should die");
            Assert(result.MissionLost, "Mission should be lost");
        }

        // === Assists ===

        public void RangerAttackWithAssist_BothDealDamage()
        {
            var grid = new BattleGrid(10, 10);
            var bondTracker = new BondTracker();
            var resolver = MakeResolver(grid, bondTracker);

            var attacker = MakeRanger("ranger_red");
            MorphRanger(attacker);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            grid.PlaceUnit("ranger_blue", new GridPosition(5, 4));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(6, 5));

            var states = MakeAssistStates(
                ("ranger_red", new AssistCandidateState { IsMorphed = true }),
                ("ranger_blue", new AssistCandidateState
                {
                    IsMorphed = true,
                    CurrentFormId = "form_base",
                    BaseFormId = "form_base",
                    WeaponBasePower = 0.8f,
                    Str = 6f,
                    Cha = 7f
                }));

            float healthBefore = enemy.Health.Current;

            var result = resolver.ResolveRangerAttack(
                attacker, enemy, 1.0f, null, states);

            Assert(result.AssistResults.Count == 1, "Should have 1 assist");
            Assert(enemy.Health.Current < healthBefore - result.PrimaryDamage.FinalDamage,
                "Total damage should exceed primary damage alone");
        }

        public void RangerAttackWithAssist_AwardsBondExperience()
        {
            var grid = new BattleGrid(10, 10);
            var bondTracker = new BondTracker();
            var resolver = MakeResolver(grid, bondTracker);

            var attacker = MakeRanger("ranger_red");
            MorphRanger(attacker);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            grid.PlaceUnit("ranger_blue", new GridPosition(5, 4));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(6, 5));

            var states = MakeAssistStates(
                ("ranger_red", new AssistCandidateState { IsMorphed = true }),
                ("ranger_blue", new AssistCandidateState
                {
                    IsMorphed = true,
                    CurrentFormId = "form_base",
                    BaseFormId = "form_base",
                    WeaponBasePower = 0.8f,
                    Str = 6f,
                    Cha = 7f
                }));

            int xpBefore = bondTracker.GetBond("ranger_red", "ranger_blue").Experience;

            resolver.ResolveRangerAttack(attacker, enemy, 1.0f, null, states);

            int xpAfter = bondTracker.GetBond("ranger_red", "ranger_blue").Experience;
            Assert(xpAfter > xpBefore, "Bond experience should increase from assist");
        }

        // === Reactive Gimmick ===

        public void AttackOnHitMonster_TriggersReactiveGimmick()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            // Create an enemy with an OnHit gimmick
            var gimmick = new GimmickData("g_counter", "Counter",
                new TokuTactics.Entities.Enemies.Gimmicks.Triggers.OnHitGimmickTrigger(),
                new TokuTactics.Entities.Enemies.Gimmicks.Behaviors.DamageGimmickBehavior(10f, 2));

            var monsterData = new EnemyData(
                "monster_counter", "Counter Beast", EnemyTier.Monster,
                ElementalType.Shadow, StatBlock.Create(str: 10, def: 8),
                maxHealth: 200, basicAttackPower: 1.0f, basicAttackRange: 1,
                movementRange: 3, behaviorTreeId: "bt_monster",
                gimmick: gimmick);

            var monster = new Enemy("monster_1", monsterData);
            grid.PlaceUnit("monster_1", new GridPosition(5, 4));

            var result = resolver.ResolveRangerAttack(
                ranger, monster, 1.0f, null, NoAssistStates());

            Assert(result.ReactiveGimmick != null, "OnHit gimmick should trigger");
        }

        // === Combo Scaling ===

        public void ComboScaling_SubsequentAttacksReduced()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphIntoForm(ranger, FormCatalog.BlazeForm());
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("enemy_1", new EnemyData(
                "tough", "Tough", EnemyTier.FootSoldier,
                null, StatBlock.Create(def: 5),
                maxHealth: 500, basicAttackPower: 1.0f, basicAttackRange: 1,
                movementRange: 3, behaviorTreeId: "bt_grunt"));
            grid.PlaceUnit("enemy_1", new GridPosition(5, 4));

            // First attack — full damage (chain count 0)
            var result1 = resolver.ResolveRangerAttack(
                ranger, enemy, 1.5f, null, NoAssistStates());

            // Simulate form switch (advances combo chain)
            ranger.SwitchForm(ranger.GetOrCreateFormInstance(FormCatalog.TorrentForm()));

            // Second attack — reduced by combo scaling
            var result2 = resolver.ResolveRangerAttack(
                ranger, enemy, 1.2f, null, NoAssistStates());

            // The combo multiplier should be < 1.0 on the second attack
            Assert(result2.PrimaryDamage.ComboMultiplier < 1.0f,
                "Second attack in chain should have reduced combo multiplier");
        }

        // === Total Damage Tracking ===

        public void TotalDamage_SumsPrimaryAndAssists()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var attacker = MakeRanger("ranger_red");
            MorphRanger(attacker);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));
            grid.PlaceUnit("ranger_blue", new GridPosition(5, 4));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(6, 5));

            var states = MakeAssistStates(
                ("ranger_red", new AssistCandidateState { IsMorphed = true }),
                ("ranger_blue", new AssistCandidateState
                {
                    IsMorphed = true,
                    CurrentFormId = "form_base",
                    BaseFormId = "form_base",
                    WeaponBasePower = 0.8f,
                    Str = 6f,
                    Cha = 7f
                }));

            var result = resolver.ResolveRangerAttack(
                attacker, enemy, 1.0f, null, states);

            int expected = (result.PrimaryDamage.WasDodged ? 0 : result.PrimaryDamage.FinalDamage)
                + result.AssistResults.Sum(a => a.Damage?.WasDodged == false ? a.Damage.FinalDamage : 0);

            Assert(result.TotalDamage == expected,
                "TotalDamage should equal primary + all assist damage");
        }

        // === Dodge ===

        public void Dodge_NoDamageNoStatus()
        {
            var grid = new BattleGrid(10, 10);
            // Guarantee dodge: set base dodge chance to 100%
            var resolver = MakeResolverWithGuaranteedDodge(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(5, 4));

            var burnEffect = FormCatalog.BlazeWeaponB().StatusEffect;

            var result = resolver.ResolveRangerAttack(
                ranger, enemy, 10.0f, burnEffect, NoAssistStates());

            Assert(result.PrimaryDamage.WasDodged, "Attack should be dodged");
            Assert(result.PrimaryDamage.FinalDamage == 0, "Dodged attack should deal 0 damage");
            Assert(enemy.Health.Current == enemy.Health.Maximum, "Enemy should be undamaged");
            Assert(!enemy.StatusEffects.HasEffect("eff_burn"),
                "Status effect should not apply on dodge");
            Assert(result.StatusEffectsApplied.Count == 0,
                "No status effects should be recorded");
        }

        public void Dodge_AssistsDontFire()
        {
            var grid = new BattleGrid(10, 10);
            var bondTracker = new BondTracker();
            var resolver = MakeResolverWithGuaranteedDodge(grid, bondTracker);

            var attacker = MakeRanger("ranger_red");
            MorphRanger(attacker);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));
            grid.PlaceUnit("ranger_blue", new GridPosition(5, 4));

            var enemy = MakeEnemy("putty_1");
            grid.PlaceUnit("putty_1", new GridPosition(6, 5));

            var states = MakeAssistStates(
                ("ranger_red", new AssistCandidateState { IsMorphed = true }),
                ("ranger_blue", new AssistCandidateState
                {
                    IsMorphed = true,
                    CurrentFormId = "form_base",
                    BaseFormId = "form_base",
                    WeaponBasePower = 0.8f,
                    Str = 6f,
                    Cha = 7f
                }));

            var result = resolver.ResolveRangerAttack(
                attacker, enemy, 1.0f, null, states);

            Assert(result.AssistResults.Count == 0,
                "No assists should fire when primary attack is dodged");
            Assert(enemy.Health.Current == enemy.Health.Maximum,
                "Enemy should take no damage from dodged attack + no assists");
        }

        // === Assists Skip Dead Targets ===

        public void AssistsSkipDeadTarget()
        {
            var grid = new BattleGrid(10, 10);
            var bondTracker = new BondTracker();
            var resolver = MakeResolver(grid, bondTracker);

            var attacker = MakeRanger("ranger_red");
            MorphRanger(attacker);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            // Two adjacent assisters
            grid.PlaceUnit("ranger_blue", new GridPosition(5, 4));
            grid.PlaceUnit("ranger_green", new GridPosition(4, 5));

            // Very weak enemy — primary attack will kill it
            var enemy = MakeEnemy("weak_1", new EnemyData(
                "weak", "Weak", EnemyTier.FootSoldier,
                null, StatBlock.Create(def: 1),
                maxHealth: 1, basicAttackPower: 1.0f, basicAttackRange: 1,
                movementRange: 3, behaviorTreeId: "bt_grunt"));
            grid.PlaceUnit("weak_1", new GridPosition(6, 5));

            var states = MakeAssistStates(
                ("ranger_red", new AssistCandidateState { IsMorphed = true }),
                ("ranger_blue", new AssistCandidateState
                {
                    IsMorphed = true, CurrentFormId = "form_base",
                    BaseFormId = "form_base", WeaponBasePower = 0.8f,
                    Str = 6f, Cha = 5f
                }),
                ("ranger_green", new AssistCandidateState
                {
                    IsMorphed = true, CurrentFormId = "form_base",
                    BaseFormId = "form_base", WeaponBasePower = 0.8f,
                    Str = 6f, Cha = 5f
                }));

            var result = resolver.ResolveRangerAttack(
                attacker, enemy, 50.0f, null, states);

            Assert(!enemy.IsAlive, "Enemy should be dead from primary");
            Assert(result.AssistResults.Count == 0,
                "No assists should process against a dead target");
        }

        // === Enemy Weapon Status ===

        public void EnemyAttack_AppliesWeaponStatus()
        {
            var grid = new BattleGrid(10, 10);
            var resolver = MakeResolver(grid);

            var ranger = MakeRanger("ranger_red");
            MorphRanger(ranger);
            grid.PlaceUnit("ranger_red", new GridPosition(5, 5));

            var enemy = MakeEnemy("commander_1", EnemyCatalog.ShadowCommander());
            grid.PlaceUnit("commander_1", new GridPosition(5, 4));

            var bleedEffect = EnemyCatalog.ShadowCommanderWeapon().StatusEffect;

            var result = resolver.ResolveEnemyAttack(enemy, ranger, 1.8f, bleedEffect);

            Assert(!result.PrimaryDamage.WasDodged, "Attack should land");
            Assert(result.StatusEffectsApplied.Contains("eff_bleed"),
                "Should apply bleed from Dark Blade");
            Assert(ranger.StatusEffects.HasEffect("eff_bleed"),
                "Ranger should have bleed effect");
        }

        // === Test Runner ===

        private CombatResolver MakeResolverWithGuaranteedDodge(
            BattleGrid grid, BondTracker bondTracker = null)
        {
            var typeChart = MakeTypeChart();
            var damageCalc = new DamageCalculator(typeChart, DeterministicRng());
            damageCalc.BaseDodgeChance = 1.0f; // Guarantee dodge
            bondTracker ??= new BondTracker();
            var assistResolver = new AssistResolver(grid, bondTracker);
            var gimmickResolver = new GimmickResolver(grid);
            var eventBus = new EventBus();

            return new CombatResolver(
                grid, damageCalc, assistResolver, gimmickResolver, bondTracker, eventBus);
        }

        public static void RunAll()
        {
            var t = new CombatResolverTests();

            // Ranger → Enemy
            t.RangerAttacksEnemy_DealsDamage();
            t.RangerAttacksEnemy_KillsEnemy();
            t.RangerAttacksEnemy_AppliesWeaponStatus();
            t.RangerAttacksEnemy_TriggersAggression();

            // Enemy → Ranger
            t.EnemyAttacksRanger_DamagesForm();
            t.EnemyAttacksRanger_FormDeath_Demorphs();
            t.EnemyAttacksUnmorphedRanger_MissionLost();
            t.EnemyAttack_AppliesWeaponStatus();

            // Assists
            t.RangerAttackWithAssist_BothDealDamage();
            t.RangerAttackWithAssist_AwardsBondExperience();
            t.AssistsSkipDeadTarget();

            // Dodge
            t.Dodge_NoDamageNoStatus();
            t.Dodge_AssistsDontFire();

            // Reactive Gimmick
            t.AttackOnHitMonster_TriggersReactiveGimmick();

            // Combo Scaling
            t.ComboScaling_SubsequentAttacksReduced();

            // Total Damage
            t.TotalDamage_SumsPrimaryAndAssists();

            System.Console.WriteLine("CombatResolverTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
