using TokuTactics.Core.Types;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Applies type effectiveness multiplier to damage based on type chart matchup.
    /// Handles both single-type (enemy) and dual-type (Ranger) defenders.
    /// Pure calculation with no side effects.
    /// </summary>
    public static class ApplyTypeMatchup
    {
        /// <summary>
        /// Result of type matchup application including modified damage and matchup metadata.
        /// </summary>
        public struct Result
        {
            public float Damage;
            public MatchupResult Matchup;
            public float Multiplier;
        }

        /// <summary>
        /// Executes the type matchup calculation.
        /// </summary>
        /// <param name="baseDamage">Damage before type effectiveness</param>
        /// <param name="attackerDualType">Attacker's dual type (innate + form)</param>
        /// <param name="defenderType">Defender's single type (for enemies)</param>
        /// <param name="defenderDualType">Defender's dual type (for Rangers), null for enemies</param>
        /// <param name="typeChart">Type chart for matchup resolution</param>
        /// <param name="strongMultiplier">Multiplier for strong matchups</param>
        /// <param name="weakMultiplier">Multiplier for weak matchups</param>
        /// <param name="doubleStrongMultiplier">Multiplier for double-strong matchups</param>
        /// <param name="doubleWeakMultiplier">Multiplier for double-weak matchups</param>
        /// <returns>Result containing modified damage, matchup type, and multiplier used</returns>
        public static Result Execute(
            float baseDamage,
            DualType attackerDualType,
            ElementalType defenderType,
            DualType? defenderDualType,
            TypeChart typeChart,
            float strongMultiplier,
            float weakMultiplier,
            float doubleStrongMultiplier,
            float doubleWeakMultiplier)
        {
            MatchupResult matchup;

            // Determine matchup based on defender type structure
            if (defenderDualType != null)
            {
                // Enemy (single type via DualType.RangerType) → Ranger (dual type)
                matchup = typeChart.ResolveDefensive(attackerDualType.RangerType, defenderDualType.Value);
            }
            else
            {
                // Ranger (dual type) → Enemy (single type)
                matchup = typeChart.Resolve(attackerDualType, defenderType);
            }

            // Convert matchup to multiplier
            float multiplier = matchup switch
            {
                MatchupResult.DoubleStrong => doubleStrongMultiplier,
                MatchupResult.Strong => strongMultiplier,
                MatchupResult.Neutral => 1.0f,
                MatchupResult.Weak => weakMultiplier,
                MatchupResult.DoubleWeak => doubleWeakMultiplier,
                _ => 1.0f
            };

            return new Result
            {
                Damage = baseDamage * multiplier,
                Matchup = matchup,
                Multiplier = multiplier
            };
        }
    }
}
