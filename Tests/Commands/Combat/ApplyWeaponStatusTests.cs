using System;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Tests.Commands.Combat
{
    public static class ApplyWeaponStatusTests
    {
        public static void Run()
        {
            Test_AppliesStatusToTracker();
            Test_ReturnsEffectId();
            Test_UsesInjectedBricks();
            Console.WriteLine("ApplyWeaponStatusTests: All passed");
        }

        private static void Test_AppliesStatusToTracker()
        {
            var template = MakeTemplate("burn");
            var tracker = new StatusEffectTracker();

            ApplyWeaponStatus.Execute(template, tracker, 10f, 0.01f);

            Assert(tracker.ActiveEffects.Count == 1, $"Expected 1 effect, got {tracker.ActiveEffects.Count}");
        }

        private static void Test_ReturnsEffectId()
        {
            var template = MakeTemplate("poison");
            var tracker = new StatusEffectTracker();

            var id = ApplyWeaponStatus.Execute(template, tracker, 0f, 0.01f);

            Assert(id == "poison", $"Expected 'poison', got '{id}'");
        }

        private static void Test_UsesInjectedBricks()
        {
            var template = MakeTemplate("test");
            var tracker = new StatusEffectTracker();
            bool potencyCalled = false;
            bool applyCalled = false;

            ApplyWeaponStatus.Execute(template, tracker, 5f, 0.01f,
                calculatePotency: (mag, scale) => { potencyCalled = true; return 1.0f; },
                applyEffect: (t, e) => { applyCalled = true; return e.Id; });

            Assert(potencyCalled, "Should call injected calculatePotency");
            Assert(applyCalled, "Should call injected applyEffect");
        }

        private static StatusEffectTemplate MakeTemplate(string id)
        {
            return new StatusEffectTemplate(id,
                new TurnStartTrigger(),
                new DamageOverTimeBehavior(5f),
                3);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
