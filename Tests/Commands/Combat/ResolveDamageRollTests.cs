using System;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Types;

namespace TokuTactics.Tests.Commands.Combat
{
    /// <summary>
    /// Tests for ResolveDamageRoll command.
    /// Tests the full orchestration flow with real bricks.
    /// Uses deterministic RNG to verify correct behavior.
    /// </summary>
    public static class ResolveDamageRollTests
    {
        public static void Run()
        {
            Test_Dodge_ReturnsZeroDamage();
            Test_NoDodge_NoCrit_CalculatesCorrectDamage();
            Test_WithCrit_AppliesCritMultiplier();
            Test_WithSTAB_AppliesSameTypeBonus();
            Test_WithoutSTAB_SkipsSameTypeBonus();
            Test_TypeMatchup_AppliesCorrectly();
            Test_ComboScaling_AppliesCorrectly();
            Test_MinimumDamage_IsOne();
            Console.WriteLine("ResolveDamageRollTests: All passed");
        }

        private static void Test_Dodge_ReturnsZeroDamage()
        {
            // Arrange: High dodge chance to guarantee dodge
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 1.0f, // 100% dodge
                LckDodgeScale = 0f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 10f,
                AttackerLck = 0f,
                DefenderDef = 5f,
                DefenderLck = 0f,
                ActionPower = 1f,
                AttackerDualType = new DualType(ElementalType.Blaze, ElementalType.Blaze),
                DefenderType = ElementalType.Normal,
                DefenderDualType = null,
                ComboMultiplier = 1f,
                HasSameTypeBonus = false
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert
            Assert.IsTrue(result.WasDodged, "Attack should be dodged");
            Assert.AreEqual(0, result.FinalDamage, "Dodged attacks deal 0 damage");
        }

        private static void Test_NoDodge_NoCrit_CalculatesCorrectDamage()
        {
            // Arrange: No dodge, no crit, neutral matchup
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 0f,
                BaseCrit = 0f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 10f,
                AttackerLck = 0f,
                DefenderDef = 5f,
                DefenderLck = 0f,
                ActionPower = 2f,
                AttackerDualType = new DualType(ElementalType.Normal, ElementalType.Normal),
                DefenderType = ElementalType.Normal,
                DefenderDualType = null,
                ComboMultiplier = 1f,
                HasSameTypeBonus = false
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert: (10 - 5) * 2 = 10 damage
            Assert.IsFalse(result.WasDodged, "Should not dodge");
            Assert.IsFalse(result.WasCritical, "Should not crit");
            Assert.AreEqual(10, result.FinalDamage, "Should deal 10 damage");
            Assert.AreEqual(MatchupResult.Neutral, result.Matchup, "Should be neutral matchup");
        }

        private static void Test_WithCrit_AppliesCritMultiplier()
        {
            // Arrange: Guaranteed crit
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 0f,
                BaseCrit = 1.0f, // 100% crit
                LckCritScale = 0f,
                CritMultiplier = 1.5f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 10f,
                AttackerLck = 0f,
                DefenderDef = 5f,
                DefenderLck = 0f,
                ActionPower = 2f,
                AttackerDualType = new DualType(ElementalType.Normal, ElementalType.Normal),
                DefenderType = ElementalType.Normal,
                DefenderDualType = null,
                ComboMultiplier = 1f,
                HasSameTypeBonus = false
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert: (10 - 5) * 2 = 10, then * 1.5 crit = 15
            Assert.IsTrue(result.WasCritical, "Should crit");
            Assert.AreEqual(15, result.FinalDamage, "Should deal 15 damage with crit");
        }

        private static void Test_WithSTAB_AppliesSameTypeBonus()
        {
            // Arrange: With STAB
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 0f,
                BaseCrit = 0f,
                SameTypeBonus = 1.25f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 10f,
                AttackerLck = 0f,
                DefenderDef = 5f,
                DefenderLck = 0f,
                ActionPower = 2f,
                AttackerDualType = new DualType(ElementalType.Blaze, ElementalType.Blaze),
                DefenderType = ElementalType.Normal,
                DefenderDualType = null,
                ComboMultiplier = 1f,
                HasSameTypeBonus = true
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert: (10 - 5) * 2 = 10, then * 1.25 STAB = 12.5, rounded = 13
            Assert.IsTrue(result.HadSameTypeBonus, "Should have STAB");
            Assert.AreEqual(13, result.FinalDamage, "Should deal 13 damage with STAB (12.5 rounded up)");
        }

        private static void Test_WithoutSTAB_SkipsSameTypeBonus()
        {
            // Arrange: No STAB
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 0f,
                BaseCrit = 0f,
                SameTypeBonus = 1.25f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 10f,
                AttackerLck = 0f,
                DefenderDef = 5f,
                DefenderLck = 0f,
                ActionPower = 2f,
                AttackerDualType = new DualType(ElementalType.Blaze, ElementalType.Blaze),
                DefenderType = ElementalType.Normal,
                DefenderDualType = null,
                ComboMultiplier = 1f,
                HasSameTypeBonus = false
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert: (10 - 5) * 2 = 10, no STAB
            Assert.IsFalse(result.HadSameTypeBonus, "Should not have STAB");
            Assert.AreEqual(10, result.FinalDamage, "Should deal 10 damage without STAB");
        }

        private static void Test_TypeMatchup_AppliesCorrectly()
        {
            // Arrange: Strong matchup (Blaze vs Frost)
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 0f,
                BaseCrit = 0f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 10f,
                AttackerLck = 0f,
                DefenderDef = 5f,
                DefenderLck = 0f,
                ActionPower = 2f,
                AttackerDualType = new DualType(ElementalType.Blaze, ElementalType.Blaze),
                DefenderType = ElementalType.Frost,
                DefenderDualType = null,
                ComboMultiplier = 1f,
                HasSameTypeBonus = false
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert: (10 - 5) * 2 = 10, then * 1.5 type = 15
            Assert.AreEqual(MatchupResult.Strong, result.Matchup, "Should be strong matchup");
            Assert.AreEqual(1.5f, result.TypeMultiplier, "Type multiplier should be 1.5");
            Assert.AreEqual(15, result.FinalDamage, "Should deal 15 damage with strong matchup");
        }

        private static void Test_ComboScaling_AppliesCorrectly()
        {
            // Arrange: Combo scaling
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 0f,
                BaseCrit = 0f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 10f,
                AttackerLck = 0f,
                DefenderDef = 5f,
                DefenderLck = 0f,
                ActionPower = 2f,
                AttackerDualType = new DualType(ElementalType.Normal, ElementalType.Normal),
                DefenderType = ElementalType.Normal,
                DefenderDualType = null,
                ComboMultiplier = 0.8f, // 80% damage for combo
                HasSameTypeBonus = false
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert: (10 - 5) * 2 = 10, then * 0.8 combo = 8
            Assert.AreEqual(0.8f, result.ComboMultiplier, "Combo multiplier should be 0.8");
            Assert.AreEqual(8, result.FinalDamage, "Should deal 8 damage with combo scaling");
        }

        private static void Test_MinimumDamage_IsOne()
        {
            // Arrange: Very weak attack
            var typeChart = BuildTestChart();
            var constants = new TunableConstants
            {
                BaseDodge = 0f,
                BaseCrit = 0f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };
            var @params = new ResolveDamageRollParams
            {
                AttackerStr = 1f,
                AttackerLck = 0f,
                DefenderDef = 10f,
                DefenderLck = 0f,
                ActionPower = 0.1f,
                AttackerDualType = new DualType(ElementalType.Blaze, ElementalType.Blaze),
                DefenderType = ElementalType.Torrent, // Weak matchup
                DefenderDualType = null,
                ComboMultiplier = 0.5f,
                HasSameTypeBonus = false
            };

            // Act
            var result = ResolveDamageRoll.Execute(@params, typeChart, new Random(), constants);

            // Assert: Even tiny damage should be at least 1
            Assert.AreEqual(1, result.FinalDamage, "Minimum damage should be 1");
        }

        private static TypeChart BuildTestChart()
        {
            var chart = new TypeChart();
            chart.AddStrength(ElementalType.Blaze, ElementalType.Frost);
            chart.AddStrength(ElementalType.Frost, ElementalType.Torrent);
            chart.AddStrength(ElementalType.Torrent, ElementalType.Blaze);
            return chart;
        }
    }

    internal static class Assert
    {
        public static void AreEqual(int expected, int actual, string message)
        {
            if (expected != actual)
            {
                throw new Exception($"FAIL: {message} | Expected: {expected}, Actual: {actual}");
            }
        }

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
