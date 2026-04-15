using System;
using TokuTactics.Bricks.Assist;
using TokuTactics.Core.Types;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Tests.Bricks.Assist
{
    public static class CheckTier4RefreshEligibilityTests
    {
        public static void Run()
        {
            Test_Tier4_BothFresh_Eligible();
            Test_BelowTier4_NotEligible();
            Test_AssisterAlreadyGave_NotEligible();
            Test_AssisterAlreadyReceived_NotEligible();
            Test_AttackerAlreadyReceived_NotEligible();
            Test_NullAttackerState_Eligible();
            Console.WriteLine("CheckTier4RefreshEligibilityTests: All passed");
        }

        private static void Test_Tier4_BothFresh_Eligible()
        {
            var assister = MakeState();
            var attacker = MakeState();
            Assert(CheckTier4RefreshEligibility.Execute(4, assister, attacker) == true,
                "Both fresh at tier 4 should be eligible");
        }

        private static void Test_BelowTier4_NotEligible()
        {
            var assister = MakeState();
            var attacker = MakeState();
            Assert(CheckTier4RefreshEligibility.Execute(3, assister, attacker) == false,
                "Below tier 4 should not be eligible");
        }

        private static void Test_AssisterAlreadyGave_NotEligible()
        {
            var assister = MakeState();
            assister.HasUsedBondRefresh = true;
            var attacker = MakeState();
            Assert(CheckTier4RefreshEligibility.Execute(4, assister, attacker) == false,
                "Assister who already gave should not be eligible");
        }

        private static void Test_AssisterAlreadyReceived_NotEligible()
        {
            var assister = MakeState();
            assister.HasReceivedBondRefresh = true;
            var attacker = MakeState();
            Assert(CheckTier4RefreshEligibility.Execute(4, assister, attacker) == false,
                "Assister who already received should not be eligible");
        }

        private static void Test_AttackerAlreadyReceived_NotEligible()
        {
            var assister = MakeState();
            var attacker = MakeState();
            attacker.HasReceivedBondRefresh = true;
            Assert(CheckTier4RefreshEligibility.Execute(4, assister, attacker) == false,
                "Attacker who already received should not be eligible");
        }

        private static void Test_NullAttackerState_Eligible()
        {
            var assister = MakeState();
            Assert(CheckTier4RefreshEligibility.Execute(4, assister, null) == true,
                "Null attacker state should be treated as eligible");
        }

        private static AssistCandidateState MakeState()
        {
            return new AssistCandidateState
            {
                IsMorphed = true,
                AssisterDualType = DualType.Single(ElementalType.Blaze)
            };
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
