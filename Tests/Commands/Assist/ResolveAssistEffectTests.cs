using System;
using TokuTactics.Commands.Assist;
using TokuTactics.Core.Types;
using TokuTactics.Systems.ActionEconomy;
using System.Collections.Generic;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Tests.Commands.Assist
{
    public static class ResolveAssistEffectTests
    {
        public static void Run()
        {
            Test_BasicAssist_SetsFields();
            Test_Tier2_SetsFormDisruption();
            Test_Tier3_NoPairDisruption();
            Test_Tier4_SetsRefresh();
            Test_UsesInjectedBricks();
            Console.WriteLine("ResolveAssistEffectTests: All passed");
        }

        private static void Test_BasicAssist_SetsFields()
        {
            var bond = MakeBond(0);
            var assister = MakeState("r2", "form_blaze", str: 10f, cha: 1.2f);
            var attacker = MakeState("r1", "form_base");

            var effect = ResolveAssistEffect.Execute(
                "r1", "r2", bond, assister, attacker, 1.0f, 1.25f, 1.5f);

            Assert(effect.AttackerId == "r1", "AttackerId should be r1");
            Assert(effect.AssisterId == "r2", "AssisterId should be r2");
            Assert(effect.AssisterStr == 10f, $"STR should be 10, got {effect.AssisterStr}");
            Assert(Math.Abs(effect.ChaMultiplier - 1.2f) < 0.001f, "CHA should be 1.2");
            Assert(!effect.IsPairAttack, "Tier 0 should not be pair attack");
            Assert(!effect.ForceToBaseForm, "Tier 0 should not disrupt");
            Assert(!effect.CanRefreshPartner, "Tier 0 should not refresh");
        }

        private static void Test_Tier2_SetsFormDisruption()
        {
            var bond = MakeBond(2);
            var assister = MakeState("r2", "form_blaze", baseFormId: "form_base");
            var attacker = MakeState("r1", "form_base");

            var effect = ResolveAssistEffect.Execute(
                "r1", "r2", bond, assister, attacker, 1.0f, 1.25f, 1.5f);

            Assert(effect.IsPairAttack, "Tier 2 should be pair attack");
            Assert(effect.ForceToBaseForm, "Tier 2 non-base should disrupt");
            Assert(effect.VacatedFormId == "form_blaze", $"Should vacate form_blaze, got {effect.VacatedFormId}");
            Assert(effect.AssisterFormId == "form_base", $"Should use base form, got {effect.AssisterFormId}");
        }

        private static void Test_Tier3_NoPairDisruption()
        {
            var bond = MakeBond(3);
            var assister = MakeState("r2", "form_blaze");
            var attacker = MakeState("r1", "form_base");

            var effect = ResolveAssistEffect.Execute(
                "r1", "r2", bond, assister, attacker, 1.0f, 1.25f, 1.5f);

            Assert(effect.IsPairAttack, "Tier 3 should be pair attack");
            Assert(!effect.ForceToBaseForm, "Tier 3 should not disrupt");
            Assert(effect.AssisterFormId == "form_blaze", "Should keep current form");
        }

        private static void Test_Tier4_SetsRefresh()
        {
            var bond = MakeBond(4);
            var assister = MakeState("r2", "form_blaze");
            var attacker = MakeState("r1", "form_base");

            var effect = ResolveAssistEffect.Execute(
                "r1", "r2", bond, assister, attacker, 1.0f, 1.25f, 1.5f);

            Assert(effect.CanRefreshPartner, "Tier 4 should enable refresh");
        }

        private static void Test_UsesInjectedBricks()
        {
            var bond = MakeBond(2);
            var assister = MakeState("r2", "form_blaze");
            var attacker = MakeState("r1", "form_base");
            bool dmgCalled = false, t2Called = false, t4Called = false;

            ResolveAssistEffect.Execute(
                "r1", "r2", bond, assister, attacker, 1.0f, 1.25f, 1.5f,
                calculateDamageMultiplier: (t, c, b1, b2) => { dmgCalled = true; return 1.0f; },
                resolveTier2: (t, s) => { t2Called = true; return null; },
                checkTier4: (t, a, ak) => { t4Called = true; return false; });

            Assert(dmgCalled, "Should call injected damage multiplier");
            Assert(t2Called, "Should call injected tier 2");
            Assert(t4Called, "Should call injected tier 4");
        }

        private static AssistCandidateState MakeState(
            string id, string formId, string baseFormId = "form_base",
            float str = 8f, float cha = 1.0f)
        {
            return new AssistCandidateState
            {
                IsMorphed = true,
                CurrentFormId = formId,
                BaseFormId = baseFormId,
                IsInBaseForm = formId == baseFormId,
                WeaponBasePower = 1.0f,
                Str = str,
                Cha = cha,
                AssisterDualType = DualType.Single(ElementalType.Blaze)
            };
        }

        private static BondState MakeBond(int tier)
        {
            var tracker = new BondTracker();
            // Thresholds: { 50, 150, 350, 700 } — add enough XP to reach desired tier
            tracker.TierThresholds = new[] { 1, 2, 3, 4 };
            var bond = tracker.GetBond("a", "b");
            for (int i = 0; i < tier; i++)
                bond.AddExperience(1, tracker.TierThresholds);
            return bond;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
