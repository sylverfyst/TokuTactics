using System;
using TokuTactics.Bricks.Loadout;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Rangers;
using TokuTactics.Commands.Loadout;

namespace TokuTactics.Tests.Bricks.Loadout
{
    public static class ValidateMorphRequestTests
    {
        public static void Run()
        {
            Test_Unmorphed_LoadoutLocked_MorphComplete();
            Test_Unmorphed_LoadoutNotLocked_NeedsLoadout();
            Test_AlreadyMorphed_Invalid();
            Test_Dead_Invalid();
            Console.WriteLine("ValidateMorphRequestTests: All passed");
        }

        private static void Test_Unmorphed_LoadoutLocked_MorphComplete()
        {
            var ranger = MakeRanger("r1");
            var result = ValidateMorphRequest.Execute(ranger, isLoadoutLocked: true);
            Assert(result == MorphRequestResult.MorphComplete, "Unmorphed + locked should be MorphComplete");
        }

        private static void Test_Unmorphed_LoadoutNotLocked_NeedsLoadout()
        {
            var ranger = MakeRanger("r1");
            var result = ValidateMorphRequest.Execute(ranger, isLoadoutLocked: false);
            Assert(result == MorphRequestResult.NeedsLoadout, "Unmorphed + unlocked should be NeedsLoadout");
        }

        private static void Test_AlreadyMorphed_Invalid()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph();
            var result = ValidateMorphRequest.Execute(ranger, isLoadoutLocked: true);
            Assert(result == MorphRequestResult.Invalid, "Already morphed should be Invalid");
        }

        private static void Test_Dead_Invalid()
        {
            var ranger = MakeRanger("r1");
            ranger.UnmorphedHealth.TakeDamage(999f);
            var result = ValidateMorphRequest.Execute(ranger, isLoadoutLocked: true);
            Assert(result == MorphRequestResult.Invalid, "Dead ranger should be Invalid");
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
