using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Entities.Forms
{
    /// <summary>
    /// Immutable data definition for a form — the "class template."
    /// This is shared across all Rangers. It defines what a form IS.
    /// Per-Ranger state (level, health, damage/status track) lives in FormInstance.
    /// 
    /// Think of FormData as the job description, FormInstance as the Ranger's experience in that job.
    /// </summary>
    public class FormData
    {
        /// <summary>Unique identifier.</summary>
        public string Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>This form's elemental type. Combined with Ranger type for dual typing.</summary>
        public ElementalType Type { get; }

        /// <summary>Base stats at level 1. Scaled by level in FormInstance.</summary>
        public StatBlock BaseStats { get; }

        /// <summary>Stats gained per level. Applied multiplicatively in FormInstance.</summary>
        public StatBlock StatsPerLevel { get; }

        /// <summary>Base health pool at level 1. Scales with level and DEF.</summary>
        public float BaseHealth { get; }

        /// <summary>Health gained per level.</summary>
        public float HealthPerLevel { get; }

        /// <summary>Movement range in tiles. Entirely form-driven.</summary>
        public int MovementRange { get; }

        /// <summary>Base attack range. Determines melee (1) vs ranged (2+).</summary>
        public int BasicAttackRange { get; }

        /// <summary>Base power of the form's inherent basic attack.</summary>
        public float BasicAttackPower { get; }

        /// <summary>Cooldown duration in turns when this form is vacated. Modified by MAG at activation.</summary>
        public int CooldownDuration { get; }

        /// <summary>First weapon option.</summary>
        public WeaponData WeaponA { get; }

        /// <summary>Second weapon option.</summary>
        public WeaponData WeaponB { get; }

        /// <summary>
        /// Whether this is a growth form — weak at level 1, highest ceiling at max level.
        /// Hidden from the player, community discovery only.
        /// </summary>
        public bool IsGrowthForm { get; }

        /// <summary>
        /// Growth forms have a different (steeper) stat curve.
        /// This multiplier is applied to StatsPerLevel at higher levels.
        /// </summary>
        public float GrowthCurveMultiplier { get; }

        public FormData(
            string id,
            string name,
            ElementalType type,
            StatBlock baseStats,
            StatBlock statsPerLevel,
            float baseHealth,
            float healthPerLevel,
            int movementRange,
            int basicAttackRange,
            float basicAttackPower,
            int cooldownDuration,
            WeaponData weaponA,
            WeaponData weaponB,
            bool isGrowthForm = false,
            float growthCurveMultiplier = 1.0f)
        {
            Id = id;
            Name = name;
            Type = type;
            BaseStats = baseStats;
            StatsPerLevel = statsPerLevel;
            BaseHealth = baseHealth;
            HealthPerLevel = healthPerLevel;
            MovementRange = movementRange;
            BasicAttackRange = basicAttackRange;
            BasicAttackPower = basicAttackPower;
            CooldownDuration = cooldownDuration;
            WeaponA = weaponA;
            WeaponB = weaponB;
            IsGrowthForm = isGrowthForm;
            GrowthCurveMultiplier = growthCurveMultiplier;
        }
    }
}
