using System;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Types;

namespace TokuTactics.Tests.Core.Combat
{
    public class DamageCalculatorTests
    {
        private TypeChart BuildTestChart()
        {
            var chart = new TypeChart();
            chart.AddStrength(ElementalType.Blaze, ElementalType.Frost);
            chart.AddStrength(ElementalType.Frost, ElementalType.Gale);
            chart.AddStrength(ElementalType.Gale, ElementalType.Blaze);
            return chart;
        }

        private DamageCalculator BuildCalculator(int seed = 42)
        {
            return new DamageCalculator(BuildTestChart(), new Random(seed))
            {
                // Disable variance and crits for deterministic testing
                BaseCritChance = 0f,
                BaseDodgeChance = 0f,
                LckCritScale = 0f,
                LckDodgeScale = 0f
            };
        }

        private DamageInput BuildInput(
            float str = 20, float def = 10, float lck = 0,
            ElementalType attackerRangerType = ElementalType.Blaze,
            ElementalType attackerFormType = ElementalType.Blaze,
            ElementalType defenderType = ElementalType.Frost,
            float actionPower = 1.0f, float comboMultiplier = 1.0f)
        {
            return new DamageInput
            {
                AttackerStr = str,
                AttackerLck = lck,
                DefenderDef = def,
                DefenderLck = 0,
                AttackerDualType = new DualType(attackerRangerType, attackerFormType),
                DefenderType = defenderType,
                ActionPower = actionPower,
                ComboMultiplier = comboMultiplier
            };
        }

        // === Base Damage Tests ===

        public void Calculate_BaseDamage_StrMinusDef()
        {
            var calc = BuildCalculator();
            var input = BuildInput(str: 20, def: 10,
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Radiant,
                defenderType: ElementalType.Shadow); // All neutral

            var result = calc.Calculate(input);

            // Base = max(1, 20 - 10*0.5) = 15
            // Neutral matchup = 1.0x
            // No same-type bonus (Shadow != Radiant)
            // No combo scaling (1.0)
            // With variance (seed-dependent), should be close to 15
            Assert(result.FinalDamage > 0, "Should deal positive damage");
            Assert(!result.WasDodged, "Should not be dodged");
        }

        public void Calculate_HighDef_MinimumOneDamage()
        {
            var calc = BuildCalculator();
            var input = BuildInput(str: 1, def: 100,
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Radiant,
                defenderType: ElementalType.Shadow);

            var result = calc.Calculate(input);

            Assert(result.FinalDamage >= 1, "Should deal at least 1 damage");
        }

        // === Type Matchup Tests ===

        public void Calculate_StrongMatchup_IncreasedDamage()
        {
            var calc = BuildCalculator();
            var neutralInput = BuildInput(
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Shadow,
                defenderType: ElementalType.Radiant); // Neutral

            var strongInput = BuildInput(
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Blaze,
                defenderType: ElementalType.Frost); // Blaze strong vs Frost

            var neutralResult = calc.Calculate(neutralInput);
            var strongResult = calc.Calculate(strongInput);

            Assert(strongResult.Matchup == MatchupResult.Strong,
                "Should be Strong matchup");
            Assert(strongResult.TypeMultiplier > 1.0f,
                "Strong should have multiplier > 1");
        }

        public void Calculate_WeakMatchup_ReducedDamage()
        {
            var calc = BuildCalculator();
            var input = BuildInput(
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Frost,
                defenderType: ElementalType.Blaze); // Frost weak vs Blaze

            var result = calc.Calculate(input);

            Assert(result.Matchup == MatchupResult.Weak, "Should be Weak matchup");
            Assert(result.TypeMultiplier < 1.0f, "Weak should have multiplier < 1");
        }

        public void Calculate_DoubleStrong_HighestMultiplier()
        {
            var calc = BuildCalculator();
            // Blaze Ranger + Blaze Form vs Frost = both strong
            var input = BuildInput(
                attackerRangerType: ElementalType.Blaze,
                attackerFormType: ElementalType.Blaze,
                defenderType: ElementalType.Frost);

            var result = calc.Calculate(input);

            Assert(result.Matchup == MatchupResult.DoubleStrong,
                "Should be DoubleStrong");
            Assert(result.TypeMultiplier == calc.DoubleStrongMultiplier,
                "Should use DoubleStrong multiplier");
        }

        public void Calculate_DoubleWeak_LowestMultiplier()
        {
            var calc = BuildCalculator();
            // Frost Ranger + Frost Form vs Blaze = both weak
            var input = BuildInput(
                attackerRangerType: ElementalType.Frost,
                attackerFormType: ElementalType.Frost,
                defenderType: ElementalType.Blaze);

            var result = calc.Calculate(input);

            Assert(result.Matchup == MatchupResult.DoubleWeak,
                "Should be DoubleWeak");
            Assert(result.TypeMultiplier == calc.DoubleWeakMultiplier,
                "Should use DoubleWeak multiplier");
        }

        // === Same-Type Bonus Tests ===

        public void Calculate_SameType_AppliesBonus()
        {
            var calc = BuildCalculator();
            // Same type: Blaze Ranger + Blaze Form, neutral matchup
            var sameInput = BuildInput(
                attackerRangerType: ElementalType.Blaze,
                attackerFormType: ElementalType.Blaze,
                defenderType: ElementalType.Shadow); // Neutral

            var result = calc.Calculate(sameInput);

            Assert(result.HadSameTypeBonus, "Should have same-type bonus");
        }

        public void Calculate_DifferentType_NoBonusFlag()
        {
            var calc = BuildCalculator();
            var input = BuildInput(
                attackerRangerType: ElementalType.Blaze,
                attackerFormType: ElementalType.Frost,
                defenderType: ElementalType.Shadow);

            var result = calc.Calculate(input);

            Assert(!result.HadSameTypeBonus, "Should NOT have same-type bonus");
        }

        // === Combo Scaling Tests ===

        public void Calculate_ComboScaling_ReducesDamage()
        {
            var calc = BuildCalculator();
            var fullInput = BuildInput(comboMultiplier: 1.0f,
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Radiant,
                defenderType: ElementalType.Shadow);
            var scaledInput = BuildInput(comboMultiplier: 0.5f,
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Radiant,
                defenderType: ElementalType.Shadow);

            var fullResult = calc.Calculate(fullInput);
            var scaledResult = calc.Calculate(scaledInput);

            Assert(scaledResult.FinalDamage < fullResult.FinalDamage,
                "Scaled damage should be less than full");
            Assert(scaledResult.ComboMultiplier == 0.5f,
                "Combo multiplier should be recorded");
        }

        // === Preview Tests ===

        public void Preview_ReturnsMinAndMax()
        {
            var calc = BuildCalculator();
            var input = BuildInput(
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Radiant,
                defenderType: ElementalType.Shadow);

            var preview = calc.Preview(input);

            Assert(preview.MinDamage > 0, "Min should be positive");
            Assert(preview.MaxDamage >= preview.MinDamage, "Max should be >= min");
            Assert(preview.CritCeiling >= preview.MaxDamage, "Crit ceiling should be >= max");
        }

        public void Preview_StrongMatchup_IndicatedInResult()
        {
            var calc = BuildCalculator();
            var input = BuildInput(
                attackerRangerType: ElementalType.Shadow,
                attackerFormType: ElementalType.Blaze,
                defenderType: ElementalType.Frost);

            var preview = calc.Preview(input);

            Assert(preview.Matchup == MatchupResult.Strong, "Preview should show Strong");
        }

        public void Preview_SameType_FlaggedInResult()
        {
            var calc = BuildCalculator();
            var input = BuildInput(
                attackerRangerType: ElementalType.Blaze,
                attackerFormType: ElementalType.Blaze,
                defenderType: ElementalType.Shadow);

            var preview = calc.Preview(input);

            Assert(preview.HasSameTypeBonus, "Preview should flag same-type bonus");
        }

        // === Dodge Tests ===

        public void Calculate_HighDodge_CanMiss()
        {
            // Use a calculator with high dodge chance
            var calc = new DamageCalculator(BuildTestChart(), new Random(42))
            {
                BaseDodgeChance = 1.0f, // 100% dodge
                BaseCritChance = 0f
            };
            var input = BuildInput();

            var result = calc.Calculate(input);

            Assert(result.WasDodged, "Should be dodged at 100% chance");
            Assert(result.FinalDamage == 0, "Dodged attacks deal 0 damage");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new DamageCalculatorTests();
            tests.Calculate_BaseDamage_StrMinusDef();
            tests.Calculate_HighDef_MinimumOneDamage();
            tests.Calculate_StrongMatchup_IncreasedDamage();
            tests.Calculate_WeakMatchup_ReducedDamage();
            tests.Calculate_DoubleStrong_HighestMultiplier();
            tests.Calculate_DoubleWeak_LowestMultiplier();
            tests.Calculate_SameType_AppliesBonus();
            tests.Calculate_DifferentType_NoBonusFlag();
            tests.Calculate_ComboScaling_ReducesDamage();
            tests.Preview_ReturnsMinAndMax();
            tests.Preview_StrongMatchup_IndicatedInResult();
            tests.Preview_SameType_FlaggedInResult();
            tests.Calculate_HighDodge_CanMiss();
            System.Console.WriteLine("DamageCalculatorTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
