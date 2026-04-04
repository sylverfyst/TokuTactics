using System;
using TokuTactics.Bricks.Combat;

namespace TokuTactics.Tests.Bricks.Combat
{
    /// <summary>
    /// Tests for RollDodge brick.
    /// Tests use deterministic RNG (seeded Random) to verify dodge mechanics.
    /// </summary>
    public static class RollDodgeTests
    {
        public static void Run()
        {
            Test_AlwaysDodge_WhenRngLowerThanChance();
            Test_NeverDodge_WhenRngHigherThanChance();
            Test_LckScaling_IncreasesDodgeChance();
            Test_ZeroLck_UsesBaseChanceOnly();
            Console.WriteLine("RollDodgeTests: All passed");
        }

        private static void Test_AlwaysDodge_WhenRngLowerThanChance()
        {
            // Arrange: RNG that returns 0.01, dodge chance 0.02
            var rng = new DeterministicRandom(0.01);
            float defenderLck = 0f;
            float baseDodgeChance = 0.02f;
            float lckScale = 0.003f;

            // Act
            bool result = RollDodge.Execute(defenderLck, baseDodgeChance, lckScale, rng);

            // Assert
            Assert.IsTrue(result, "Should dodge when RNG (0.01) < dodge chance (0.02)");
        }

        private static void Test_NeverDodge_WhenRngHigherThanChance()
        {
            // Arrange: RNG that returns 0.03, dodge chance 0.02
            var rng = new DeterministicRandom(0.03);
            float defenderLck = 0f;
            float baseDodgeChance = 0.02f;
            float lckScale = 0.003f;

            // Act
            bool result = RollDodge.Execute(defenderLck, baseDodgeChance, lckScale, rng);

            // Assert
            Assert.IsFalse(result, "Should not dodge when RNG (0.03) >= dodge chance (0.02)");
        }

        private static void Test_LckScaling_IncreasesDodgeChance()
        {
            // Arrange: RNG at 0.05, base dodge 0.02, LCK 10 with scale 0.003 = 0.02 + 0.03 = 0.05
            var rng = new DeterministicRandom(0.049); // Just under total chance
            float defenderLck = 10f;
            float baseDodgeChance = 0.02f;
            float lckScale = 0.003f;
            // Total dodge chance = 0.02 + (10 * 0.003) = 0.05

            // Act
            bool result = RollDodge.Execute(defenderLck, baseDodgeChance, lckScale, rng);

            // Assert
            Assert.IsTrue(result, "Should dodge with LCK scaling: 0.049 < 0.05");
        }

        private static void Test_ZeroLck_UsesBaseChanceOnly()
        {
            // Arrange
            var rng = new DeterministicRandom(0.019); // Just under base chance
            float defenderLck = 0f;
            float baseDodgeChance = 0.02f;
            float lckScale = 0.003f;

            // Act
            bool result = RollDodge.Execute(defenderLck, baseDodgeChance, lckScale, rng);

            // Assert
            Assert.IsTrue(result, "Should dodge with zero LCK using only base chance");
        }
    }

    /// <summary>
    /// Deterministic Random implementation for testing.
    /// Always returns the same value from NextDouble().
    /// </summary>
    internal class DeterministicRandom : Random
    {
        private readonly double _fixedValue;

        public DeterministicRandom(double fixedValue)
        {
            _fixedValue = fixedValue;
        }

        public override double NextDouble()
        {
            return _fixedValue;
        }
    }
}
