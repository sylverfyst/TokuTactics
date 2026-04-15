using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Phase;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Bricks.Phase
{
    public static class CheckRangerDefeatTests
    {
        public static void Run()
        {
            Test_AllAlive_ReturnsNull();
            Test_OneDeadRanger_ReturnsId();
            Test_MultipleDeadRangers_ReturnsFirst();
            Test_EmptyList_ReturnsNull();
            Console.WriteLine("CheckRangerDefeatTests: All passed");
        }

        private static void Test_AllAlive_ReturnsNull()
        {
            var rangers = new List<Ranger> { MakeRanger("r1"), MakeRanger("r2") };

            var result = CheckRangerDefeat.Execute(rangers);

            Assert(result == null, "All alive should return null");
        }

        private static void Test_OneDeadRanger_ReturnsId()
        {
            var dead = MakeRanger("r2");
            dead.UnmorphedHealth.TakeDamage(999f);
            var rangers = new List<Ranger> { MakeRanger("r1"), dead };

            var result = CheckRangerDefeat.Execute(rangers);

            Assert(result == "r2", $"Should return dead ranger ID, got {result}");
        }

        private static void Test_MultipleDeadRangers_ReturnsFirst()
        {
            var dead1 = MakeRanger("r1");
            dead1.UnmorphedHealth.TakeDamage(999f);
            var dead2 = MakeRanger("r2");
            dead2.UnmorphedHealth.TakeDamage(999f);
            var rangers = new List<Ranger> { dead1, dead2 };

            var result = CheckRangerDefeat.Execute(rangers);

            Assert(result == "r1", $"Should return first dead ranger, got {result}");
        }

        private static void Test_EmptyList_ReturnsNull()
        {
            var result = CheckRangerDefeat.Execute(new List<Ranger>());

            Assert(result == null, "Empty list should return null");
        }

        private static Ranger MakeRanger(string id)
        {
            return new Ranger(id, id, ElementalType.Blaze,
                new Proclivity(StatType.STR), null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4),
                50f, FormCatalog.BaseForm());
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
