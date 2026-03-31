using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Enemies.Gimmicks;
using TokuTactics.Entities.Enemies.Gimmicks.Triggers;
using TokuTactics.Entities.Enemies.Gimmicks.Behaviors;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Tests.Entities.Enemies
{
    public class EnemyTests
    {
        // === Builders ===

        private EnemyData MakeFootSoldier(ElementalType? type = null)
        {
            return new EnemyData(
                "foot_basic", "Putty", EnemyTier.FootSoldier,
                type, StatBlock.Create(str: 5, def: 3, spd: 4),
                maxHealth: 30, basicAttackPower: 1.0f, basicAttackRange: 1,
                movementRange: 4, behaviorTreeId: "bt_grunt",
                aggressiveBehaviorTreeId: "bt_grunt_aggressive",
                aggressionThreshold: 0.3f);
        }

        private EnemyData MakeMonster(GimmickData gimmick = null)
        {
            gimmick ??= new GimmickData(
                "gimmick_poison_aura", "Poison Aura",
                new TurnStartGimmickTrigger(),
                new StatusEffectGimmickBehavior(
                    new StatusEffectTemplate("eff_poison",
                        new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 3),
                    range: 1),
                cooldown: 2);

            return new EnemyData(
                "monster_ep1", "Venom Beast", EnemyTier.Monster,
                ElementalType.Shadow, StatBlock.Create(str: 12, def: 8, spd: 5, mag: 6),
                maxHealth: 150, basicAttackPower: 1.5f, basicAttackRange: 1,
                movementRange: 3, behaviorTreeId: "bt_monster",
                aggressiveBehaviorTreeId: "bt_monster_aggressive",
                aggressionThreshold: 0.4f, gimmick: gimmick);
        }

        private EnemyData MakeLieutenant(GimmickData gimmick = null)
        {
            var weapon = new WeaponData(
                "wpn_lt_blade", "Dark Blade", 1.8f, 1,
                new StatusEffectTemplate("eff_bleed",
                    new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 3));

            gimmick ??= new GimmickData(
                "gimmick_lt_trap", "Shadow Trap",
                new TurnStartGimmickTrigger(),
                new TerrainModifyGimmickBehavior(TokuTactics.Core.Grid.TerrainType.Hazard, radius: 2),
                cooldown: 3);

            return new EnemyData(
                "lt_shadow", "Shadow Commander", EnemyTier.Lieutenant,
                ElementalType.Shadow, StatBlock.Create(str: 15, def: 12, spd: 7, mag: 8),
                maxHealth: 200, basicAttackPower: 1.3f, basicAttackRange: 1,
                movementRange: 4, behaviorTreeId: "bt_lieutenant",
                aggressiveBehaviorTreeId: "bt_lieutenant_aggressive",
                aggressionThreshold: 0.25f, usesUtilityScoring: true,
                weapon: weapon, gimmick: gimmick);
        }

        // === Construction ===

        public void FootSoldier_ConstructsCorrectly()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier(ElementalType.Blaze));

            Assert(enemy.Id == "foot_1", "Should have correct ID");
            Assert(enemy.Data.Tier == EnemyTier.FootSoldier, "Should be FootSoldier tier");
            Assert(enemy.IsAlive, "Should be alive");
            Assert(!enemy.HasWeapon, "Foot soldiers have no weapon");
            Assert(!enemy.HasGimmick, "Foot soldiers have no gimmick");
        }

        public void FootSoldier_TypelessUsesNormal()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier(type: null));

            Assert(enemy.DualType.RangerType == ElementalType.Normal,
                "Typeless should use Normal");
        }

        public void FootSoldier_TypedHasType()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier(ElementalType.Blaze));

            Assert(enemy.DualType.RangerType == ElementalType.Blaze, "Should have Blaze type");
        }

        public void Monster_HasGimmick()
        {
            var enemy = new Enemy("monster_1", MakeMonster());

            Assert(enemy.HasGimmick, "Monster should have gimmick");
            Assert(enemy.Data.Gimmick.Id == "gimmick_poison_aura", "Should have correct gimmick");
            Assert(enemy.IsGimmickVoluntary, "TurnStart trigger should be voluntary");
        }

        public void Lieutenant_HasWeaponAndGimmick()
        {
            var enemy = new Enemy("lt_1", MakeLieutenant());

            Assert(enemy.HasWeapon, "Lieutenant should have weapon");
            Assert(enemy.HasGimmick, "Lieutenant should have gimmick");
            Assert(enemy.Data.Weapon.Id == "wpn_lt_blade", "Should have correct weapon");
            Assert(enemy.Data.Gimmick.Id == "gimmick_lt_trap", "Should have correct gimmick");
        }

        // === Damage ===

        public void TakeDamage_ReducesHealth()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier());
            var evt = enemy.TakeDamage(10);

            Assert(enemy.Health.Current == 20, "Should have 20 health");
            Assert(evt.DamageDealt == 10, "Should report 10 damage");
        }

        public void TakeDamage_LethalKills()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier());
            var evt = enemy.TakeDamage(999);

            Assert(!enemy.IsAlive, "Should be dead");
            Assert(evt.Died, "Should report death");
        }

        // === Aggression ===

        public void Aggression_TriggeredBelowThreshold()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier());
            var evt = enemy.TakeDamage(22); // 8/30 = 26%, below 30%

            Assert(enemy.IsAggressive, "Should be aggressive");
            Assert(evt.BecameAggressive, "Should report change");
        }

        public void Aggression_StaysTrueAfterHealing()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier());
            enemy.TakeDamage(25);
            enemy.Health.Heal(20);

            Assert(enemy.IsAggressive, "Should stay aggressive");
        }

        public void Aggression_SwitchesBehaviorTree()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier());
            Assert(enemy.ActiveBehaviorTreeId == "bt_grunt", "Normal tree");
            enemy.TakeDamage(25);
            Assert(enemy.ActiveBehaviorTreeId == "bt_grunt_aggressive", "Aggressive tree");
        }

        // === Composable Gimmick Triggers ===

        public void Gimmick_TurnStartTrigger_AlwaysActivates()
        {
            var gimmick = new GimmickData("g", "G",
                new TurnStartGimmickTrigger(),
                new DamageGimmickBehavior(10, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            Assert(enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "TurnStart should always activate");
        }

        public void Gimmick_EveryNTurnsTrigger_RespectsTurnCount()
        {
            var gimmick = new GimmickData("g", "G",
                new EveryNTurnsGimmickTrigger(3),
                new DamageGimmickBehavior(10, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            Assert(!enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "Should not trigger at turn 0");

            enemy.StartTurn(); enemy.StartTurn(); enemy.StartTurn();

            Assert(enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "Should trigger at turn 3");
        }

        public void Gimmick_EveryNTurnsTrigger_ResetsAfterActivation()
        {
            var gimmick = new GimmickData("g", "G",
                new EveryNTurnsGimmickTrigger(2),
                new DamageGimmickBehavior(10, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            enemy.StartTurn(); enemy.StartTurn();
            enemy.OnGimmickActivated();

            Assert(!enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "Should not trigger immediately after activation");

            enemy.StartTurn(); enemy.StartTurn();
            Assert(enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "Should trigger again after N turns");
        }

        public void Gimmick_HealthThresholdTrigger_FiresOnce()
        {
            var gimmick = new GimmickData("g", "G",
                new HealthThresholdGimmickTrigger(0.5f),
                new ShieldGimmickBehavior(3));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            Assert(!enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "Should not trigger at full health");

            enemy.TakeDamage(80); // 70/150 = 46%
            Assert(enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "Should trigger below threshold");

            enemy.OnGimmickActivated();
            Assert(!enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()),
                "Should not trigger again (one-shot)");
        }

        public void Gimmick_OnHitTrigger_IsReactive()
        {
            var gimmick = new GimmickData("g", "G",
                new OnHitGimmickTrigger(),
                new DamageGimmickBehavior(10, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            Assert(!enemy.IsGimmickVoluntary, "OnHit should NOT be voluntary");

            var context = enemy.BuildGimmickContext();
            Assert(!enemy.ShouldGimmickActivate(context),
                "Should not trigger without WasJustHit");

            context.WasJustHit = true;
            Assert(enemy.ShouldGimmickActivate(context),
                "Should trigger with WasJustHit");
        }

        public void Gimmick_AdjacentTrigger_IsReactive()
        {
            var gimmick = new GimmickData("g", "G",
                new RangerAdjacentGimmickTrigger(),
                new StatusEffectGimmickBehavior(null, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            Assert(!enemy.IsGimmickVoluntary, "Adjacent should NOT be voluntary");

            var context = enemy.BuildGimmickContext();
            context.IsRangerAdjacent = true;
            Assert(enemy.ShouldGimmickActivate(context),
                "Should trigger with adjacent Ranger");
        }

        public void Gimmick_Cooldown_BlocksActivation()
        {
            var gimmick = new GimmickData("g", "G",
                new TurnStartGimmickTrigger(),
                new DamageGimmickBehavior(10, 1),
                cooldown: 2);
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            enemy.OnGimmickActivated();
            Assert(!enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()), "On cooldown");

            enemy.StartTurn(); enemy.StartTurn();
            Assert(enemy.ShouldGimmickActivate(enemy.BuildGimmickContext()), "Cooldown expired");
        }

        // === Composable Gimmick Behaviors (Declarative Output) ===

        public void GimmickBehavior_Damage_ProducesOutput()
        {
            var behavior = new DamageGimmickBehavior(20f, 2);
            var context = new GimmickContext { OwnerMag = 10f };

            var output = behavior.GetOutput(context);

            Assert(output.Damage > 0, "Should produce damage");
            Assert(output.HasEffect, "Should have effect");
            Assert(behavior.Range == 2, "Range should be 2");
        }

        public void GimmickBehavior_Spawn_ProducesOutput()
        {
            var behavior = new SpawnGimmickBehavior(4, "foot_basic");
            var output = behavior.GetOutput(new GimmickContext());

            Assert(output.SpawnCount == 4, "Should spawn 4");
            Assert(output.SpawnEnemyDataId == "foot_basic", "Correct spawn type");
        }

        public void GimmickBehavior_Shield_ProducesOutput()
        {
            var behavior = new ShieldGimmickBehavior(3);
            var output = behavior.GetOutput(new GimmickContext());

            Assert(output.ActivateShield, "Should activate shield");
            Assert(output.ShieldDuration == 3, "Duration should be 3");
        }

        public void GimmickBehavior_TerrainModify_ProducesOutput()
        {
            var behavior = new TerrainModifyGimmickBehavior(
                TokuTactics.Core.Grid.TerrainType.Hazard, radius: 2);
            var output = behavior.GetOutput(new GimmickContext());

            Assert(output.ModifyTerrain, "Should modify terrain");
            Assert(output.TargetTerrain == TokuTactics.Core.Grid.TerrainType.Hazard, "Should set hazard");
            Assert(output.TerrainRadius == 2, "Radius should be 2");
        }

        public void GimmickBehavior_Heal_ProducesOutput()
        {
            var behavior = new HealGimmickBehavior(30f);
            var output = behavior.GetOutput(new GimmickContext { OwnerMag = 0f });

            Assert(output.Healing > 0, "Should produce healing");
        }

        public void GimmickBehavior_Displacement_ProducesOutput()
        {
            var behavior = new DisplacementGimmickBehavior(2, isPush: true, range: 3);
            var output = behavior.GetOutput(new GimmickContext());

            Assert(output.DisplacementDistance == 2, "Distance should be 2");
            Assert(output.DisplacementPush, "Should be push");
            Assert(behavior.Range == 3, "Range should be 3");
        }

        // === Shield ===

        public void Shield_BlocksDamage()
        {
            var enemy = new Enemy("m_1", MakeMonster());
            enemy.ActivateShield(2);

            var evt = enemy.TakeDamage(50);

            Assert(evt.WasShielded, "Should be shielded");
            Assert(evt.DamageDealt == 0, "No damage through shield");
        }

        public void Shield_ExpiresAfterTurns()
        {
            var enemy = new Enemy("m_1", MakeMonster());
            enemy.ActivateShield(2);

            enemy.StartTurn();
            Assert(enemy.IsShielded, "Still shielded after 1");

            enemy.StartTurn();
            Assert(!enemy.IsShielded, "Expired after 2");
        }

        // === Action Slots ===

        public void FootSoldier_HasOneAction()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier());
            Assert(enemy.MaxActionsPerTurn == 1, "1 action");
        }

        public void Monster_HasTwoActions()
        {
            var enemy = new Enemy("m_1", MakeMonster());
            Assert(enemy.MaxActionsPerTurn == 2, "2 actions");
        }

        public void Lieutenant_HasTwoActions()
        {
            var enemy = new Enemy("lt_1", MakeLieutenant());
            Assert(enemy.MaxActionsPerTurn == 2, "2 actions");
        }

        public void ConsumeAction_PreventsDoublePick()
        {
            var enemy = new Enemy("m_1", MakeMonster());
            enemy.StartTurn();

            Assert(enemy.ConsumeAction(EnemyActionType.BasicAttack), "First basic succeeds");
            Assert(!enemy.ConsumeAction(EnemyActionType.BasicAttack), "Double basic fails");
        }

        public void StartTurn_ResetsActionSlots()
        {
            var enemy = new Enemy("m_1", MakeMonster());
            enemy.StartTurn();
            enemy.ConsumeAction(EnemyActionType.BasicAttack);
            enemy.ConsumeAction(EnemyActionType.Gimmick);

            enemy.StartTurn();

            Assert(enemy.CanUseAction, "Should have actions");
            Assert(enemy.ActionsUsed.Count == 0, "Used set should be cleared");
        }

        // === Available Actions ===

        public void FootSoldier_AvailableActions_BasicOnly()
        {
            var enemy = new Enemy("foot_1", MakeFootSoldier());
            var actions = enemy.GetAvailableActions();

            Assert(actions.Count == 1, "1 action type");
            Assert(actions.Contains(EnemyActionType.BasicAttack), "Basic only");
        }

        public void Monster_AvailableActions_BasicAndGimmick()
        {
            var gimmick = new GimmickData("g", "G",
                new TurnStartGimmickTrigger(),
                new DamageGimmickBehavior(10, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            var actions = enemy.GetAvailableActions();

            Assert(actions.Contains(EnemyActionType.BasicAttack), "Has basic");
            Assert(actions.Contains(EnemyActionType.Gimmick), "Has gimmick");
            Assert(!actions.Contains(EnemyActionType.WeaponAttack), "No weapon");
        }

        public void Lieutenant_AvailableActions_AllThree()
        {
            var enemy = new Enemy("lt_1", MakeLieutenant());
            var actions = enemy.GetAvailableActions();

            Assert(actions.Count == 3, "3 types available");
            Assert(enemy.MaxActionsPerTurn == 2, "But only 2 slots");
        }

        public void Lieutenant_AvailableActions_SubtractsUsed()
        {
            var enemy = new Enemy("lt_1", MakeLieutenant());
            enemy.StartTurn();
            enemy.ConsumeAction(EnemyActionType.WeaponAttack);

            var actions = enemy.GetAvailableActions();

            Assert(!actions.Contains(EnemyActionType.WeaponAttack), "Weapon used");
            Assert(actions.Contains(EnemyActionType.BasicAttack), "Basic available");
            Assert(actions.Contains(EnemyActionType.Gimmick), "Gimmick available");
        }

        public void ReactiveGimmick_NotInAvailableActions()
        {
            var gimmick = new GimmickData("g", "G",
                new OnHitGimmickTrigger(),
                new DamageGimmickBehavior(10, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            var actions = enemy.GetAvailableActions();

            Assert(!actions.Contains(EnemyActionType.Gimmick),
                "OnHit gimmick should NOT appear in voluntary actions");
        }

        public void AdjacencyGimmick_NotInAvailableActions()
        {
            var gimmick = new GimmickData("g", "G",
                new RangerAdjacentGimmickTrigger(),
                new StatusEffectGimmickBehavior(null, 1));
            var enemy = new Enemy("m_1", MakeMonster(gimmick));

            Assert(!enemy.GetAvailableActions().Contains(EnemyActionType.Gimmick),
                "Adjacent gimmick should NOT appear in voluntary actions");
        }

        public void Lieutenant_EveryNTurnsGimmick_NotAvailableBeforeN()
        {
            var gimmick = new GimmickData("g", "G",
                new EveryNTurnsGimmickTrigger(3),
                new SpawnGimmickBehavior(2, "foot_basic"));
            var enemy = new Enemy("lt_1", MakeLieutenant(gimmick));

            Assert(!enemy.GetAvailableActions().Contains(EnemyActionType.Gimmick),
                "Not ready before N turns");

            enemy.StartTurn(); enemy.StartTurn(); enemy.StartTurn();

            Assert(enemy.GetAvailableActions().Contains(EnemyActionType.Gimmick),
                "Ready after N turns");
        }

        public void DeadEnemy_NoAvailableActions()
        {
            var enemy = new Enemy("lt_1", MakeLieutenant());
            enemy.TakeDamage(999);

            Assert(enemy.GetAvailableActions().Count == 0, "Dead = no actions");
        }

        // === Data Independence ===

        public void SharedData_InstancesAreIndependent()
        {
            var data = MakeFootSoldier(ElementalType.Blaze);
            var enemy1 = new Enemy("foot_1", data);
            var enemy2 = new Enemy("foot_2", data);

            enemy1.TakeDamage(20);

            Assert(enemy1.Health.Current == 10, "Enemy 1 damaged");
            Assert(enemy2.Health.Current == 30, "Enemy 2 untouched");
        }

        public void SharedData_HealthThresholdTrigger_IndependentPerInstance()
        {
            var gimmick = new GimmickData("g", "G",
                new HealthThresholdGimmickTrigger(0.5f),
                new ShieldGimmickBehavior(3));
            var data = MakeMonster(gimmick);

            var enemy1 = new Enemy("m_1", data);
            var enemy2 = new Enemy("m_2", data);

            // Damage enemy1 below threshold and fire its gimmick
            enemy1.TakeDamage(80); // 70/150 = 46%
            Assert(enemy1.ShouldGimmickActivate(enemy1.BuildGimmickContext()),
                "Enemy 1 should trigger");
            enemy1.OnGimmickActivated();
            Assert(!enemy1.ShouldGimmickActivate(enemy1.BuildGimmickContext()),
                "Enemy 1 should not trigger again (one-shot)");

            // Enemy2 should still be able to trigger independently
            enemy2.TakeDamage(80);
            Assert(enemy2.ShouldGimmickActivate(enemy2.BuildGimmickContext()),
                "Enemy 2 should trigger independently — not blocked by enemy 1's one-shot");
        }

        public void SharedData_StatelessTrigger_SafeToShare()
        {
            var gimmick = new GimmickData("g", "G",
                new TurnStartGimmickTrigger(),
                new DamageGimmickBehavior(10, 1));
            var data = MakeMonster(gimmick);

            var enemy1 = new Enemy("m_1", data);
            var enemy2 = new Enemy("m_2", data);

            // Both should activate independently
            Assert(enemy1.ShouldGimmickActivate(enemy1.BuildGimmickContext()),
                "Enemy 1 should trigger");
            enemy1.OnGimmickActivated();

            Assert(enemy2.ShouldGimmickActivate(enemy2.BuildGimmickContext()),
                "Enemy 2 should trigger independently");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new EnemyTests();

            // Construction
            t.FootSoldier_ConstructsCorrectly();
            t.FootSoldier_TypelessUsesNormal();
            t.FootSoldier_TypedHasType();
            t.Monster_HasGimmick();
            t.Lieutenant_HasWeaponAndGimmick();

            // Damage
            t.TakeDamage_ReducesHealth();
            t.TakeDamage_LethalKills();

            // Aggression
            t.Aggression_TriggeredBelowThreshold();
            t.Aggression_StaysTrueAfterHealing();
            t.Aggression_SwitchesBehaviorTree();

            // Composable Gimmick Triggers
            t.Gimmick_TurnStartTrigger_AlwaysActivates();
            t.Gimmick_EveryNTurnsTrigger_RespectsTurnCount();
            t.Gimmick_EveryNTurnsTrigger_ResetsAfterActivation();
            t.Gimmick_HealthThresholdTrigger_FiresOnce();
            t.Gimmick_OnHitTrigger_IsReactive();
            t.Gimmick_AdjacentTrigger_IsReactive();
            t.Gimmick_Cooldown_BlocksActivation();

            // Composable Gimmick Behaviors
            t.GimmickBehavior_Damage_ProducesOutput();
            t.GimmickBehavior_Spawn_ProducesOutput();
            t.GimmickBehavior_Shield_ProducesOutput();
            t.GimmickBehavior_TerrainModify_ProducesOutput();
            t.GimmickBehavior_Heal_ProducesOutput();
            t.GimmickBehavior_Displacement_ProducesOutput();

            // Shield
            t.Shield_BlocksDamage();
            t.Shield_ExpiresAfterTurns();

            // Action Slots
            t.FootSoldier_HasOneAction();
            t.Monster_HasTwoActions();
            t.Lieutenant_HasTwoActions();
            t.ConsumeAction_PreventsDoublePick();
            t.StartTurn_ResetsActionSlots();

            // Available Actions
            t.FootSoldier_AvailableActions_BasicOnly();
            t.Monster_AvailableActions_BasicAndGimmick();
            t.Lieutenant_AvailableActions_AllThree();
            t.Lieutenant_AvailableActions_SubtractsUsed();
            t.ReactiveGimmick_NotInAvailableActions();
            t.AdjacencyGimmick_NotInAvailableActions();
            t.Lieutenant_EveryNTurnsGimmick_NotAvailableBeforeN();
            t.DeadEnemy_NoAvailableActions();

            // Data Independence
            t.SharedData_InstancesAreIndependent();
            t.SharedData_HealthThresholdTrigger_IndependentPerInstance();
            t.SharedData_StatelessTrigger_SafeToShare();

            System.Console.WriteLine("EnemyTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
