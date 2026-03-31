using System.Collections.Generic;
using TokuTactics.Core.Stats;

namespace TokuTactics.Tests.Core.Stats
{
    /// <summary>
    /// Tests for StatBlock. Run with any xUnit/NUnit test runner.
    /// Using a simple assertion pattern that works without a test framework dependency.
    /// Replace Assert.That() calls with your preferred test framework.
    /// </summary>
    public class StatBlockTests
    {
        public void DefaultConstructor_AllStatsZero()
        {
            var block = new StatBlock();

            Assert(block.Get(StatType.STR) == 0f, "STR should be 0");
            Assert(block.Get(StatType.DEF) == 0f, "DEF should be 0");
            Assert(block.Get(StatType.SPD) == 0f, "SPD should be 0");
            Assert(block.Get(StatType.MAG) == 0f, "MAG should be 0");
            Assert(block.Get(StatType.CHA) == 0f, "CHA should be 0");
            Assert(block.Get(StatType.LCK) == 0f, "LCK should be 0");
        }

        public void Create_SetsSpecifiedValues()
        {
            var block = StatBlock.Create(str: 10, def: 5, spd: 8);

            Assert(block.Get(StatType.STR) == 10f, "STR should be 10");
            Assert(block.Get(StatType.DEF) == 5f, "DEF should be 5");
            Assert(block.Get(StatType.SPD) == 8f, "SPD should be 8");
            Assert(block.Get(StatType.MAG) == 0f, "MAG should default to 0");
        }

        public void Add_CombinesTwoBlocks()
        {
            var a = StatBlock.Create(str: 10, def: 5);
            var b = StatBlock.Create(str: 3, def: 7, mag: 4);

            var result = a.Add(b);

            Assert(result.Get(StatType.STR) == 13f, "STR should be 13");
            Assert(result.Get(StatType.DEF) == 12f, "DEF should be 12");
            Assert(result.Get(StatType.MAG) == 4f, "MAG should be 4");
        }

        public void Add_DoesNotMutateOriginal()
        {
            var a = StatBlock.Create(str: 10);
            var b = StatBlock.Create(str: 5);

            var result = a.Add(b);

            Assert(a.Get(StatType.STR) == 10f, "Original A should be unchanged");
            Assert(b.Get(StatType.STR) == 5f, "Original B should be unchanged");
            Assert(result.Get(StatType.STR) == 15f, "Result should be sum");
        }

        public void Scale_MultipliesAllValues()
        {
            var block = StatBlock.Create(str: 10, def: 4, spd: 6);

            var result = block.Scale(2.0f);

            Assert(result.Get(StatType.STR) == 20f, "STR should be doubled");
            Assert(result.Get(StatType.DEF) == 8f, "DEF should be doubled");
            Assert(result.Get(StatType.SPD) == 12f, "SPD should be doubled");
        }

        public void Scale_DoesNotMutateOriginal()
        {
            var block = StatBlock.Create(str: 10);
            block.Scale(3.0f);

            Assert(block.Get(StatType.STR) == 10f, "Original should be unchanged");
        }

        public void WithBonus_AddsToBonusStat()
        {
            var block = StatBlock.Create(str: 10, def: 5);

            var result = block.WithBonus(StatType.STR, 3f);

            Assert(result.Get(StatType.STR) == 13f, "STR should have bonus");
            Assert(result.Get(StatType.DEF) == 5f, "DEF should be unchanged");
        }

        public void WithBonus_DoesNotMutateOriginal()
        {
            var block = StatBlock.Create(str: 10);
            block.WithBonus(StatType.STR, 5f);

            Assert(block.Get(StatType.STR) == 10f, "Original should be unchanged");
        }

        public void DictionaryConstructor_HandlesPartialInput()
        {
            var values = new Dictionary<StatType, float>
            {
                { StatType.STR, 15f },
                { StatType.LCK, 7f }
            };

            var block = new StatBlock(values);

            Assert(block.Get(StatType.STR) == 15f, "STR should be set");
            Assert(block.Get(StatType.LCK) == 7f, "LCK should be set");
            Assert(block.Get(StatType.DEF) == 0f, "Missing stats should default to 0");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new StatBlockTests();
            tests.DefaultConstructor_AllStatsZero();
            tests.Create_SetsSpecifiedValues();
            tests.Add_CombinesTwoBlocks();
            tests.Add_DoesNotMutateOriginal();
            tests.Scale_MultipliesAllValues();
            tests.Scale_DoesNotMutateOriginal();
            tests.WithBonus_AddsToBonusStat();
            tests.WithBonus_DoesNotMutateOriginal();
            tests.DictionaryConstructor_HandlesPartialInput();
            System.Console.WriteLine("StatBlockTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
