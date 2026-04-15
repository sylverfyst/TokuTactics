using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Tests.Systems.ActionEconomy
{
    public class BondTrackerTests
    {
        // === Basic Bond State ===

        public void GetBond_NewPair_StartsAtTierZero()
        {
            var tracker = new BondTracker();

            var bond = tracker.GetBond("red", "blue");

            Assert(bond.Tier == 0, "Should start at tier 0");
            Assert(bond.Experience == 0, "Should have 0 experience");
        }

        public void GetBond_OrderIndependent()
        {
            var tracker = new BondTracker();

            var bondAB = tracker.GetBond("red", "blue");
            var bondBA = tracker.GetBond("blue", "red");

            Assert(ReferenceEquals(bondAB, bondBA), "Should return same bond regardless of order");
        }

        // === Experience and Tier Progression ===

        public void AddAssistExperience_IncreasesExp()
        {
            var tracker = new BondTracker();

            tracker.AddAssistExperience("red", "blue", chaMultiplier: 0f);

            var bond = tracker.GetBond("red", "blue");
            Assert(bond.Experience > 0, "Should have experience after assist");
        }

        public void AddAssistExperience_ChaMultiplier_ScalesGain()
        {
            var trackerLow = new BondTracker();
            var trackerHigh = new BondTracker();

            trackerLow.AddAssistExperience("red", "blue", chaMultiplier: 0f);
            trackerHigh.AddAssistExperience("red", "blue", chaMultiplier: 10f);

            var bondLow = trackerLow.GetBond("red", "blue");
            var bondHigh = trackerHigh.GetBond("red", "blue");

            Assert(bondHigh.Experience > bondLow.Experience,
                "Higher CHA should give more bond experience");
        }

        public void TierProgression_ReachesTier1()
        {
            var tracker = new BondTracker();
            // Default threshold for tier 1 = 50

            for (int i = 0; i < 10; i++) // 10 assists * 10 base = 100 exp, past 50
                tracker.AddAssistExperience("red", "blue", chaMultiplier: 0f);

            var bond = tracker.GetBond("red", "blue");
            Assert(bond.Tier >= 1, "Should reach at least tier 1");
        }

        public void TierProgression_ReachesTier4()
        {
            var tracker = new BondTracker();
            // Default threshold for tier 4 = 700

            for (int i = 0; i < 100; i++) // 100 * 10 = 1000 exp, past 700
                tracker.AddAssistExperience("red", "blue", chaMultiplier: 0f);

            var bond = tracker.GetBond("red", "blue");
            Assert(bond.Tier == 4, "Should reach tier 4 with enough assists");
        }

        public void AddAssistExperience_ReturnsTierChange_OnTierUp()
        {
            var tracker = new BondTracker();
            tracker.TierThresholds = new[] { 5, 20, 50, 100 };

            BondTierChange change = null;
            for (int i = 0; i < 5; i++)
            {
                change = tracker.AddAssistExperience("red", "blue", chaMultiplier: 0f);
                if (change != null) break;
            }

            // 10 exp per assist, threshold[0]=5, so first assist crosses tier 1
            Assert(change != null, "Should have gotten a tier change");
            Assert(change.NewTier > change.OldTier, "New tier should be higher");
        }

        public void AddAssistExperience_ReturnsNull_WhenNoTierChange()
        {
            var tracker = new BondTracker();

            var change = tracker.AddAssistExperience("red", "blue", chaMultiplier: 0f);

            // First assist gives 10 exp, tier 1 threshold is 50
            // So no tier change
            Assert(change == null, "Should return null when tier doesn't change");
        }

        // === Bond State Queries ===

        public void BondState_Involves_ReturnsCorrectly()
        {
            var bond = new BondState("red", "blue");

            Assert(bond.Involves("red"), "Should involve red");
            Assert(bond.Involves("blue"), "Should involve blue");
            Assert(!bond.Involves("green"), "Should not involve green");
        }

        public void BondState_GetPartner_ReturnsOther()
        {
            var bond = new BondState("red", "blue");

            Assert(bond.GetPartner("red") == "blue", "Red's partner should be blue");
            Assert(bond.GetPartner("blue") == "red", "Blue's partner should be red");
            Assert(bond.GetPartner("green") == null, "Non-member should return null");
        }

        // === Collection Queries ===

        public void GetBondsAtTier_FiltersCorrectly()
        {
            var tracker = new BondTracker();
            tracker.TierThresholds = new[] { 10, 30, 60, 120 }; // Low thresholds for testing

            // Build up red-blue to tier 2+ (10 assists * 10 exp = 100 exp, past threshold[1]=30)
            for (int i = 0; i < 10; i++)
                tracker.AddAssistExperience("red", "blue", 0);

            // Leave red-green at tier 1 (1 assist * 10 exp = 10, past threshold[0]=10)
            tracker.AddAssistExperience("red", "green", 0);

            var tier2Plus = tracker.GetBondsAtTier(2);

            Assert(tier2Plus.Count >= 1, "Should have at least 1 bond at tier 2+");
        }

        public void GetBondsForRanger_ReturnsAllPairs()
        {
            var tracker = new BondTracker();
            tracker.AddAssistExperience("red", "blue", 0);
            tracker.AddAssistExperience("red", "green", 0);
            tracker.AddAssistExperience("blue", "green", 0);

            var redBonds = tracker.GetBondsForRanger("red");

            Assert(redBonds.Count == 2, "Red should have 2 bonds (with blue and green)");
        }

        // === Multiple Independent Bonds ===

        public void MultipleBonds_IndependentTracking()
        {
            var tracker = new BondTracker();

            for (int i = 0; i < 10; i++)
                tracker.AddAssistExperience("red", "blue", 0);

            var redBlue = tracker.GetBond("red", "blue");
            var redGreen = tracker.GetBond("red", "green");

            Assert(redBlue.Experience > 0, "Red-Blue should have experience");
            Assert(redGreen.Experience == 0, "Red-Green should have no experience");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new BondTrackerTests();
            tests.GetBond_NewPair_StartsAtTierZero();
            tests.GetBond_OrderIndependent();
            tests.AddAssistExperience_IncreasesExp();
            tests.AddAssistExperience_ChaMultiplier_ScalesGain();
            tests.TierProgression_ReachesTier1();
            tests.TierProgression_ReachesTier4();
            tests.AddAssistExperience_ReturnsTierChange_OnTierUp();
            tests.AddAssistExperience_ReturnsNull_WhenNoTierChange();
            tests.BondState_Involves_ReturnsCorrectly();
            tests.BondState_GetPartner_ReturnsOther();
            tests.GetBondsAtTier_FiltersCorrectly();
            tests.GetBondsForRanger_ReturnsAllPairs();
            tests.MultipleBonds_IndependentTracking();
            System.Console.WriteLine("BondTrackerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
