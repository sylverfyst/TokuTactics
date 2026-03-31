using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Zords;

namespace TokuTactics.Tests.Entities.Zords
{
    public class MegazordTests
    {
        private ZordData MakeZord(string id, ElementalType type, float str = 10, int recruitOrder = 0)
        {
            return new ZordData(id, id, type,
                StatBlock.Create(str: str, def: 5),
                StatBlock.Create(str: 1, def: 0.5f),
                100, 10, 4, 1, 1.0f, null,
                isGrowthZord: false)
            { RecruitmentOrder = recruitOrder };
        }

        private ZordInstance MakeInstance(ZordData data, int level = 1)
        {
            return new ZordInstance(data, level);
        }

        // === Combination ===

        public void Combine_Succeeds_WhenAvailable()
        {
            var mz = new Megazord();
            var zords = new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) };

            bool result = mz.Combine(zords);

            Assert(result, "Should combine");
            Assert(mz.IsCombined, "Should be combined");
            Assert(mz.CombinationTimer == mz.BaseCombinationDuration, "Timer should be set");
        }

        public void Combine_Fails_WhenAlreadyCombined()
        {
            var mz = new Megazord();
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });

            bool result = mz.Combine(new[] { MakeInstance(MakeZord("z2", ElementalType.Frost)) });

            Assert(!result, "Should not combine while already combined");
        }

        public void Combine_Fails_WhenOnCooldown()
        {
            var mz = new Megazord();
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });
            mz.Decombine(); // Start cooldown

            bool result = mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });

            Assert(!result, "Should not combine while on cooldown");
        }

        public void Combine_Fails_WithEmptyZords()
        {
            var mz = new Megazord();

            bool result = mz.Combine(new ZordInstance[] { });

            Assert(!result, "Should not combine with no zords");
        }

        // === Timer ===

        public void TickTimer_CountsDown()
        {
            var mz = new Megazord() { BaseCombinationDuration = 3 };
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });

            bool expired = mz.TickTimer();

            Assert(!expired, "Should not expire after 1 tick");
            Assert(mz.CombinationTimer == 2, "Should be 2 remaining");
        }

        public void TickTimer_Decombines_WhenExpired()
        {
            var mz = new Megazord() { BaseCombinationDuration = 2 };
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });

            mz.TickTimer();
            bool expired = mz.TickTimer();

            Assert(expired, "Should expire on second tick");
            Assert(!mz.IsCombined, "Should be decombined");
            Assert(mz.CombinationCooldown.IsOnCooldown, "Cooldown should start");
        }

        // === Additive Stats ===

        public void GetStats_SumsAllComponentZords()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze, str: 10));
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost, str: 15));
            mz.Combine(new[] { z1, z2 });

            var stats = mz.GetStats();

            Assert(stats.Get(StatType.STR) == 25f, "STR should be sum of both zords");
            Assert(stats.Get(StatType.DEF) == 10f, "DEF should be sum of both zords");
        }

        // === Dual Type from Top Zords ===

        public void GetDualType_FromTwoHighestLevel()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze, recruitOrder: 0), level: 5);
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost, recruitOrder: 1), level: 3);
            var z3 = MakeInstance(MakeZord("z3", ElementalType.Gale, recruitOrder: 2), level: 8);
            mz.Combine(new[] { z1, z2, z3 });

            var dt = mz.GetDualType();

            // z3 (level 8) and z1 (level 5) are the top two
            Assert(dt.RangerType == ElementalType.Gale, "First type should be highest level (Gale)");
            Assert(dt.FormType == ElementalType.Blaze, "Second type should be second highest (Blaze)");
        }

        public void GetDualType_TieBreaker_EarlierRecruitWins()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze, recruitOrder: 0), level: 5);
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost, recruitOrder: 1), level: 5);
            mz.Combine(new[] { z1, z2 });

            var dt = mz.GetDualType();

            // Same level, z1 recruited first
            Assert(dt.RangerType == ElementalType.Blaze, "Earlier recruit should be first");
            Assert(dt.FormType == ElementalType.Frost, "Later recruit should be second");
        }

        public void GetDualType_SingleZord_SingleType()
        {
            var mz = new Megazord();
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });

            var dt = mz.GetDualType();

            Assert(dt.IsSameType, "Single zord should produce same-type");
            Assert(dt.RangerType == ElementalType.Blaze, "Should be the zord's type");
        }

        // === Hot-Swap ===

        public void SwapZord_ReturnsRemovedZord()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze));
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost));
            var z3 = MakeInstance(MakeZord("z3", ElementalType.Gale));
            mz.Combine(new[] { z1, z2 });

            var removed = mz.SwapZord(0, z3);

            Assert(removed == z1, "Should return the removed zord");
            Assert(mz.ComponentZords[0] == z3, "Slot should have new zord");
        }

        public void SwapZord_Fails_WhenNotCombined()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze));

            var result = mz.SwapZord(0, z1);

            Assert(result == null, "Should fail when not combined");
        }

        public void SwapZord_Fails_WhenZordAlreadySlotted()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze));
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost));
            mz.Combine(new[] { z1, z2 });

            var result = mz.SwapZord(0, z2); // z2 already in slot 1

            Assert(result == null, "Should fail when zord already slotted");
        }

        public void SwapZord_ChangesType()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze, recruitOrder: 0), level: 5);
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost, recruitOrder: 1), level: 3);
            var z3 = MakeInstance(MakeZord("z3", ElementalType.Gale, recruitOrder: 2), level: 10);
            mz.Combine(new[] { z1, z2 });

            var typeBefore = mz.GetDualType();
            mz.SwapZord(1, z3);
            var typeAfter = mz.GetDualType();

            // z3 at level 10 becomes the top zord
            Assert(typeAfter.RangerType == ElementalType.Gale, "Type should change after swap");
        }

        // === Ultimate ===

        public void ExecuteUltimate_Decombines()
        {
            var mz = new Megazord();
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });

            float multiplier = mz.ExecuteUltimate();

            Assert(!mz.IsCombined, "Should decombine after ultimate");
            Assert(mz.CombinationCooldown.IsOnCooldown, "Cooldown should start");
        }

        public void ExecuteUltimate_ReturnsBaseMultiplier_WhenNoHidden()
        {
            var mz = new Megazord();
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });

            float multiplier = mz.ExecuteUltimate();

            Assert(multiplier == 1.0f, "Should return 1.0 when no hidden multiplier");
        }

        public void ExecuteUltimate_ReturnsHiddenMultiplier_WhenMatched()
        {
            var mz = new Megazord();
            mz.RegisterHiddenMultiplier(new[] { "z1", "z2" }, 2.5f);

            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze));
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost));
            mz.Combine(new[] { z1, z2 });

            float multiplier = mz.ExecuteUltimate();

            Assert(multiplier == 2.5f, "Should return registered hidden multiplier");
        }

        public void ExecuteUltimate_Fails_WhenNotCombined()
        {
            var mz = new Megazord();

            float multiplier = mz.ExecuteUltimate();

            Assert(multiplier == 0f, "Should return 0 when not combined");
        }

        // === Preview Swap ===

        public void PreviewSwap_DoesNotModifyActualState()
        {
            var mz = new Megazord();
            var z1 = MakeInstance(MakeZord("z1", ElementalType.Blaze));
            var z2 = MakeInstance(MakeZord("z2", ElementalType.Frost));
            var z3 = MakeInstance(MakeZord("z3", ElementalType.Gale));
            mz.Combine(new[] { z1, z2 });

            var (previewStats, previewType) = mz.PreviewSwap(0, z3);

            Assert(mz.ComponentZords[0] == z1, "Original zord should still be in slot");
        }

        // === Cooldown ===

        public void CooldownTicksToAvailable()
        {
            var mz = new Megazord(combinationCooldownDuration: 3);
            mz.Combine(new[] { MakeInstance(MakeZord("z1", ElementalType.Blaze)) });
            mz.Decombine();

            mz.TickCooldown();
            mz.TickCooldown();
            mz.TickCooldown();

            Assert(mz.CombinationCooldown.IsAvailable, "Should be available after full cooldown");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new MegazordTests();
            tests.Combine_Succeeds_WhenAvailable();
            tests.Combine_Fails_WhenAlreadyCombined();
            tests.Combine_Fails_WhenOnCooldown();
            tests.Combine_Fails_WithEmptyZords();
            tests.TickTimer_CountsDown();
            tests.TickTimer_Decombines_WhenExpired();
            tests.GetStats_SumsAllComponentZords();
            tests.GetDualType_FromTwoHighestLevel();
            tests.GetDualType_TieBreaker_EarlierRecruitWins();
            tests.GetDualType_SingleZord_SingleType();
            tests.SwapZord_ReturnsRemovedZord();
            tests.SwapZord_Fails_WhenNotCombined();
            tests.SwapZord_Fails_WhenZordAlreadySlotted();
            tests.SwapZord_ChangesType();
            tests.ExecuteUltimate_Decombines();
            tests.ExecuteUltimate_ReturnsBaseMultiplier_WhenNoHidden();
            tests.ExecuteUltimate_ReturnsHiddenMultiplier_WhenMatched();
            tests.ExecuteUltimate_Fails_WhenNotCombined();
            tests.PreviewSwap_DoesNotModifyActualState();
            tests.CooldownTicksToAvailable();
            System.Console.WriteLine("MegazordTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
