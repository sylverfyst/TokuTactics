using System;
using TokuTactics.Bricks.Assist;

namespace TokuTactics.Tests.Bricks.Assist
{
    public static class CalculateAssistDamageMultiplierTests
    {
        public static void Run()
        {
            Test_Tier0_BaseMultiplier();
            Test_Tier1_AppliesBonus();
            Test_Tier2_UsesPairAttackMultiplier();
            Test_Tier3Plus_UsesPairAttackMultiplier();
            Test_ComboScaling_Applied();
            Console.WriteLine("CalculateAssistDamageMultiplierTests: All passed");
        }

        private static void Test_Tier0_BaseMultiplier()
        {
            var result = CalculateAssistDamageMultiplier.Execute(0, 1.0f, 1.25f, 1.5f);
            Assert(Math.Abs(result - 1.0f) < 0.001f, $"Tier 0 should be 1.0, got {result}");
        }

        private static void Test_Tier1_AppliesBonus()
        {
            var result = CalculateAssistDamageMultiplier.Execute(1, 1.0f, 1.25f, 1.5f);
            Assert(Math.Abs(result - 1.25f) < 0.001f, $"Tier 1 should be 1.25, got {result}");
        }

        private static void Test_Tier2_UsesPairAttackMultiplier()
        {
            var result = CalculateAssistDamageMultiplier.Execute(2, 1.0f, 1.25f, 1.5f);
            Assert(Math.Abs(result - 1.5f) < 0.001f, $"Tier 2 should be 1.5, got {result}");
        }

        private static void Test_Tier3Plus_UsesPairAttackMultiplier()
        {
            var result = CalculateAssistDamageMultiplier.Execute(4, 1.0f, 1.25f, 1.5f);
            Assert(Math.Abs(result - 1.5f) < 0.001f, $"Tier 4 should be 1.5, got {result}");
        }

        private static void Test_ComboScaling_Applied()
        {
            var result = CalculateAssistDamageMultiplier.Execute(1, 0.8f, 1.25f, 1.5f);
            Assert(Math.Abs(result - 1.0f) < 0.001f, $"0.8 * 1.25 should be 1.0, got {result}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
