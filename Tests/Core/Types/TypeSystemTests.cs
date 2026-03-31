using TokuTactics.Core.Types;

namespace TokuTactics.Tests.Core.Types
{
    public class TypeSystemTests
    {
        private TypeChart BuildTestChart()
        {
            var chart = new TypeChart();
            // Blaze > Frost > Gale > Blaze (triangle)
            chart.AddStrength(ElementalType.Blaze, ElementalType.Frost);
            chart.AddStrength(ElementalType.Frost, ElementalType.Gale);
            chart.AddStrength(ElementalType.Gale, ElementalType.Blaze);
            // Volt > Torrent
            chart.AddStrength(ElementalType.Volt, ElementalType.Torrent);
            return chart;
        }

        // === DualType Tests ===

        public void DualType_Single_BothTypesMatch()
        {
            var dt = DualType.Single(ElementalType.Blaze);

            Assert(dt.RangerType == ElementalType.Blaze, "Ranger type should be Blaze");
            Assert(dt.FormType == ElementalType.Blaze, "Form type should be Blaze");
            Assert(dt.IsSameType, "Single should be same-type");
        }

        public void DualType_MixedTypes_NotSameType()
        {
            var dt = new DualType(ElementalType.Blaze, ElementalType.Torrent);

            Assert(!dt.IsSameType, "Mixed types should not be same-type");
        }

        public void DualType_MatchingTypes_IsSameType()
        {
            var dt = new DualType(ElementalType.Frost, ElementalType.Frost);

            Assert(dt.IsSameType, "Matching types should be same-type");
        }

        // === Single Type Matchup Tests ===

        public void CheckSingle_StrongMatchup_ReturnsPositive()
        {
            var chart = BuildTestChart();

            int result = chart.CheckSingle(ElementalType.Blaze, ElementalType.Frost);

            Assert(result == 1, "Blaze should be strong against Frost");
        }

        public void CheckSingle_WeakMatchup_ReturnsNegative()
        {
            var chart = BuildTestChart();

            int result = chart.CheckSingle(ElementalType.Frost, ElementalType.Blaze);

            Assert(result == -1, "Frost should be weak against Blaze (Blaze is strong vs Frost)");
        }

        public void CheckSingle_NeutralMatchup_ReturnsZero()
        {
            var chart = BuildTestChart();

            int result = chart.CheckSingle(ElementalType.Blaze, ElementalType.Torrent);

            Assert(result == 0, "Blaze vs Torrent should be neutral");
        }

        public void CheckSingle_SameType_ReturnsZero()
        {
            var chart = BuildTestChart();

            int result = chart.CheckSingle(ElementalType.Blaze, ElementalType.Blaze);

            Assert(result == 0, "Same type should be neutral");
        }

        // === Dual Type Resolution Tests ===

        public void Resolve_BothStrong_DoubleStrong()
        {
            var chart = BuildTestChart();
            // Ranger=Blaze, Form=Blaze (same-type), both strong vs Frost
            var attacker = new DualType(ElementalType.Blaze, ElementalType.Blaze);

            var result = chart.Resolve(attacker, ElementalType.Frost);

            Assert(result == MatchupResult.DoubleStrong, "Both types strong should be DoubleStrong");
        }

        public void Resolve_BothWeak_DoubleWeak()
        {
            var chart = BuildTestChart();
            // Ranger=Frost, Form=Frost, both weak vs Blaze (Blaze is strong against Frost)
            // Wait — Frost is NOT weak vs Blaze. Blaze is strong vs Frost means Frost is weak vs Blaze.
            // Actually: CheckSingle(Frost, Blaze) checks if Frost attacks Blaze.
            // Blaze is strong against Frost, so Frost is weak against Blaze? No.
            // The strength relationship is Blaze ATTACKS Frost effectively.
            // If Frost attacks Blaze, that's Frost (attacker) vs Blaze (defender).
            // Blaze is strong against Frost means defending Blaze has advantage? No.
            // AddStrength(Blaze, Frost) means Blaze is strong ATTACKING Frost.
            // CheckSingle(Frost, Blaze) = is Frost strong vs Blaze? No.
            //   Is Blaze strong vs Frost? Yes. So Frost is WEAK when attacking Blaze.
            //   Returns -1. Correct.
            
            // For DoubleWeak: both Ranger and Form types should be weak vs defender.
            // Frost Ranger + Frost Form attacking Blaze:
            // CheckSingle(Frost, Blaze) = -1 (Frost weak vs Blaze)
            // CheckSingle(Frost, Blaze) = -1 (same again)
            // Total = -2 = DoubleWeak
            var attacker = new DualType(ElementalType.Frost, ElementalType.Frost);

            var result = chart.Resolve(attacker, ElementalType.Blaze);

            // Blaze strong vs Frost, so Frost attacking Blaze = weak
            // Both types Frost, both weak vs Blaze = DoubleWeak
            Assert(result == MatchupResult.DoubleWeak, "Both types weak should be DoubleWeak");
        }

        public void Resolve_OneStrongOneWeak_Neutral()
        {
            var chart = BuildTestChart();
            // Ranger=Blaze (strong vs Frost), Form=Gale (weak vs Frost, because Frost beats Gale)
            var attacker = new DualType(ElementalType.Blaze, ElementalType.Gale);

            var result = chart.Resolve(attacker, ElementalType.Frost);

            // Blaze vs Frost = +1 (strong)
            // Gale vs Frost = -1 (Frost beats Gale)
            // Total = 0 = Neutral
            Assert(result == MatchupResult.Neutral, "Strong + Weak should cancel to Neutral");
        }

        public void Resolve_OneStrongOneNeutral_Strong()
        {
            var chart = BuildTestChart();
            // Ranger=Blaze (strong vs Frost), Form=Torrent (neutral vs Frost)
            var attacker = new DualType(ElementalType.Blaze, ElementalType.Torrent);

            var result = chart.Resolve(attacker, ElementalType.Frost);

            // Blaze vs Frost = +1
            // Torrent vs Frost = 0
            // Total = +1 = Strong
            Assert(result == MatchupResult.Strong, "Strong + Neutral should be Strong");
        }

        public void Resolve_OneWeakOneNeutral_Weak()
        {
            var chart = BuildTestChart();
            // Ranger=Gale (weak vs Frost), Form=Torrent (neutral vs Frost)
            var attacker = new DualType(ElementalType.Gale, ElementalType.Torrent);

            var result = chart.Resolve(attacker, ElementalType.Frost);

            // Gale vs Frost = -1 (Frost beats Gale)
            // Torrent vs Frost = 0
            // Total = -1 = Weak
            Assert(result == MatchupResult.Weak, "Weak + Neutral should be Weak");
        }

        public void Resolve_BothNeutral_Neutral()
        {
            var chart = BuildTestChart();
            var attacker = new DualType(ElementalType.Shadow, ElementalType.Radiant);

            var result = chart.Resolve(attacker, ElementalType.Frost);

            Assert(result == MatchupResult.Neutral, "Both neutral should be Neutral");
        }

        // === Same-Type Bonus Tests ===

        public void IsSameTypeBonus_MatchingTypes_True()
        {
            var chart = BuildTestChart();
            var dt = new DualType(ElementalType.Blaze, ElementalType.Blaze);

            Assert(chart.IsSameTypeBonus(dt), "Same types should give bonus");
        }

        public void IsSameTypeBonus_DifferentTypes_False()
        {
            var chart = BuildTestChart();
            var dt = new DualType(ElementalType.Blaze, ElementalType.Frost);

            Assert(!chart.IsSameTypeBonus(dt), "Different types should not give bonus");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new TypeSystemTests();
            tests.DualType_Single_BothTypesMatch();
            tests.DualType_MixedTypes_NotSameType();
            tests.DualType_MatchingTypes_IsSameType();
            tests.CheckSingle_StrongMatchup_ReturnsPositive();
            tests.CheckSingle_WeakMatchup_ReturnsNegative();
            tests.CheckSingle_NeutralMatchup_ReturnsZero();
            tests.CheckSingle_SameType_ReturnsZero();
            tests.Resolve_BothStrong_DoubleStrong();
            tests.Resolve_BothWeak_DoubleWeak();
            tests.Resolve_OneStrongOneWeak_Neutral();
            tests.Resolve_OneStrongOneNeutral_Strong();
            tests.Resolve_OneWeakOneNeutral_Weak();
            tests.Resolve_BothNeutral_Neutral();
            tests.IsSameTypeBonus_MatchingTypes_True();
            tests.IsSameTypeBonus_DifferentTypes_False();
            System.Console.WriteLine("TypeSystemTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
