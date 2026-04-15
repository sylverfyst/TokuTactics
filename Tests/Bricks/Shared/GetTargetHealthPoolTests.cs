using System;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Bricks.Shared
{
    public static class GetTargetHealthPoolTests
    {
        public static void Run()
        {
            Test_Unmorphed_ReturnsUnmorphedHealth();
            Test_Morphed_ReturnsFormHealth();
            Console.WriteLine("GetTargetHealthPoolTests: All passed");
        }

        private static void Test_Unmorphed_ReturnsUnmorphedHealth()
        {
            var ranger = MakeRanger("r1");

            var pool = GetTargetHealthPool.Execute(ranger);

            Assert(pool == ranger.UnmorphedHealth, "Unmorphed ranger should return unmorphed health pool");
        }

        private static void Test_Morphed_ReturnsFormHealth()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph(); // Morphs into base form

            var pool = GetTargetHealthPool.Execute(ranger);

            Assert(pool == ranger.CurrentForm.Health, "Morphed ranger should return form health pool");
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
