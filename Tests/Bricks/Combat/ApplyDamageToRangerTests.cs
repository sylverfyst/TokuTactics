using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Bricks.Combat
{
    public static class ApplyDamageToRangerTests
    {
        public static void Run()
        {
            Test_Unmorphed_DamagesUnmorphedHealth();
            Test_Morphed_DamagesFormHealth();
            Console.WriteLine("ApplyDamageToRangerTests: All passed");
        }

        private static void Test_Unmorphed_DamagesUnmorphedHealth()
        {
            var ranger = MakeRanger("r1");
            float before = ranger.UnmorphedHealth.Current;

            ApplyDamageToRanger.Execute(ranger, 10);

            Assert(ranger.UnmorphedHealth.Current == before - 10,
                $"Expected {before - 10}, got {ranger.UnmorphedHealth.Current}");
        }

        private static void Test_Morphed_DamagesFormHealth()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph();
            float before = ranger.CurrentForm.Health.Current;

            ApplyDamageToRanger.Execute(ranger, 10);

            Assert(ranger.CurrentForm.Health.Current == before - 10,
                $"Expected {before - 10}, got {ranger.CurrentForm.Health.Current}");
            Assert(ranger.UnmorphedHealth.Current == ranger.UnmorphedHealth.Maximum,
                "Unmorphed health should be unchanged");
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
