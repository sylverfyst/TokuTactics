using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class ApplyStatusEffectTests
    {
        public static void Run()
        {
            Test_AppliesEffectToTracker();
            Test_ReturnsEffectId();
            Console.WriteLine("ApplyStatusEffectTests: All passed");
        }

        private static void Test_AppliesEffectToTracker()
        {
            var tracker = new StatusEffectTracker();
            var effect = new StatusEffectInstance(
                "burn_test", new TurnStartTrigger(),
                new DamageOverTimeBehavior(5f), 3);

            ApplyStatusEffect.Execute(tracker, effect);

            Assert(tracker.ActiveEffects.Count == 1, $"Expected 1 effect, got {tracker.ActiveEffects.Count}");
        }

        private static void Test_ReturnsEffectId()
        {
            var tracker = new StatusEffectTracker();
            var effect = new StatusEffectInstance(
                "poison_test", new TurnStartTrigger(),
                new DamageOverTimeBehavior(3f), 2);

            var id = ApplyStatusEffect.Execute(tracker, effect);

            Assert(id == "poison_test", $"Expected 'poison_test', got '{id}'");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
