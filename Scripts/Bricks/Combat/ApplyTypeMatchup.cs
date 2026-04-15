using TokuTactics.Commands.Combat;
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
        /// <param name="attackType">Attacker's elemental type</param>
        /// <param name="defenderType">Defender's single type (for enemies)</param>
        /// <param name="defenderDualType">Defender's dual type (for Rangers), null for enemies</param>
        /// <param name="typeChart">Type chart for matchup resolution</param>
        /// <param name="constants">Tunable constants containing type matchup multipliers</param>
        /// <returns>Result containing modified damage, matchup type, and multiplier used</returns>
        public static Result Execute(
            float baseDamage,
            ElementalType attackType,
            ElementalType defenderType,
            DualType? defenderDualType,
            TypeChart typeChart,
            TunableConstants constants)
        {
            MatchupResult matchup;

            // Determine matchup based on defender type structure
            if (defenderDualType != null)
            {
                // Attacker (single type) → Ranger (dual type)
                // Check matchup against both defender types and sum
                int vsRangerType = typeChart.CheckSingle(attackType, defenderDualType.Value.RangerType);
                int vsFormType = typeChart.CheckSingle(attackType, defenderDualType.Value.FormType);
                int total = vsRangerType + vsFormType;

                matchup = total switch
                {
                    >= 2 => MatchupResult.DoubleStrong,
                    1 => MatchupResult.Strong,
                    0 => MatchupResult.Neutral,
                    -1 => MatchupResult.Weak,
                    <= -2 => MatchupResult.DoubleWeak
                };
            }
            else
            {
                // Attacker (single type) → Enemy (single type)
                // Check single type matchup (strong/weak/neutral only, no double-strength)
                int strength = typeChart.CheckSingle(attackType, defenderType);

                matchup = strength switch
                {
                    1 => MatchupResult.Strong,
                    -1 => MatchupResult.Weak,
                    _ => MatchupResult.Neutral
                };
            }

            // Convert matchup to multiplier using constants
            float multiplier = matchup switch
            {
                MatchupResult.DoubleStrong => constants.DoubleStrongMultiplier,
                MatchupResult.Strong => constants.StrongMultiplier,
                MatchupResult.Neutral => 1.0f,
                MatchupResult.Weak => constants.WeakMultiplier,
                MatchupResult.DoubleWeak => constants.DoubleWeakMultiplier,
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
