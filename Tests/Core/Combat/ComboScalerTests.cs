using TokuTactics.Core.Combat;

namespace TokuTactics.Tests.Core.Combat
{
    public class ComboScalerTests
    {
        public void Initial_FullDamage()
        {
            var scaler = new ComboScaler();

            Assert(scaler.DamageMultiplier == 1.0f, "First action should be full damage");
            Assert(scaler.ChainCount == 0, "Chain count should be 0");
        }

        public void OneChain_ReducesDamage()
        {
            var scaler = new ComboScaler();

            scaler.AdvanceChain();

            Assert(scaler.ChainCount == 1, "Chain count should be 1");
            Assert(scaler.DamageMultiplier == 0.8f, "Second action should be 80%");
        }

        public void MultipleChains_ProgressivelyReduce()
        {
            var scaler = new ComboScaler();

            scaler.AdvanceChain(); // 0.8
            scaler.AdvanceChain(); // 0.6
            scaler.AdvanceChain(); // 0.4

            Assert(scaler.DamageMultiplier == 0.4f, "Fourth action should be 40%");
        }

        public void DeepChain_FloorsAtMinimum()
        {
            var scaler = new ComboScaler();

            for (int i = 0; i < 10; i++)
                scaler.AdvanceChain();

            Assert(scaler.DamageMultiplier == 0.1f, "Deep chain should floor at 10%");
        }

        public void StatusEffectMultiplier_AlwaysFull()
        {
            var scaler = new ComboScaler();

            for (int i = 0; i < 5; i++)
            {
                Assert(scaler.StatusEffectMultiplier == 1.0f,
                    $"Status effect should be 1.0 at chain {i}");
                scaler.AdvanceChain();
            }
        }

        public void AssistDamage_HigherThanActorButStillScaled()
        {
            var scaler = new ComboScaler();
            scaler.AdvanceChain(); // Actor at 0.8

            float actorDamage = scaler.DamageMultiplier;
            float assistDamage = scaler.AssistDamageMultiplier;

            Assert(assistDamage > actorDamage, "Assist should be higher than actor");
            Assert(assistDamage <= 1.0f, "Assist should not exceed full damage");
        }

        public void AssistDamage_AtFullChain_StillCapped()
        {
            var scaler = new ComboScaler();
            // No chains — full damage

            Assert(scaler.AssistDamageMultiplier <= 1.0f, "Assist should cap at 1.0");
        }

        public void AssistDamage_DeepChain_StillBetterThanActor()
        {
            var scaler = new ComboScaler();
            for (int i = 0; i < 5; i++)
                scaler.AdvanceChain();

            float actorDamage = scaler.DamageMultiplier;
            float assistDamage = scaler.AssistDamageMultiplier;

            Assert(assistDamage >= actorDamage, "Assist should always be >= actor damage");
        }

        public void Reset_RestoresFullDamage()
        {
            var scaler = new ComboScaler();
            scaler.AdvanceChain();
            scaler.AdvanceChain();

            scaler.ResetChain();

            Assert(scaler.DamageMultiplier == 1.0f, "Should be full damage after reset");
            Assert(scaler.ChainCount == 0, "Chain count should be 0");
        }

        public void Reset_ThenChain_ScalesFromFresh()
        {
            var scaler = new ComboScaler();
            scaler.AdvanceChain();
            scaler.AdvanceChain();
            scaler.ResetChain();

            scaler.AdvanceChain();

            Assert(scaler.DamageMultiplier == 0.8f, "Should scale from fresh after reset");
        }

        public void AssistScaleBonus_IsConfigurable()
        {
            var scaler = new ComboScaler();
            scaler.AssistScaleBonus = 1.5f;
            scaler.AdvanceChain(); // Actor at 0.8

            float assistDamage = scaler.AssistDamageMultiplier;

            Assert(assistDamage <= 1.0f, "Should still cap at 1.0");
            Assert(assistDamage > scaler.DamageMultiplier, "Should be higher than actor");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new ComboScalerTests();
            tests.Initial_FullDamage();
            tests.OneChain_ReducesDamage();
            tests.MultipleChains_ProgressivelyReduce();
            tests.DeepChain_FloorsAtMinimum();
            tests.StatusEffectMultiplier_AlwaysFull();
            tests.AssistDamage_HigherThanActorButStillScaled();
            tests.AssistDamage_AtFullChain_StillCapped();
            tests.AssistDamage_DeepChain_StillBetterThanActor();
            tests.Reset_RestoresFullDamage();
            tests.Reset_ThenChain_ScalesFromFresh();
            tests.AssistScaleBonus_IsConfigurable();
            System.Console.WriteLine("ComboScalerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
