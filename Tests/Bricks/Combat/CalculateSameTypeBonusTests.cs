using System;
using TokuTactics.Bricks.Combat;

namespace TokuTactics.Tests.Bricks.Combat
{
    /// <summary>
    /// Tests for CalculateSameTypeBonus brick.
    /// STAB (Same Type Attack Bonus) increases damage when Ranger and Form types match.
    /// </summary>
    public static class CalculateSameTypeBonusTests
    {
        public static void Run()
        {
            Test_WithBonus_DamageIncreased();
            Test_WithoutBonus_DamageUnchanged();
            Test_StandardMultiplier_1Point25();
            Test_HighMultiplier();
            Console.WriteLine("CalculateSameTypeBonusTests: All passed");
        }

        private static void Test_WithBonus_DamageIncreased()
        {
            // Arrange
            float baseDamage = 100f;
            float stabMultiplier = 1.25f;

            // Act
            float result = CalculateSameTypeBonus.Execute(baseDamage, stabMultiplier);

            // Assert
            Assert.AreEqual(125f, result, "STAB should increase 100 damage to 125 with 1.25x multiplier");
        }

        private static void Test_WithoutBonus_DamageUnchanged()
        {
            // Arrange
            float baseDamage = 100f;
            float stabMultiplier = 1.0f; // No bonus

            // Act
            float result = CalculateSameTypeBonus.Execute(baseDamage, stabMultiplier);

            // Assert
            Assert.AreEqual(100f, result, "No STAB (1.0x) should leave damage unchanged");
        }

        private static void Test_StandardMultiplier_1Point25()
        {
            // Arrange: Test with the standard game multiplier
            float baseDamage = 80f;
            float stabMultiplier = 1.25f;

            // Act
            float result = CalculateSameTypeBonus.Execute(baseDamage, stabMultiplier);

            // Assert
            Assert.AreEqual(100f, result, "80 damage with 1.25x STAB = 100");
        }

        private static void Test_HighMultiplier()
        {
            // Arrange: Test with a hypothetical high multiplier
            float baseDamage = 50f;
            float stabMultiplier = 2.0f;

            // Act
            float result = CalculateSameTypeBonus.Execute(baseDamage, stabMultiplier);

            // Assert
            Assert.AreEqual(100f, result, "50 damage with 2.0x STAB = 100");
        }
    }
}
