using System.Collections.Generic;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Systems.ActionEconomy
{
    public class TurnOrderTests
    {
        private class MockParticipant : ITurnParticipant
        {
            public string ParticipantId { get; set; }
            public float Speed { get; set; }
            public bool CanAct { get; set; } = true;
        }

        // === Build ===

        public void Build_SortsBySpeedDescending()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "slow", Speed = 3 },
                new MockParticipant { ParticipantId = "fast", Speed = 10 },
                new MockParticipant { ParticipantId = "mid", Speed = 6 }
            });

            Assert(order.Entries[0].Participant.ParticipantId == "fast", "Fastest should be first");
            Assert(order.Entries[1].Participant.ParticipantId == "mid", "Mid should be second");
            Assert(order.Entries[2].Participant.ParticipantId == "slow", "Slowest should be last");
        }

        public void Build_FiltersOutUnableToAct()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "active", Speed = 5, CanAct = true },
                new MockParticipant { ParticipantId = "stunned", Speed = 8, CanAct = false },
                new MockParticipant { ParticipantId = "active2", Speed = 3, CanAct = true }
            });

            Assert(order.Entries.Count == 2, "Should only include units that can act");
            Assert(order.Entries[0].Participant.ParticipantId == "active", "First active should be first");
        }

        public void Build_EmptyList_NoEntries()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>());

            Assert(order.Entries.Count == 0, "Should have no entries");
            Assert(order.IsPhaseComplete, "Empty phase should be complete");
        }

        // === Advance ===

        public void Advance_ReturnsFirstEntry()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "a", Speed = 10 },
                new MockParticipant { ParticipantId = "b", Speed = 5 }
            });

            var entry = order.Advance();

            Assert(entry != null, "Should return an entry");
            Assert(entry.Participant.ParticipantId == "a", "Should be the fastest unit");
        }

        public void Advance_ProgressesThroughAll()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "a", Speed = 10 },
                new MockParticipant { ParticipantId = "b", Speed = 5 }
            });

            var first = order.Advance();
            var second = order.Advance();
            var third = order.Advance();

            Assert(first.Participant.ParticipantId == "a", "First should be a");
            Assert(second.Participant.ParticipantId == "b", "Second should be b");
            Assert(third == null, "Third should be null (phase complete)");
            Assert(order.IsPhaseComplete, "Phase should be complete");
        }

        public void Current_ReturnsCurrentEntry()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "a", Speed = 10 }
            });

            Assert(order.Current == null, "Should be null before first advance");

            order.Advance();
            Assert(order.Current.Participant.ParticipantId == "a", "Should be current after advance");
        }

        // === Reset ===

        public void Reset_AllowsReplay()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "a", Speed = 10 }
            });

            order.Advance();
            Assert(order.IsPhaseComplete == false || order.Advance() == null, "Should progress");

            order.Reset();
            var entry = order.Advance();

            Assert(entry != null, "Should be able to advance after reset");
            Assert(entry.Participant.ParticipantId == "a", "Should restart from first");
        }

        // === Phase Complete ===

        public void IsPhaseComplete_FalseBeforeAllAdvanced()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "a", Speed = 10 },
                new MockParticipant { ParticipantId = "b", Speed = 5 }
            });

            order.Advance();

            Assert(!order.IsPhaseComplete, "Should not be complete with one remaining");
        }

        public void IsPhaseComplete_TrueAfterAllAdvanced()
        {
            var order = new TurnOrder();
            order.Build(new List<ITurnParticipant>
            {
                new MockParticipant { ParticipantId = "a", Speed = 10 }
            });

            order.Advance();
            order.Advance(); // Past the end

            Assert(order.IsPhaseComplete, "Should be complete after all advanced");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new TurnOrderTests();
            tests.Build_SortsBySpeedDescending();
            tests.Build_FiltersOutUnableToAct();
            tests.Build_EmptyList_NoEntries();
            tests.Advance_ReturnsFirstEntry();
            tests.Advance_ProgressesThroughAll();
            tests.Current_ReturnsCurrentEntry();
            tests.Reset_AllowsReplay();
            tests.IsPhaseComplete_FalseBeforeAllAdvanced();
            tests.IsPhaseComplete_TrueAfterAllAdvanced();
            System.Console.WriteLine("TurnOrderTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
