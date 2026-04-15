using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.Health;
using TokuTactics.Core.StatusEffect;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class ApplyEffectOutputToHealthTests
    {
        public static void Run()
        {
            Test_AppliesDamage();
            Test_AppliesHealing();
            Test_AppliesBothDamageAndHealing();
            Test_ZeroEffectDoesNothing();
            Test_HealingCappedAtMax();
            Console.WriteLine("ApplyEffectOutputToHealthTests: All passed");
        }

        private static void Test_AppliesDamage()
        {
            var hp = new HealthPool(100f);
            var output = new EffectOutput { Damage = 30f };

            ApplyEffectOutputToHealth.Execute(hp, output);

            Assert(hp.Current == 70f, $"Expected 70, got {hp.Current}");
        }

        private static void Test_AppliesHealing()
        {
            var hp = new HealthPool(100f);
            hp.TakeDamage(50f);
            var output = new EffectOutput { Healing = 20f };

            ApplyEffectOutputToHealth.Execute(hp, output);

            Assert(hp.Current == 70f, $"Expected 70, got {hp.Current}");
        }

        private static void Test_AppliesBothDamageAndHealing()
        {
            var hp = new HealthPool(100f);
            var output = new EffectOutput { Damage = 40f, Healing = 10f };

            ApplyEffectOutputToHealth.Execute(hp, output);

            // Damage applied first (40 -> 60), then heal (60 -> 70)
            Assert(hp.Current == 70f, $"Expected 70, got {hp.Current}");
        }

        private static void Test_ZeroEffectDoesNothing()
        {
            var hp = new HealthPool(100f);
            var output = new EffectOutput();

            ApplyEffectOutputToHealth.Execute(hp, output);

            Assert(hp.Current == 100f, $"Expected 100, got {hp.Current}");
        }

        private static void Test_HealingCappedAtMax()
        {
            var hp = new HealthPool(100f);
            hp.TakeDamage(10f);
            var output = new EffectOutput { Healing = 50f };

            ApplyEffectOutputToHealth.Execute(hp, output);

            Assert(hp.Current == 100f, $"Expected 100 (capped), got {hp.Current}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
