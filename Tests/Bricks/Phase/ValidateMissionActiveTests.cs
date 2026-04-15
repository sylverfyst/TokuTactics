using System;
using TokuTactics.Bricks.Phase;
using TokuTactics.Systems.PhaseManagement;

namespace TokuTactics.Tests.Bricks.Phase
{
    public static class ValidateMissionActiveTests
    {
        public static void Run()
        {
            Test_Active_ReturnsTrue();
            Test_NotStarted_ReturnsFalse();
            Test_Victory_ReturnsFalse();
            Test_Defeat_ReturnsFalse();
            Console.WriteLine("ValidateMissionActiveTests: All passed");
        }

        private static void Test_Active_ReturnsTrue()
        {
            Assert(ValidateMissionActive.Execute(MissionState.Active) == true, "Active should return true");
        }

        private static void Test_NotStarted_ReturnsFalse()
        {
            Assert(ValidateMissionActive.Execute(MissionState.NotStarted) == false, "NotStarted should return false");
        }

        private static void Test_Victory_ReturnsFalse()
        {
            Assert(ValidateMissionActive.Execute(MissionState.Victory) == false, "Victory should return false");
        }

        private static void Test_Defeat_ReturnsFalse()
        {
            Assert(ValidateMissionActive.Execute(MissionState.Defeat) == false, "Defeat should return false");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
