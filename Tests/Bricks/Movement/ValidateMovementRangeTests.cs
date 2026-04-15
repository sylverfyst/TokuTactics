using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Movement;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Bricks.Movement
{
    public static class ValidateMovementRangeTests
    {
        public static void Run()
        {
            Test_InRange_ReturnsTrue();
            Test_NotInRange_ReturnsFalse();
            Test_NullRange_ReturnsFalse();
            Console.WriteLine("ValidateMovementRangeTests: All passed");
        }

        private static void Test_InRange_ReturnsTrue()
        {
            var range = new Dictionary<GridPosition, int>
            {
                { new GridPosition(1, 1), 1 },
                { new GridPosition(2, 2), 2 }
            };

            Assert(ValidateMovementRange.Execute(new GridPosition(1, 1), range) == true,
                "Destination in range should return true");
        }

        private static void Test_NotInRange_ReturnsFalse()
        {
            var range = new Dictionary<GridPosition, int>
            {
                { new GridPosition(1, 1), 1 }
            };

            Assert(ValidateMovementRange.Execute(new GridPosition(5, 5), range) == false,
                "Destination not in range should return false");
        }

        private static void Test_NullRange_ReturnsFalse()
        {
            Assert(ValidateMovementRange.Execute(new GridPosition(0, 0), null) == false,
                "Null range should return false");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
