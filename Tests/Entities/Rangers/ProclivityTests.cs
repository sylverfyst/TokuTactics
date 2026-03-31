using System;
using System.Linq;
using TokuTactics.Core.Stats;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Entities.Rangers
{
    public class ProclivityTests
    {
        // === Random Assignment ===

        public void RandomAssignment_ReturnsSixUnique()
        {
            var assignment = Proclivity.RandomAssignment(6, new Random(42));

            Assert(assignment.Length == 6, "Should return 6 assignments");
            Assert(assignment.Distinct().Count() == 6, "All should be unique");
        }

        public void RandomAssignment_CoversAllStats()
        {
            var assignment = Proclivity.RandomAssignment(6, new Random(42));
            var allStats = (StatType[])Enum.GetValues(typeof(StatType));

            foreach (var stat in allStats)
            {
                Assert(assignment.Contains(stat), $"Should include {stat}");
            }
        }

        public void RandomAssignment_DifferentSeeds_DifferentResults()
        {
            var a = Proclivity.RandomAssignment(6, new Random(1));
            var b = Proclivity.RandomAssignment(6, new Random(999));

            bool anyDifferent = false;
            for (int i = 0; i < 6; i++)
            {
                if (a[i] != b[i])
                {
                    anyDifferent = true;
                    break;
                }
            }

            Assert(anyDifferent, "Different seeds should produce different assignments");
        }

        public void RandomAssignment_FewerThanSix_StillWorks()
        {
            var assignment = Proclivity.RandomAssignment(3, new Random(42));

            Assert(assignment.Length == 3, "Should return 3 assignments");
            Assert(assignment.Distinct().Count() == 3, "All should be unique");
        }

        // === Bonus Rolling ===

        public void RollBonus_HighLuck_MoreLikelyToTrigger()
        {
            // Use a fixed seed and count triggers over many rolls
            var rng = new Random(42);
            var proclivity = new Proclivity(StatType.STR, rng);
            proclivity.BaseChance = 0.3f;
            proclivity.LckScale = 0.01f;

            int highLuckTriggers = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (proclivity.RollBonus(50f) > 0) // High LCK
                    highLuckTriggers++;
            }

            rng = new Random(42);
            proclivity = new Proclivity(StatType.STR, rng);
            proclivity.BaseChance = 0.3f;
            proclivity.LckScale = 0.01f;

            int lowLuckTriggers = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (proclivity.RollBonus(0f) > 0) // Zero LCK
                    lowLuckTriggers++;
            }

            Assert(highLuckTriggers > lowLuckTriggers,
                $"High LCK ({highLuckTriggers}) should trigger more than low LCK ({lowLuckTriggers})");
        }

        public void RollBonus_ReturnsBonusAmount_WhenTriggered()
        {
            // Force trigger by setting chance to 100%
            var proclivity = new Proclivity(StatType.STR, new Random(42));
            proclivity.BaseChance = 1.0f;
            proclivity.BonusAmount = 2.5f;

            float bonus = proclivity.RollBonus(0f);

            Assert(bonus == 2.5f, "Should return the configured bonus amount");
        }

        public void RollBonus_ReturnsZero_WhenNotTriggered()
        {
            // Force no trigger by setting chance to 0%
            var proclivity = new Proclivity(StatType.STR, new Random(42));
            proclivity.BaseChance = 0f;
            proclivity.LckScale = 0f;

            float bonus = proclivity.RollBonus(0f);

            Assert(bonus == 0f, "Should return 0 when not triggered");
        }

        public void AffinityStat_IsSetCorrectly()
        {
            var proclivity = new Proclivity(StatType.MAG);

            Assert(proclivity.AffinityStat == StatType.MAG, "Should store the affinity stat");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new ProclivityTests();
            tests.RandomAssignment_ReturnsSixUnique();
            tests.RandomAssignment_CoversAllStats();
            tests.RandomAssignment_DifferentSeeds_DifferentResults();
            tests.RandomAssignment_FewerThanSix_StillWorks();
            tests.RollBonus_HighLuck_MoreLikelyToTrigger();
            tests.RollBonus_ReturnsBonusAmount_WhenTriggered();
            tests.RollBonus_ReturnsZero_WhenNotTriggered();
            tests.AffinityStat_IsSetCorrectly();
            System.Console.WriteLine("ProclivityTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
