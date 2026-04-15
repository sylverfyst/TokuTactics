using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class ValidateAttackRangeTests
    {
        public static void Run()
        {
            Test_InRange_ReturnsTrue();
            Test_OutOfRange_ReturnsFalse();
            Test_ExactRange_ReturnsTrue();
            Test_SamePosition_ReturnsTrue();
            Console.WriteLine("ValidateAttackRangeTests: All passed");
        }

        private static void Test_InRange_ReturnsTrue()
        {
            Assert(ValidateAttackRange.Execute(
                new GridPosition(5, 5), new GridPosition(5, 6), 3) == true,
                "Distance 1 within range 3 should be true");
        }

        private static void Test_OutOfRange_ReturnsFalse()
        {
            Assert(ValidateAttackRange.Execute(
                new GridPosition(0, 0), new GridPosition(5, 5), 3) == false,
                "Distance 10 outside range 3 should be false");
        }

        private static void Test_ExactRange_ReturnsTrue()
        {
            Assert(ValidateAttackRange.Execute(
                new GridPosition(5, 5), new GridPosition(5, 8), 3) == true,
                "Distance exactly equal to range should be true");
        }

        private static void Test_SamePosition_ReturnsTrue()
        {
            Assert(ValidateAttackRange.Execute(
                new GridPosition(5, 5), new GridPosition(5, 5), 1) == true,
                "Same position (distance 0) should be in range");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
