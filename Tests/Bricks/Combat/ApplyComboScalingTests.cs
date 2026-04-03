using System;
using TokuTactics.Bricks.Combat;

namespace TokuTactics.Tests.Bricks.Combat
{
    /// <summary>
    /// Tests for ApplyComboScaling brick.
    /// Combo system reduces damage on successive hits in a chain.
    /// </summary>
    public static class ApplyComboScalingTests
    {
        public static void Run()
        {
            Test_NoCombo_FullDamage();
            Test_SecondHit_ReducedDamage();
            Test_ThirdHit_FurtherReducedDamage();
            Test_ZeroMultiplier_ZeroDamage();
            Console.WriteLine("ApplyComboScalingTests: All passed");
        }

        private static void Test_NoCombo_FullDamage()
        {
            // Arrange: 1.0x = no combo, first hit
            float baseDamage = 100f;
            float comboMultiplier = 1.0f;

            // Act
            float result = ApplyComboScaling.Execute(baseDamage, comboMultiplier);

            // Assert
            Assert.AreEqual(100f, result, "1.0x combo multiplier should leave damage unchanged");
        }

        private static void Test_SecondHit_ReducedDamage()
        {
            // Arrange: 0.8x = second hit in combo
            float baseDamage = 100f;
            float comboMultiplier = 0.8f;

            // Act
            float result = ApplyComboScaling.Execute(baseDamage, comboMultiplier);

            // Assert
            Assert.AreEqual(80f, result, "0.8x combo multiplier should reduce 100 damage to 80");
        }

        private static void Test_ThirdHit_FurtherReducedDamage()
        {
            // Arrange: 0.6x = third hit in combo
            float baseDamage = 100f;
            float comboMultiplier = 0.6f;

            // Act
            float result = ApplyComboScaling.Execute(baseDamage, comboMultiplier);

            // Assert
            Assert.AreEqual(60f, result, "0.6x combo multiplier should reduce 100 damage to 60");
        }

        private static void Test_ZeroMultiplier_ZeroDamage()
        {
            // Arrange: Edge case - zero multiplier
            float baseDamage = 100f;
            float comboMultiplier = 0f;

            // Act
            float result = ApplyComboScaling.Execute(baseDamage, comboMultiplier);

            // Assert
            Assert.AreEqual(0f, result, "0x combo multiplier should result in zero damage");
        }
    }
}
