using System;
using TokuTactics.Bricks.Combat;

namespace TokuTactics.Tests.Bricks.Combat
{
    /// <summary>
    /// Tests for CalculateBaseDamage brick.
    /// Formula: (attackerStr / defenderDef) * actionPower
    /// </summary>
    public static class CalculateBaseDamageTests
    {
        public static void Run()
        {
            Test_NormalCalculation();
            Test_VeryHighStrength();
            Test_VeryLowDefense();
            Test_ZeroPower();
            Console.WriteLine("CalculateBaseDamageTests: All passed");
        }

        private static void Test_NormalCalculation()
        {
            // Arrange
            float attackerStr = 10f;
            float defenderDef = 5f;
            float actionPower = 20f;

            // Act
            float result = CalculateBaseDamage.Execute(attackerStr, defenderDef, actionPower);

            // Assert
            Assert.AreEqual(40f, result, "STR 10 / DEF 5 * Power 20 = 40");
        }

        private static void Test_VeryHighStrength()
        {
            // Arrange
            float attackerStr = 100f;
            float defenderDef = 10f;
            float actionPower = 10f;

            // Act
            float result = CalculateBaseDamage.Execute(attackerStr, defenderDef, actionPower);

            // Assert
            Assert.AreEqual(100f, result, "STR 100 / DEF 10 * Power 10 = 100");
        }

        private static void Test_VeryLowDefense()
        {
            // Arrange
            float attackerStr = 20f;
            float defenderDef = 0.1f; // Very low, but not zero
            float actionPower = 5f;

            // Act
            float result = CalculateBaseDamage.Execute(attackerStr, defenderDef, actionPower);

            // Assert
            // Should not crash or produce infinity
            Assert.IsTrue(result > 0 && !float.IsInfinity(result),
                "Very low DEF should still produce finite positive result");
        }

        private static void Test_ZeroPower()
        {
            // Arrange
            float attackerStr = 20f;
            float defenderDef = 10f;
            float actionPower = 0f;

            // Act
            float result = CalculateBaseDamage.Execute(attackerStr, defenderDef, actionPower);

            // Assert
            Assert.AreEqual(0f, result, "Zero power should result in zero damage");
        }
    }

    internal static class Assert
    {
        public static void AreEqual(float expected, float actual, string message)
        {
            const float epsilon = 0.001f;
            if (Math.Abs(expected - actual) > epsilon)
            {
                throw new Exception($"FAIL: {message} | Expected: {expected}, Actual: {actual}");
            }
        }

        public static void AreEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"FAIL: {message} | Expected: {expected}, Actual: {actual}");
            }
        }

        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"FAIL: {message}");
            }
        }

        public static void IsFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new Exception($"FAIL: {message}");
            }
        }
    }
}
