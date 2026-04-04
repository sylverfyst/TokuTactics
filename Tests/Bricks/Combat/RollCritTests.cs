using System;
using TokuTactics.Bricks.Combat;

namespace TokuTactics.Tests.Bricks.Combat
{
    /// <summary>
    /// Tests for RollCrit brick.
    /// Tests use deterministic RNG (seeded Random) to verify crit mechanics.
    /// </summary>
    public static class RollCritTests
    {
        public static void Run()
        {
            Test_AlwaysCrit_WhenRngLowerThanChance();
            Test_NeverCrit_WhenRngHigherThanChance();
            Test_LckScaling_IncreasesCritChance();
            Test_ZeroLck_UsesBaseChanceOnly();
            Console.WriteLine("RollCritTests: All passed");
        }

        private static void Test_AlwaysCrit_WhenRngLowerThanChance()
        {
            // Arrange: RNG that returns 0.04, crit chance 0.05
            var rng = new DeterministicRandom(0.04);
            float attackerLck = 0f;
            float baseCritChance = 0.05f;
            float lckScale = 0.005f;

            // Act
            bool result = RollCrit.Execute(attackerLck, baseCritChance, lckScale, rng);

            // Assert
            Assert.IsTrue(result, "Should crit when RNG (0.04) < crit chance (0.05)");
        }

        private static void Test_NeverCrit_WhenRngHigherThanChance()
        {
            // Arrange: RNG that returns 0.06, crit chance 0.05
            var rng = new DeterministicRandom(0.06);
            float attackerLck = 0f;
            float baseCritChance = 0.05f;
            float lckScale = 0.005f;

            // Act
            bool result = RollCrit.Execute(attackerLck, baseCritChance, lckScale, rng);

            // Assert
            Assert.IsFalse(result, "Should not crit when RNG (0.06) >= crit chance (0.05)");
        }

        private static void Test_LckScaling_IncreasesCritChance()
        {
            // Arrange: RNG at 0.10, base crit 0.05, LCK 10 with scale 0.005 = 0.05 + 0.05 = 0.10
            var rng = new DeterministicRandom(0.099); // Just under total chance
            float attackerLck = 10f;
            float baseCritChance = 0.05f;
            float lckScale = 0.005f;
            // Total crit chance = 0.05 + (10 * 0.005) = 0.10

            // Act
            bool result = RollCrit.Execute(attackerLck, baseCritChance, lckScale, rng);

            // Assert
            Assert.IsTrue(result, "Should crit with LCK scaling: 0.099 < 0.10");
        }

        private static void Test_ZeroLck_UsesBaseChanceOnly()
        {
            // Arrange
            var rng = new DeterministicRandom(0.049); // Just under base chance
            float attackerLck = 0f;
            float baseCritChance = 0.05f;
            float lckScale = 0.005f;

            // Act
            bool result = RollCrit.Execute(attackerLck, baseCritChance, lckScale, rng);

            // Assert
            Assert.IsTrue(result, "Should crit with zero LCK using only base chance");
        }
    }
}
