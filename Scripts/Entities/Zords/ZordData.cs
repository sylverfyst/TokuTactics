using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.Health;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Entities.Zords
{
    /// <summary>
    /// Immutable data definition for a zord.
    /// Parallels FormData — defines what a zord IS.
    /// ZordInstance tracks per-Ranger state (level, health).
    /// </summary>
    public class ZordData
    {
        public string Id { get; }
        public string Name { get; }
        public ElementalType Type { get; }
        public StatBlock BaseStats { get; }
        public StatBlock StatsPerLevel { get; }
        public float BaseHealth { get; }
        public float HealthPerLevel { get; }
        public int MovementRange { get; }
        public int BasicAttackRange { get; }
        public float BasicAttackPower { get; }

        /// <summary>The zord's personal ability that affects the map.</summary>
        public IPersonalAbility PersonalAbility { get; }

        /// <summary>Whether this is a growth zord (hidden, community discovery).</summary>
        public bool IsGrowthZord { get; }
        public float GrowthCurveMultiplier { get; }

        /// <summary>Order of recruitment — used as tiebreaker for Megazord type.</summary>
        public int RecruitmentOrder { get; set; }

        public ZordData(
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
            IPersonalAbility personalAbility,
            bool isGrowthZord = false,
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
            PersonalAbility = personalAbility;
            IsGrowthZord = isGrowthZord;
            GrowthCurveMultiplier = growthCurveMultiplier;
        }
    }

    /// <summary>
    /// Per-Ranger zord state. Tracks level and health.
    /// Parallels FormInstance.
    /// </summary>
    public class ZordInstance : IStatProvider
    {
        public ZordData Data { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public HealthPool Health { get; }

        public ZordInstance(ZordData data, int startingLevel = 1)
        {
            Data = data;
            Level = startingLevel;
            Experience = 0;

            float maxHealth = data.BaseHealth + (data.HealthPerLevel * (startingLevel - 1));
            Health = new HealthPool(maxHealth);
        }

        public StatBlock GetStats()
        {
            float levelMultiplier = Level - 1;

            if (Data.IsGrowthZord && Level > 5)
            {
                float growthBonus = (Level - 5) * (Data.GrowthCurveMultiplier - 1.0f);
                levelMultiplier += growthBonus;
            }

            return Data.BaseStats.Add(Data.StatsPerLevel.Scale(levelMultiplier));
        }

        public bool AddExperience(int amount)
        {
            Experience += amount;
            int threshold = 100 + (Level * 20); // Same curve as forms, tunable

            if (Experience >= threshold)
            {
                Experience -= threshold;
                Level++;
                float newMaxHealth = Data.BaseHealth + (Data.HealthPerLevel * (Level - 1));
                Health.SetMaximum(newMaxHealth);
                return true;
            }
            return false;
        }
    }
}
