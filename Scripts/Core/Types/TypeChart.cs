using System.Collections.Generic;

namespace TokuTactics.Core.Types
{
    /// <summary>
    /// Data-driven type matchup chart. Defines which types are strong/weak against which.
    /// Configure via AddStrength — everything not defined is Neutral.
    /// This is the single source of truth for type interactions.
    /// </summary>
    public class TypeChart
    {
        // Key: attacker type, Value: set of types it's strong against
        private readonly Dictionary<ElementalType, HashSet<ElementalType>> _strengths = new();

        /// <summary>
        /// Declare that attackerType is strong against defenderType.
        /// The inverse (defenderType is weak against attackerType) is implicit.
        /// </summary>
        public void AddStrength(ElementalType attackerType, ElementalType defenderType)
        {
            if (!_strengths.ContainsKey(attackerType))
                _strengths[attackerType] = new HashSet<ElementalType>();

            _strengths[attackerType].Add(defenderType);
        }

        /// <summary>
        /// Check a single type vs single type matchup.
        /// Returns +1 for strong, -1 for weak, 0 for neutral.
        /// </summary>
        public int CheckSingle(ElementalType attackerType, ElementalType defenderType)
        {
            // Attacker is strong against defender
            if (_strengths.ContainsKey(attackerType) && _strengths[attackerType].Contains(defenderType))
                return 1;

            // Defender is strong against attacker (meaning attacker is weak)
            if (_strengths.ContainsKey(defenderType) && _strengths[defenderType].Contains(attackerType))
                return -1;

            return 0;
        }

        /// <summary>
        /// Resolve a dual type attacker vs a single type defender.
        /// Sums both type interactions: +2 = DoubleStrong, -2 = DoubleWeak,
        /// +1 = Strong, -1 = Weak, 0 = Neutral (including cancellation).
        /// </summary>
        public MatchupResult Resolve(DualType attacker, ElementalType defenderType)
        {
            int rangerResult = CheckSingle(attacker.RangerType, defenderType);
            int formResult = CheckSingle(attacker.FormType, defenderType);
            int total = rangerResult + formResult;

            return total switch
            {
                >= 2 => MatchupResult.DoubleStrong,
                1 => MatchupResult.Strong,
                0 => MatchupResult.Neutral,
                -1 => MatchupResult.Weak,
                _ => MatchupResult.DoubleWeak
            };
        }

        /// <summary>
        /// Resolve how much damage a dual-typed defender takes from a single-typed attacker.
        /// This is the inverse of Resolve() — use when evaluating incoming damage.
        /// 
        /// Usage guide:
        /// - Resolve(attacker, defenderType): "How effective am I attacking this enemy?"
        ///   Used for damage calculation, matchup preview UI.
        /// - ResolveDefensive(attackerType, defender): "How vulnerable am I to this attack?"
        ///   Used for AI threat assessment, form selection when being targeted.
        /// </summary>
        public MatchupResult ResolveDefensive(ElementalType attackerType, DualType defender)
        {
            // Check how the attacker's type interacts with each of the defender's types
            int vsRangerType = CheckSingle(attackerType, defender.RangerType);
            int vsFormType = CheckSingle(attackerType, defender.FormType);
            int total = vsRangerType + vsFormType;

            // From the defender's perspective, attacker being strong is BAD
            // So we invert: attacker strong (+) means defender is weak
            return total switch
            {
                >= 2 => MatchupResult.DoubleWeak,   // Attacker strong vs both = defender very vulnerable
                1 => MatchupResult.Weak,              // Attacker strong vs one = defender vulnerable
                0 => MatchupResult.Neutral,
                -1 => MatchupResult.Strong,           // Attacker weak vs one = defender resistant
                _ => MatchupResult.DoubleStrong       // Attacker weak vs both = defender very resistant
            };
        }

        /// <summary>
        /// Check if a Ranger type matches their form type for same-type bonus.
        /// Separate from matchup resolution — this is a damage multiplier.
        /// Same-type bonus is a double-edged sword: increased offense but doubled vulnerability.
        /// </summary>
        public bool IsSameTypeBonus(DualType dualType) => dualType.IsSameType;
    }
}
