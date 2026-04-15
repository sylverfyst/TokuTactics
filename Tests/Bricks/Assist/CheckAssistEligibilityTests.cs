using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Assist;
using TokuTactics.Core.Types;
using TokuTactics.Core.Assist;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Tests.Bricks.Assist
{
    public static class CheckAssistEligibilityTests
    {
        public static void Run()
        {
            Test_MorphedRanger_Eligible();
            Test_Self_NotEligible();
            Test_NotInStates_NotEligible();
            Test_Unmorphed_NotEligible();
            Console.WriteLine("CheckAssistEligibilityTests: All passed");
        }

        private static void Test_MorphedRanger_Eligible()
        {
            var states = MakeStates(("r2", true));
            Assert(CheckAssistEligibility.Execute("r2", "r1", states) == true,
                "Morphed ranger should be eligible");
        }

        private static void Test_Self_NotEligible()
        {
            var states = MakeStates(("r1", true));
            Assert(CheckAssistEligibility.Execute("r1", "r1", states) == false,
                "Cannot assist yourself");
        }

        private static void Test_NotInStates_NotEligible()
        {
            var states = MakeStates();
            Assert(CheckAssistEligibility.Execute("enemy_1", "r1", states) == false,
                "Unit not in ranger states should not be eligible");
        }

        private static void Test_Unmorphed_NotEligible()
        {
            var states = MakeStates(("r2", false));
            Assert(CheckAssistEligibility.Execute("r2", "r1", states) == false,
                "Unmorphed ranger should not be eligible");
        }

        private static Dictionary<string, AssistCandidateState> MakeStates(params (string id, bool morphed)[] rangers)
        {
            var states = new Dictionary<string, AssistCandidateState>();
            foreach (var (id, morphed) in rangers)
            {
                states[id] = new AssistCandidateState
                {
                    IsMorphed = morphed,
                    AssisterDualType = DualType.Single(ElementalType.Blaze)
                };
            }
            return states;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
