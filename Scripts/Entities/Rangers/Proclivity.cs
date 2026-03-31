using System;
using TokuTactics.Core.Stats;

namespace TokuTactics.Entities.Rangers
{
    /// <summary>
    /// A Ranger's hidden stat proclivity. Determines which stat occasionally
    /// gets bonus points on form level-up, influenced by LCK.
    /// 
    /// Never communicated to the player. Randomized per fresh save.
    /// Persists through New Game+, rerolled on New Game++.
    /// </summary>
    public class Proclivity
    {
        private readonly Random _rng;

        /// <summary>The stat this Ranger has affinity for.</summary>
        public StatType AffinityStat { get; }

        /// <summary>Tunable: base chance of the bonus triggering (before LCK).</summary>
        public float BaseChance { get; set; } = 0.3f;

        /// <summary>Tunable: how much each point of LCK adds to trigger chance.</summary>
        public float LckScale { get; set; } = 0.005f;

        /// <summary>Tunable: bonus stat points when the proclivity triggers.</summary>
        public float BonusAmount { get; set; } = 1.0f;

        public Proclivity(StatType affinityStat, Random rng = null)
        {
            AffinityStat = affinityStat;
            _rng = rng ?? new Random();
        }

        /// <summary>
        /// Roll for a proclivity bonus on level-up.
        /// Returns the bonus amount if triggered, 0 if not.
        /// </summary>
        public float RollBonus(float rangerLck)
        {
            float chance = BaseChance + (rangerLck * LckScale);
            return _rng.NextDouble() < chance ? BonusAmount : 0f;
        }

        /// <summary>
        /// Assign random proclivities to a set of Rangers.
        /// Ensures one-to-one mapping: each stat is assigned to exactly one Ranger.
        /// </summary>
        public static StatType[] RandomAssignment(int rangerCount, Random rng = null)
        {
            rng ??= new Random();
            var stats = (StatType[])Enum.GetValues(typeof(StatType));
            var assignment = new StatType[rangerCount];

            // Shuffle stats
            var shuffled = new StatType[stats.Length];
            Array.Copy(stats, shuffled, stats.Length);
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // Assign one per Ranger (6 Rangers, 6 stats = perfect mapping)
            for (int i = 0; i < rangerCount && i < shuffled.Length; i++)
            {
                assignment[i] = shuffled[i];
            }

            return assignment;
        }
    }
}
