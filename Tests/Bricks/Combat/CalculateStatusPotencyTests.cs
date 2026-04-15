using System;
using TokuTactics.Bricks.Combat;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class CalculateStatusPotencyTests
    {
        public static void Run()
        {
            Test_ZeroMag_ReturnsOne();
            Test_PositiveMag_ScalesCorrectly();
            Test_HighMag_ScalesLinearly();
            Console.WriteLine("CalculateStatusPotencyTests: All passed");
        }

        private static void Test_ZeroMag_ReturnsOne()
        {
            var result = CalculateStatusPotency.Execute(0f, 0.01f);
            Assert(Math.Abs(result - 1.0f) < 0.001f, $"Expected 1.0, got {result}");
        }

        private static void Test_PositiveMag_ScalesCorrectly()
        {
            var result = CalculateStatusPotency.Execute(10f, 0.01f);
            Assert(Math.Abs(result - 1.1f) < 0.001f, $"Expected 1.1, got {result}");
        }

        private static void Test_HighMag_ScalesLinearly()
        {
            var result = CalculateStatusPotency.Execute(50f, 0.02f);
            Assert(Math.Abs(result - 2.0f) < 0.001f, $"Expected 2.0, got {result}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
