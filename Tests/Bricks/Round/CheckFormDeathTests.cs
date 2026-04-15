using System;
using TokuTactics.Bricks.Round;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Bricks.Round
{
    public static class CheckFormDeathTests
    {
        public static void Run()
        {
            Test_Unmorphed_ReturnsFalse();
            Test_MorphedAlive_ReturnsFalse();
            Test_MorphedDead_ReturnsTrue();
            Console.WriteLine("CheckFormDeathTests: All passed");
        }

        private static void Test_Unmorphed_ReturnsFalse()
        {
            var ranger = MakeRanger("r1");

            Assert(CheckFormDeath.Execute(ranger) == false, "Unmorphed ranger should return false");
        }

        private static void Test_MorphedAlive_ReturnsFalse()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph();

            Assert(CheckFormDeath.Execute(ranger) == false, "Morphed alive form should return false");
        }

        private static void Test_MorphedDead_ReturnsTrue()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph();
            // Kill the form
            ranger.CurrentForm.Health.TakeDamage(999f);

            Assert(CheckFormDeath.Execute(ranger) == true, "Morphed dead form should return true");
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
