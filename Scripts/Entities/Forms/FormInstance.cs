using System;
using System.Collections.Generic;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Health;

namespace TokuTactics.Entities.Forms
{
    /// <summary>
    /// Per-Ranger mutable state for a specific form.
    /// FormData is the class template (shared). FormInstance is one Ranger's experience in it.
    /// Tracks level, damage/status scaling tracks, accumulated proclivity bonuses,
    /// and the independent health pool.
    /// 
    /// Each Ranger has one FormInstance per form they've used.
    /// </summary>
    public class FormInstance : IStatProvider
    {
        /// <summary>The form definition this instance is based on.</summary>
        public FormData Data { get; }

        /// <summary>Current level of this form for this specific Ranger.</summary>
        public int Level { get; private set; }

        /// <summary>Current experience toward next level.</summary>
        public int Experience { get; private set; }

        /// <summary>
        /// Investment in the damage scaling track.
        /// Each level-up adds 1 to either DamageTrack or StatusEffectTrack.
        /// </summary>
        public int DamageTrack { get; private set; }

        /// <summary>
        /// Investment in the status effect scaling track.
        /// Each level-up adds 1 to either DamageTrack or StatusEffectTrack.
        /// </summary>
        public int StatusEffectTrack { get; private set; }

        /// <summary>
        /// Accumulated proclivity bonus points per stat.
        /// These invisible bonuses build up over many level-ups, influenced by LCK.
        /// Applied on top of the form's level-derived stats in GetStats().
        /// </summary>
        private readonly Dictionary<StatType, float> _accumulatedBonuses = new();

        /// <summary>Read-only access to accumulated bonuses (for save/load).</summary>
        public IReadOnlyDictionary<StatType, float> AccumulatedBonuses => _accumulatedBonuses;

        /// <summary>
        /// This form's independent health pool. Persists across switches,
        /// regenerates during cooldown.
        /// </summary>
        public HealthPool Health { get; }

        /// <summary>Whether this form's health has been reduced to 0.</summary>
        public bool IsDead => !Health.IsAlive;

        /// <summary>Which weapon is currently equipped (A or B). Set at loadout.</summary>
        public WeaponSlot EquippedWeapon { get; set; } = WeaponSlot.A;

        public FormInstance(FormData data, int startingLevel = 1)
        {
            Data = data;
            Level = startingLevel;
            Experience = 0;
            DamageTrack = 0;
            StatusEffectTrack = 0;

            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
                _accumulatedBonuses[stat] = 0f;

            float maxHealth = CalculateMaxHealth();
            Health = new HealthPool(maxHealth);
        }

        /// <summary>
        /// Get the computed stats for this form at current level,
        /// including growth form scaling and accumulated proclivity bonuses.
        /// </summary>
        public StatBlock GetStats()
        {
            float levelMultiplier = Level - 1;

            // Growth forms have accelerating returns at higher levels
            if (Data.IsGrowthForm && Level > 5)
            {
                float growthBonus = (Level - 5) * (Data.GrowthCurveMultiplier - 1.0f);
                levelMultiplier += growthBonus;
            }

            var levelStats = Data.BaseStats.Add(Data.StatsPerLevel.Scale(levelMultiplier));

            // Apply accumulated proclivity bonuses
            return levelStats.Add(new StatBlock(_accumulatedBonuses));
        }

        /// <summary>
        /// Get the effective damage power, including base + damage track scaling.
        /// </summary>
        public float GetDamagePower()
        {
            float trackBonus = DamageTrack * 0.05f; // 5% per point, tunable
            return Data.BasicAttackPower * (1.0f + trackBonus);
        }

        /// <summary>
        /// Get the effective weapon power for the equipped weapon.
        /// </summary>
        public float GetWeaponPower()
        {
            var weapon = GetEquippedWeapon();
            float trackBonus = DamageTrack * 0.05f;
            return weapon.BasePower * (1.0f + trackBonus);
        }

        /// <summary>
        /// Get the status effect potency multiplier for weapon effects.
        /// Combines the status effect track investment with the form level.
        /// MAG scaling is applied externally by the combat system.
        /// </summary>
        public float GetStatusEffectPotency()
        {
            float trackBonus = StatusEffectTrack * 0.05f; // 5% per point, tunable
            return 1.0f + trackBonus;
        }

        /// <summary>
        /// Get the currently equipped weapon data.
        /// </summary>
        public Weapons.WeaponData GetEquippedWeapon()
        {
            return EquippedWeapon == WeaponSlot.A ? Data.WeaponA : Data.WeaponB;
        }

        /// <summary>
        /// Add experience. Returns true if leveled up.
        /// </summary>
        public bool AddExperience(int amount)
        {
            Experience += amount;
            int threshold = GetExperienceThreshold();

            if (Experience >= threshold)
            {
                Experience -= threshold;
                return true; // Caller handles the level-up choice
            }
            return false;
        }

        /// <summary>
        /// Apply a level up. Called after the player makes their damage/status choice.
        /// Proclivity bonus stat and amount come from Proclivity.RollBonus().
        /// </summary>
        public void LevelUp(LevelUpChoice choice, StatType? proclivityBonusStat = null, float bonusAmount = 0)
        {
            Level++;

            if (choice == LevelUpChoice.Damage)
                DamageTrack++;
            else
                StatusEffectTrack++;

            // Apply proclivity bonus if the roll succeeded
            if (proclivityBonusStat.HasValue && bonusAmount > 0)
            {
                _accumulatedBonuses[proclivityBonusStat.Value] += bonusAmount;
            }

            // Update health pool for new level
            float newMaxHealth = CalculateMaxHealth();
            Health.SetMaximum(newMaxHealth);
        }

        /// <summary>
        /// Restore accumulated bonuses from save data.
        /// </summary>
        public void RestoreAccumulatedBonuses(Dictionary<StatType, float> bonuses)
        {
            foreach (var kvp in bonuses)
            {
                _accumulatedBonuses[kvp.Key] = kvp.Value;
            }
        }

        private float CalculateMaxHealth()
        {
            float baseHp = Data.BaseHealth + (Data.HealthPerLevel * (Level - 1));
            // DEF contribution to health pool
            float defBonus = GetStats().Get(StatType.DEF) * 2.0f; // tunable multiplier
            return baseHp + defBonus;
        }

        private int GetExperienceThreshold()
        {
            return 100 + (Level * 20); // tunable curve
        }
    }

    public enum WeaponSlot
    {
        A,
        B
    }

    public enum LevelUpChoice
    {
        Damage,
        StatusEffect
    }
}
