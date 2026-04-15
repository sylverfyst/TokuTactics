using System;
using TokuTactics.Bricks.Assist;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Bricks.Assist
{
    public static class MapRangerToAssistStateTests
    {
        public static void Run()
        {
            Test_UnmorphedRanger_MapsCorrectly();
            Test_MorphedRanger_MapsFormData();
            Test_IncludesBudgetRefreshFlags();
            Test_NullBudget_HandledGracefully();
            Console.WriteLine("MapRangerToAssistStateTests: All passed");
        }

        private static void Test_UnmorphedRanger_MapsCorrectly()
        {
            var ranger = MakeRanger("r1");
            var budget = new ActionBudget();

            var state = MapRangerToAssistState.Execute(ranger, budget);

            Assert(!state.IsMorphed, "Unmorphed ranger should map IsMorphed=false");
            Assert(state.Str == 8f, $"STR should be 8, got {state.Str}");
        }

        private static void Test_MorphedRanger_MapsFormData()
        {
            var ranger = MakeRanger("r1");
            ranger.Morph();
            var budget = new ActionBudget();

            var state = MapRangerToAssistState.Execute(ranger, budget);

            Assert(state.IsMorphed, "Morphed ranger should map IsMorphed=true");
            Assert(state.CurrentFormId != null, "Should have a current form ID");
            Assert(state.WeaponBasePower > 0, "Should have weapon power from form");
        }

        private static void Test_IncludesBudgetRefreshFlags()
        {
            var ranger = MakeRanger("r1");
            var budget = new ActionBudget();
            budget.HasUsedBondRefresh = true;
            budget.HasReceivedBondRefresh = true;

            var state = MapRangerToAssistState.Execute(ranger, budget);

            Assert(state.HasUsedBondRefresh, "Should map HasUsedBondRefresh");
            Assert(state.HasReceivedBondRefresh, "Should map HasReceivedBondRefresh");
        }

        private static void Test_NullBudget_HandledGracefully()
        {
            var ranger = MakeRanger("r1");

            var state = MapRangerToAssistState.Execute(ranger, null);

            Assert(!state.HasUsedBondRefresh, "Null budget should default to false");
            Assert(!state.HasReceivedBondRefresh, "Null budget should default to false");
        }

        private static Ranger MakeRanger(string id)
        {
            return new Ranger(id, id, ElementalType.Blaze,
                new Proclivity(StatType.STR), null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4, cha: 3),
                50f, FormCatalog.BaseForm());
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
