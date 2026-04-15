using System;
using TokuTactics.Bricks.Bond;

namespace TokuTactics.Tests.Bricks.Bond
{
    public static class CalculateScaledBondExpTests
    {
        public static void Run()
        {
            Test_ZeroCha_ReturnsBase();
            Test_PositiveCha_Scales();
            Test_HighCha_ScalesLinearly();
            Console.WriteLine("CalculateScaledBondExpTests: All passed");
        }

        private static void Test_ZeroCha_ReturnsBase()
        {
            var result = CalculateScaledBondExp.Execute(10, 0f);
            Assert(result == 10, $"Zero CHA should return base, got {result}");
        }

        private static void Test_PositiveCha_Scales()
        {
            // 10 * (1.0 + 5.0 * 0.1) = 10 * 1.5 = 15
            var result = CalculateScaledBondExp.Execute(10, 5.0f);
            Assert(result == 15, $"Expected 15, got {result}");
        }

        private static void Test_HighCha_ScalesLinearly()
        {
            // 10 * (1.0 + 10.0 * 0.1) = 10 * 2.0 = 20
            var result = CalculateScaledBondExp.Execute(10, 10.0f);
            Assert(result == 20, $"Expected 20, got {result}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
