using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Types;

namespace TokuTactics.Tests.Bricks.Combat
{
    /// <summary>
    /// Tests for ApplyTypeMatchup brick.
    /// Tests type effectiveness multipliers against single and dual-type defenders.
    /// </summary>
    public static class ApplyTypeMatchupTests
    {
        public static void Run()
        {
            Test_StrongMatchup_IncreasedDamage();
            Test_WeakMatchup_ReducedDamage();
            Test_NeutralMatchup_UnchangedDamage();
            Test_DualTypeDefender_BothTypesChecked();
            Test_DoubleStrongMatchup_HighestMultiplier();
            Test_DoubleWeakMatchup_LowestMultiplier();
            Console.WriteLine("ApplyTypeMatchupTests: All passed");
        }

        private static TypeChart BuildTestChart()
        {
            var chart = new TypeChart();
            chart.AddStrength(ElementalType.Blaze, ElementalType.Frost);
            chart.AddStrength(ElementalType.Frost, ElementalType.Torrent);
            chart.AddStrength(ElementalType.Torrent, ElementalType.Blaze);
            return chart;
        }

        private static void Test_StrongMatchup_IncreasedDamage()
        {
            // Arrange
            var chart = BuildTestChart();
            float baseDamage = 100f;
            ElementalType attackType = ElementalType.Blaze;
            ElementalType defenderType = ElementalType.Frost;
            DualType? defenderDualType = null; // Single-type defender
            float strongMultiplier = 1.5f;

            // Act
            var result = ApplyTypeMatchup.Execute(
                baseDamage, attackType, defenderType, defenderDualType,
                chart, strongMultiplier, 0.5f, 2.0f, 0.25f);

            // Assert
            Assert.AreEqual(150f, result.Damage, "Blaze vs Frost should be 1.5x (strong)");
            Assert.AreEqual(MatchupResult.Strong, result.Result, "Matchup should be Strong");
            Assert.AreEqual(1.5f, result.Multiplier, "Multiplier should be 1.5");
        }

        private static void Test_WeakMatchup_ReducedDamage()
        {
            // Arrange
            var chart = BuildTestChart();
            float baseDamage = 100f;
            ElementalType attackType = ElementalType.Blaze;
            ElementalType defenderType = ElementalType.Torrent;
            DualType? defenderDualType = null;
            float weakMultiplier = 0.5f;

            // Act
            var result = ApplyTypeMatchup.Execute(
                baseDamage, attackType, defenderType, defenderDualType,
                chart, 1.5f, weakMultiplier, 2.0f, 0.25f);

            // Assert
            Assert.AreEqual(50f, result.Damage, "Blaze vs Torrent should be 0.5x (weak)");
            Assert.AreEqual(MatchupResult.Weak, result.Result, "Matchup should be Weak");
            Assert.AreEqual(0.5f, result.Multiplier, "Multiplier should be 0.5");
        }

        private static void Test_NeutralMatchup_UnchangedDamage()
        {
            // Arrange
            var chart = BuildTestChart();
            float baseDamage = 100f;
            ElementalType attackType = ElementalType.Shadow; // Not in chart
            ElementalType defenderType = ElementalType.Radiant; // Not in chart
            DualType? defenderDualType = null;

            // Act
            var result = ApplyTypeMatchup.Execute(
                baseDamage, attackType, defenderType, defenderDualType,
                chart, 1.5f, 0.5f, 2.0f, 0.25f);

            // Assert
            Assert.AreEqual(100f, result.Damage, "Shadow vs Radiant should be 1.0x (neutral)");
            Assert.AreEqual(MatchupResult.Neutral, result.Result, "Matchup should be Neutral");
            Assert.AreEqual(1.0f, result.Multiplier, "Multiplier should be 1.0");
        }

        private static void Test_DualTypeDefender_BothTypesChecked()
        {
            // Arrange
            var chart = BuildTestChart();
            float baseDamage = 100f;
            ElementalType attackType = ElementalType.Blaze;
            ElementalType defenderType = ElementalType.Normal; // Not used when dual type present
            DualType? defenderDualType = new DualType(ElementalType.Frost, ElementalType.Torrent);
            // Blaze is strong vs Frost (+1), weak vs Torrent (-1) = Neutral (0)

            // Act
            var result = ApplyTypeMatchup.Execute(
                baseDamage, attackType, defenderType, defenderDualType,
                chart, 1.5f, 0.5f, 2.0f, 0.25f);

            // Assert
            Assert.AreEqual(100f, result.Damage, "Blaze vs Frost/Torrent should cancel to neutral");
            Assert.AreEqual(MatchupResult.Neutral, result.Result, "Matchup should be Neutral");
        }

        private static void Test_DoubleStrongMatchup_HighestMultiplier()
        {
            // Arrange
            var chart = BuildTestChart();
            float baseDamage = 100f;
            ElementalType attackType = ElementalType.Blaze;
            ElementalType defenderType = ElementalType.Normal; // Not used
            // Both Frost types, Blaze is strong against both
            DualType? defenderDualType = new DualType(ElementalType.Frost, ElementalType.Frost);
            float doubleStrongMultiplier = 2.0f;

            // Act
            var result = ApplyTypeMatchup.Execute(
                baseDamage, attackType, defenderType, defenderDualType,
                chart, 1.5f, 0.5f, doubleStrongMultiplier, 0.25f);

            // Assert
            Assert.AreEqual(200f, result.Damage, "Blaze vs Frost/Frost should be 2.0x (double strong)");
            Assert.AreEqual(MatchupResult.DoubleStrong, result.Result, "Matchup should be DoubleStrong");
            Assert.AreEqual(2.0f, result.Multiplier, "Multiplier should be 2.0");
        }

        private static void Test_DoubleWeakMatchup_LowestMultiplier()
        {
            // Arrange
            var chart = BuildTestChart();
            float baseDamage = 100f;
            ElementalType attackType = ElementalType.Blaze;
            ElementalType defenderType = ElementalType.Normal; // Not used
            // Both Torrent types, Blaze is weak against both
            DualType? defenderDualType = new DualType(ElementalType.Torrent, ElementalType.Torrent);
            float doubleWeakMultiplier = 0.25f;

            // Act
            var result = ApplyTypeMatchup.Execute(
                baseDamage, attackType, defenderType, defenderDualType,
                chart, 1.5f, 0.5f, 2.0f, doubleWeakMultiplier);

            // Assert
            Assert.AreEqual(25f, result.Damage, "Blaze vs Torrent/Torrent should be 0.25x (double weak)");
            Assert.AreEqual(MatchupResult.DoubleWeak, result.Result, "Matchup should be DoubleWeak");
            Assert.AreEqual(0.25f, result.Multiplier, "Multiplier should be 0.25");
        }
    }
}
