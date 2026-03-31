using System;
using System.Collections.Generic;

namespace TokuTactics.Core.Stats
{
    /// <summary>
    /// Immutable container for a complete set of stat values.
    /// Used as the base representation for form stats, modifiers, and computed totals.
    /// Create new blocks via Add/Scale operations — never mutate.
    /// </summary>
    public sealed class StatBlock
    {
        private readonly Dictionary<StatType, float> _values;

        public StatBlock()
        {
            _values = new Dictionary<StatType, float>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                _values[stat] = 0f;
            }
        }

        public StatBlock(Dictionary<StatType, float> values)
        {
            _values = new Dictionary<StatType, float>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                _values[stat] = values.ContainsKey(stat) ? values[stat] : 0f;
            }
        }

        public float Get(StatType stat) => _values[stat];

        /// <summary>
        /// Returns a new StatBlock with another block's values added.
        /// </summary>
        public StatBlock Add(StatBlock other)
        {
            var result = new Dictionary<StatType, float>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                result[stat] = _values[stat] + other._values[stat];
            }
            return new StatBlock(result);
        }

        /// <summary>
        /// Returns a new StatBlock with all values scaled by a multiplier.
        /// </summary>
        public StatBlock Scale(float multiplier)
        {
            var result = new Dictionary<StatType, float>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                result[stat] = _values[stat] * multiplier;
            }
            return new StatBlock(result);
        }

        /// <summary>
        /// Returns a new StatBlock with a single stat modified by a flat amount.
        /// </summary>
        public StatBlock WithBonus(StatType stat, float bonus)
        {
            var result = new Dictionary<StatType, float>(_values);
            result[stat] = _values[stat] + bonus;
            return new StatBlock(result);
        }

        /// <summary>
        /// Create a StatBlock from individual values.
        /// </summary>
        public static StatBlock Create(
            float str = 0, float def = 0, float spd = 0,
            float mag = 0, float cha = 0, float lck = 0)
        {
            return new StatBlock(new Dictionary<StatType, float>
            {
                { StatType.STR, str },
                { StatType.DEF, def },
                { StatType.SPD, spd },
                { StatType.MAG, mag },
                { StatType.CHA, cha },
                { StatType.LCK, lck }
            });
        }
    }
}
