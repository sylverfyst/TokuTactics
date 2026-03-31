using TokuTactics.Core.Stats;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Tests.Core.StatusEffect
{
    public class StatusEffectTests
    {
        // === StatusEffectInstance Tests ===

        public void Effect_TicksDownCorrectly()
        {
            var effect = new StatusEffectInstance(
                "test_dot", new TurnStartTrigger(), new DamageOverTimeBehavior(10f), 3);

            Assert(effect.RemainingDuration == 3, "Should start at 3");
            Assert(!effect.IsExpired, "Should not be expired");

            effect.Tick();
            Assert(effect.RemainingDuration == 2, "Should be 2 after tick");

            effect.Tick();
            effect.Tick();
            Assert(effect.RemainingDuration == 0, "Should be 0 after 3 ticks");
            Assert(effect.IsExpired, "Should be expired");
        }

        public void Effect_Process_ReturnsOutput_OnCorrectPhase()
        {
            var effect = new StatusEffectInstance(
                "test", new TurnStartTrigger(), new DamageOverTimeBehavior(10f), 3);

            var wrongContext = new EffectContext { Phase = "move" };
            var wrongResult = effect.Process(wrongContext);
            Assert(wrongResult == null, "Wrong phase should return null");

            var rightContext = new EffectContext { Phase = "turn_start", SourceMag = 5f };
            var rightResult = effect.Process(rightContext);
            Assert(rightResult != null, "Right phase should return output");
            Assert(rightResult.Damage > 0, "DoT should produce damage");
        }

        public void Effect_Process_ReturnsNull_WhenExpired()
        {
            var effect = new StatusEffectInstance(
                "test", new TurnStartTrigger(), new DamageOverTimeBehavior(10f), 1);
            effect.Tick(); // Expire it

            var context = new EffectContext { Phase = "turn_start" };
            var result = effect.Process(context);

            Assert(result == null, "Expired effect should return null");
        }

        public void Effect_InstantTrigger_FiresOnlyOnce()
        {
            var trigger = new InstantTrigger();

            var context1 = new EffectContext { Phase = "any" };
            Assert(trigger.ShouldTrigger(context1), "Should fire first time");

            var context2 = new EffectContext { Phase = "any" };
            Assert(!trigger.ShouldTrigger(context2), "Should NOT fire second time");
        }

        public void Effect_Potency_ScalesOutput()
        {
            var lowPotency = new StatusEffectInstance(
                "low", new TurnStartTrigger(), new DamageOverTimeBehavior(10f), 3, potency: 1.0f);
            var highPotency = new StatusEffectInstance(
                "high", new TurnStartTrigger(), new DamageOverTimeBehavior(10f), 3, potency: 2.0f);

            var context = new EffectContext { Phase = "turn_start", SourceMag = 0f };
            var lowOutput = lowPotency.Process(context);

            context = new EffectContext { Phase = "turn_start", SourceMag = 0f };
            var highOutput = highPotency.Process(context);

            Assert(highOutput.Damage > lowOutput.Damage, "Higher potency should deal more damage");
        }

        // === Behavior Output Tests ===

        public void StatModifierBehavior_ProducesStatOutput()
        {
            var behavior = new StatModifierBehavior(StatType.DEF, -5f);
            var context = new EffectContext { PotencyMultiplier = 1.0f };

            var output = behavior.GetOutput(context);

            Assert(output.StatModifiers != null, "Should have stat modifiers");
            Assert(output.StatModifiers[StatType.DEF] == -5f, "Should be -5 DEF");
        }

        public void StatModifierBehavior_RemovalUndoesModifier()
        {
            var behavior = new StatModifierBehavior(StatType.DEF, -5f);
            var context = new EffectContext { PotencyMultiplier = 1.0f };

            var removal = behavior.GetRemovalOutput(context);

            Assert(removal.StatModifiers != null, "Should have removal modifiers");
            Assert(removal.StatModifiers[StatType.DEF] == 5f, "Should undo the -5 DEF");
        }

        public void DamageOverTimeBehavior_ProducesDamage()
        {
            var behavior = new DamageOverTimeBehavior(10f);
            var context = new EffectContext { PotencyMultiplier = 1.5f, SourceMag = 10f };

            var output = behavior.GetOutput(context);

            Assert(output.Damage > 0, "Should produce damage");
            Assert(output.Damage > 10f, "Should be scaled by potency and MAG");
        }

        public void StunBehavior_PreventsAction()
        {
            var behavior = new StunBehavior();
            var context = new EffectContext();

            var output = behavior.GetOutput(context);

            Assert(output.PreventsAction, "Should prevent action");
        }

        public void MovementReductionBehavior_ReducesMovement()
        {
            var behavior = new MovementReductionBehavior(0.5f);
            var context = new EffectContext { PotencyMultiplier = 1.0f };

            var output = behavior.GetOutput(context);

            Assert(output.MovementMultiplier < 1.0f, "Should reduce movement");
        }

        public void HealOverTimeBehavior_ProducesHealing()
        {
            var behavior = new HealOverTimeBehavior(15f);
            var context = new EffectContext { PotencyMultiplier = 1.0f, SourceMag = 0f };

            var output = behavior.GetOutput(context);

            Assert(output.Healing > 0, "Should produce healing");
        }

        // === StatusEffectTracker Tests ===

        public void Tracker_Apply_AddsEffect()
        {
            var tracker = new StatusEffectTracker();
            var effect = new StatusEffectInstance(
                "poison", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 3);

            tracker.Apply(effect);

            Assert(tracker.ActiveEffects.Count == 1, "Should have 1 effect");
            Assert(tracker.HasEffect("poison"), "Should find poison by ID");
        }

        public void Tracker_Process_ReturnsOutputs()
        {
            var tracker = new StatusEffectTracker();
            tracker.Apply(new StatusEffectInstance(
                "dot", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 3));

            var context = new EffectContext { Phase = "turn_start", SourceMag = 0f };
            var outputs = tracker.Process(context);

            Assert(outputs.Count == 1, "Should have 1 output");
            Assert(outputs[0].Damage > 0, "Output should have damage");
        }

        public void Tracker_TickAndClean_RemovesExpired_ReturnsRemovalOutputs()
        {
            var tracker = new StatusEffectTracker();
            tracker.Apply(new StatusEffectInstance(
                "short_mod", new InstantTrigger(), new StatModifierBehavior(StatType.DEF, -5f), 1));
            tracker.Apply(new StatusEffectInstance(
                "long_dot", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 3));

            var context = new EffectContext { PotencyMultiplier = 1.0f };
            var removalOutputs = tracker.TickAndClean(context);

            Assert(tracker.ActiveEffects.Count == 1, "Short effect should be removed");
            Assert(!tracker.HasEffect("short_mod"), "Short should be gone");
            Assert(tracker.HasEffect("long_dot"), "Long should remain");
            Assert(removalOutputs.Count == 1, "Should have 1 removal output (stat modifier undo)");
        }

        public void Tracker_Remove_ReturnsRemovalOutput()
        {
            var tracker = new StatusEffectTracker();
            tracker.Apply(new StatusEffectInstance(
                "def_break", new InstantTrigger(), new StatModifierBehavior(StatType.DEF, -5f), 5));

            var context = new EffectContext { PotencyMultiplier = 1.0f };
            var output = tracker.Remove("def_break", context);

            Assert(!tracker.HasEffect("def_break"), "Should be removed");
            Assert(output != null, "Should return removal output");
            Assert(output.StatModifiers != null, "Should have stat undo");
        }

        public void Tracker_Remove_ReturnsNull_ForMissing()
        {
            var tracker = new StatusEffectTracker();
            var context = new EffectContext();

            var output = tracker.Remove("nonexistent", context);

            Assert(output == null, "Should return null for missing effect");
        }

        public void Tracker_ClearAll_ReturnsAllRemovalOutputs()
        {
            var tracker = new StatusEffectTracker();
            tracker.Apply(new StatusEffectInstance(
                "mod_a", new InstantTrigger(), new StatModifierBehavior(StatType.STR, 5f), 5));
            tracker.Apply(new StatusEffectInstance(
                "mod_b", new InstantTrigger(), new StatModifierBehavior(StatType.DEF, -3f), 5));
            tracker.Apply(new StatusEffectInstance(
                "dot", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 5));

            var context = new EffectContext { PotencyMultiplier = 1.0f };
            var outputs = tracker.ClearAll(context);

            Assert(tracker.ActiveEffects.Count == 0, "Should have no effects");
            Assert(outputs.Count == 2, "Should have 2 removal outputs (stat mods, not DoT)");
        }

        public void Tracker_MultipleTickCycles_ExpireAtDifferentTimes()
        {
            var tracker = new StatusEffectTracker();
            tracker.Apply(new StatusEffectInstance(
                "dur1", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 1));
            tracker.Apply(new StatusEffectInstance(
                "dur2", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 2));
            tracker.Apply(new StatusEffectInstance(
                "dur3", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 3));

            var context = new EffectContext();

            tracker.TickAndClean(context);
            Assert(tracker.ActiveEffects.Count == 2, "dur1 expired after turn 1");

            tracker.TickAndClean(context);
            Assert(tracker.ActiveEffects.Count == 1, "dur2 expired after turn 2");

            tracker.TickAndClean(context);
            Assert(tracker.ActiveEffects.Count == 0, "dur3 expired after turn 3");
        }

        // === CreateFresh Independence ===

        public void InstantTrigger_CreateFresh_IndependentState()
        {
            var original = new InstantTrigger();

            // Fire the original
            var context1 = new EffectContext { Phase = "any" };
            Assert(original.ShouldTrigger(context1), "Original should fire first time");
            Assert(!original.ShouldTrigger(context1), "Original should not fire second time");

            // Create a fresh copy — should have independent state
            var fresh = original.CreateFresh();
            var context2 = new EffectContext { Phase = "any" };
            Assert(fresh.ShouldTrigger(context2),
                "Fresh copy should fire independently — not blocked by original");
        }

        public void StatusEffectTemplate_CreateInstance_FreshTrigger()
        {
            var template = new StatusEffectTemplate(
                "eff_test", new InstantTrigger(), new StunBehavior(), 1);

            // Create first instance and fire its trigger
            var instance1 = template.CreateInstance(1.0f);
            var context1 = new EffectContext { Phase = "any" };
            var output1 = instance1.Process(context1);
            Assert(output1 != null, "First instance should fire");

            // Second process on same instance should not fire (one-shot)
            var output1b = instance1.Process(new EffectContext { Phase = "any" });
            Assert(output1b == null, "Same instance should not fire twice");

            // Create second instance from same template — should fire independently
            var instance2 = template.CreateInstance(1.0f);
            var context2 = new EffectContext { Phase = "any" };
            var output2 = instance2.Process(context2);
            Assert(output2 != null,
                "Second instance should fire independently — not blocked by first");
        }

        public void TurnStartTrigger_CreateFresh_ReturnsSelf()
        {
            var trigger = new TurnStartTrigger();
            var fresh = trigger.CreateFresh();

            Assert(ReferenceEquals(trigger, fresh),
                "Stateless trigger should return this from CreateFresh");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new StatusEffectTests();
            tests.Effect_TicksDownCorrectly();
            tests.Effect_Process_ReturnsOutput_OnCorrectPhase();
            tests.Effect_Process_ReturnsNull_WhenExpired();
            tests.Effect_InstantTrigger_FiresOnlyOnce();
            tests.Effect_Potency_ScalesOutput();
            tests.StatModifierBehavior_ProducesStatOutput();
            tests.StatModifierBehavior_RemovalUndoesModifier();
            tests.DamageOverTimeBehavior_ProducesDamage();
            tests.StunBehavior_PreventsAction();
            tests.MovementReductionBehavior_ReducesMovement();
            tests.HealOverTimeBehavior_ProducesHealing();
            tests.Tracker_Apply_AddsEffect();
            tests.Tracker_Process_ReturnsOutputs();
            tests.Tracker_TickAndClean_RemovesExpired_ReturnsRemovalOutputs();
            tests.Tracker_Remove_ReturnsRemovalOutput();
            tests.Tracker_Remove_ReturnsNull_ForMissing();
            tests.Tracker_ClearAll_ReturnsAllRemovalOutputs();
            tests.Tracker_MultipleTickCycles_ExpireAtDifferentTimes();
            tests.InstantTrigger_CreateFresh_IndependentState();
            tests.StatusEffectTemplate_CreateInstance_FreshTrigger();
            tests.TurnStartTrigger_CreateFresh_ReturnsSelf();
            System.Console.WriteLine("StatusEffectTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
