using System;
using System.Collections.Generic;
using TokuTactics.Commands.Phase;
using TokuTactics.Core.Health;
using TokuTactics.Core.Stats;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Commands.Phase
{
    public static class ProcessRoundStatusEffectsTests
    {
        public static void Run()
        {
            Test_NoEffects_EmptyResult();
            Test_RangerDoT_AppliesDamage();
            Test_FormDeath_ProducesDemorphEvent();
            Test_UsesInjectedBricks();
            Console.WriteLine("ProcessRoundStatusEffectsTests: All passed");
        }

        private static void Test_NoEffects_EmptyResult()
        {
            var rangers = new List<Ranger> { MakeRanger("r1") };
            var enemies = new List<Enemy> { MakeEnemy("e1") };

            var result = ProcessRoundStatusEffects.Execute(rangers, enemies);

            Assert(result.DemorphEvents.Count == 0, "No demorph events expected");
            Assert(result.AggressionEvents.Count == 0, "No aggression events expected");
        }

        private static void Test_RangerDoT_AppliesDamage()
        {
            var ranger = MakeRanger("r1");
            float healthBefore = ranger.UnmorphedHealth.Current;
            // Add a DoT effect
            ranger.StatusEffects.Apply(new StatusEffectInstance(
                "dot_test",
                new TurnStartTrigger(),
                new DamageOverTimeBehavior(5f),
                3));
            var rangers = new List<Ranger> { ranger };
            var enemies = new List<Enemy>();

            ProcessRoundStatusEffects.Execute(rangers, enemies);

            Assert(ranger.UnmorphedHealth.Current < healthBefore,
                $"DoT should reduce health. Before={healthBefore}, After={ranger.UnmorphedHealth.Current}");
        }

        private static void Test_FormDeath_ProducesDemorphEvent()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph(); // Morph into base form
            // Add lethal DoT
            ranger.StatusEffects.Apply(new StatusEffectInstance(
                "lethal_dot",
                new TurnStartTrigger(),
                new DamageOverTimeBehavior(999f),
                3));
            var rangers = new List<Ranger> { ranger };
            var enemies = new List<Enemy>();

            var result = ProcessRoundStatusEffects.Execute(rangers, enemies);

            Assert(result.DemorphEvents.Count == 1,
                $"Expected 1 demorph event, got {result.DemorphEvents.Count}");
            Assert(result.DemorphEvents[0].RangerId == "r1",
                $"Expected r1, got {result.DemorphEvents[0].RangerId}");
        }

        private static void Test_UsesInjectedBricks()
        {
            var ranger = MakeRanger("r1");
            // Add a DoT so processing actually runs
            ranger.StatusEffects.Apply(new StatusEffectInstance(
                "dot_test",
                new TurnStartTrigger(),
                new DamageOverTimeBehavior(5f),
                3));
            var rangers = new List<Ranger> { ranger };
            var enemies = new List<Enemy>();
            bool getPoolCalled = false;
            bool applyEffectCalled = false;

            ProcessRoundStatusEffects.Execute(rangers, enemies,
                getTargetHealthPool: r => { getPoolCalled = true; return r.UnmorphedHealth; },
                applyEffectOutput: (hp, o) => { applyEffectCalled = true; },
                checkFormDeath: r => false);

            Assert(getPoolCalled, "Should call injected getTargetHealthPool");
            Assert(applyEffectCalled, "Should call injected applyEffectOutput");
        }

        private static Ranger MakeRanger(string id)
        {
            return new Ranger(id, id, ElementalType.Blaze,
                new Proclivity(StatType.STR), null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4),
                50f, FormCatalog.BaseForm());
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
