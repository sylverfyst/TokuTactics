using System;
using TokuTactics.Bricks.Bond;

namespace TokuTactics.Tests.Bricks.Bond
{
    public static class ResolveBondTierTests
    {
        public static void Run()
        {
            Test_BelowAllThresholds_Tier0();
            Test_ExactThreshold_ReturnsTier();
            Test_AboveHighest_ReturnsMaxTier();
            Test_BetweenThresholds_ReturnsLower();
            Console.WriteLine("ResolveBondTierTests: All passed");
        }

        private static void Test_BelowAllThresholds_Tier0()
        {
            var result = ResolveBondTier.Execute(0, new[] { 50, 150, 350, 700 });
            Assert(result == 0, $"Below all should be tier 0, got {result}");
        }

        private static void Test_ExactThreshold_ReturnsTier()
        {
            var result = ResolveBondTier.Execute(50, new[] { 50, 150, 350, 700 });
            Assert(result == 1, $"Exact tier 1 threshold should return 1, got {result}");
        }

        private static void Test_AboveHighest_ReturnsMaxTier()
        {
            var result = ResolveBondTier.Execute(999, new[] { 50, 150, 350, 700 });
            Assert(result == 4, $"Above all should be tier 4, got {result}");
        }

        private static void Test_BetweenThresholds_ReturnsLower()
        {
            var result = ResolveBondTier.Execute(100, new[] { 50, 150, 350, 700 });
            Assert(result == 1, $"Between tier 1 and 2 should return 1, got {result}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
