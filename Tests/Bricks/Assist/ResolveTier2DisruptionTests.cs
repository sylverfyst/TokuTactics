using System;
using TokuTactics.Bricks.Assist;
using TokuTactics.Core.Types;
using TokuTactics.Core.Assist;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Tests.Bricks.Assist
{
    public static class ResolveTier2DisruptionTests
    {
        public static void Run()
        {
            Test_Tier2_NotBaseForm_ReturnsFormId();
            Test_Tier2_InBaseForm_ReturnsNull();
            Test_Tier1_ReturnsNull();
            Test_Tier3_ReturnsNull();
            Console.WriteLine("ResolveTier2DisruptionTests: All passed");
        }

        private static void Test_Tier2_NotBaseForm_ReturnsFormId()
        {
            var state = MakeState("form_blaze", isBase: false);
            var result = ResolveTier2Disruption.Execute(2, state);
            Assert(result == "form_blaze", $"Should return vacated form ID, got {result}");
        }

        private static void Test_Tier2_InBaseForm_ReturnsNull()
        {
            var state = MakeState("form_base", isBase: true);
            var result = ResolveTier2Disruption.Execute(2, state);
            Assert(result == null, "Base form should not be disrupted");
        }

        private static void Test_Tier1_ReturnsNull()
        {
            var state = MakeState("form_blaze", isBase: false);
            var result = ResolveTier2Disruption.Execute(1, state);
            Assert(result == null, "Tier 1 should not disrupt");
        }

        private static void Test_Tier3_ReturnsNull()
        {
            var state = MakeState("form_blaze", isBase: false);
            var result = ResolveTier2Disruption.Execute(3, state);
            Assert(result == null, "Tier 3 should not disrupt");
        }

        private static AssistCandidateState MakeState(string formId, bool isBase)
        {
            return new AssistCandidateState
            {
                CurrentFormId = formId,
                IsInBaseForm = isBase,
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
