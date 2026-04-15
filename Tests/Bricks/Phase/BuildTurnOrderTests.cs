using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Phase;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Bricks.Phase
{
    public static class BuildTurnOrderTests
    {
        public static void Run()
        {
            Test_SortsBySpeedDescending();
            Test_FiltersCannotAct();
            Test_EmptyInput_EmptyOutput();
            Test_AssignsOrderIndices();
            Console.WriteLine("BuildTurnOrderTests: All passed");
        }

        private static void Test_SortsBySpeedDescending()
        {
            var participants = new List<ITurnParticipant>
            {
                new TestParticipant("slow", 3f),
                new TestParticipant("fast", 10f),
                new TestParticipant("mid", 6f)
            };

            var result = BuildTurnOrder.Execute(participants);

            Assert(result.Count == 3, $"Expected 3, got {result.Count}");
            Assert(result[0].Participant.ParticipantId == "fast", "First should be fastest");
            Assert(result[1].Participant.ParticipantId == "mid", "Second should be mid");
            Assert(result[2].Participant.ParticipantId == "slow", "Third should be slowest");
        }

        private static void Test_FiltersCannotAct()
        {
            var participants = new List<ITurnParticipant>
            {
                new TestParticipant("alive", 5f, canAct: true),
                new TestParticipant("dead", 10f, canAct: false)
            };

            var result = BuildTurnOrder.Execute(participants);

            Assert(result.Count == 1, $"Should filter dead, got {result.Count}");
            Assert(result[0].Participant.ParticipantId == "alive", "Should only include alive");
        }

        private static void Test_EmptyInput_EmptyOutput()
        {
            var result = BuildTurnOrder.Execute(new List<ITurnParticipant>());
            Assert(result.Count == 0, "Empty input should produce empty output");
        }

        private static void Test_AssignsOrderIndices()
        {
            var participants = new List<ITurnParticipant>
            {
                new TestParticipant("a", 5f),
                new TestParticipant("b", 10f)
            };

            var result = BuildTurnOrder.Execute(participants);

            Assert(result[0].OrderIndex == 0, "First should be index 0");
            Assert(result[1].OrderIndex == 1, "Second should be index 1");
            Assert(!result[0].HasActed, "HasActed should be false");
        }

        private class TestParticipant : ITurnParticipant
        {
            public string ParticipantId { get; }
            public float Speed { get; }
            public bool CanAct { get; }

            public TestParticipant(string id, float speed, bool canAct = true)
            {
                ParticipantId = id;
                Speed = speed;
                CanAct = canAct;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
