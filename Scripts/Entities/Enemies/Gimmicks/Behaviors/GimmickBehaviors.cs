using TokuTactics.Core.Grid;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Entities.Enemies.Gimmicks.Behaviors
{
    /// <summary>
    /// Deal damage to all targets in range.
    /// Example: AoE slam, fire breath.
    /// </summary>
    public class DamageGimmickBehavior : IGimmickBehavior
    {
        public string Id => "gimmick_behavior_damage";
        public int Range { get; }

        /// <summary>Base damage before MAG scaling.</summary>
        public float BaseDamage { get; }

        public DamageGimmickBehavior(float baseDamage, int range)
        {
            BaseDamage = baseDamage;
            Range = range;
        }

        public GimmickOutput GetOutput(GimmickContext context)
        {
            return new GimmickOutput
            {
                Damage = BaseDamage * (1 + context.OwnerMag * 0.01f)
            };
        }
    }

    /// <summary>
    /// Spawn additional foot soldiers.
    /// Example: Summon wave, call reinforcements.
    /// </summary>
    public class SpawnGimmickBehavior : IGimmickBehavior
    {
        public string Id => "gimmick_behavior_spawn";
        public int Range => 0; // Spawns are placed by the combat resolver, not ranged

        /// <summary>How many foot soldiers to spawn.</summary>
        public int Count { get; }

        /// <summary>EnemyData ID of the foot soldier type to spawn.</summary>
        public string FootSoldierDataId { get; }

        /// <summary>Maximum search radius for finding open spawn positions.</summary>
        public int SearchRadius { get; }

        public SpawnGimmickBehavior(int count, string footSoldierDataId, int searchRadius = 5)
        {
            Count = count;
            FootSoldierDataId = footSoldierDataId;
            SearchRadius = searchRadius;
        }

        public GimmickOutput GetOutput(GimmickContext context)
        {
            return new GimmickOutput
            {
                SpawnCount = Count,
                SpawnEnemyDataId = FootSoldierDataId,
                SpawnSearchRadius = SearchRadius
            };
        }
    }

    /// <summary>
    /// Apply a status effect to all targets in range.
    /// Example: Poison aura, slow field, defense break zone.
    /// </summary>
    public class StatusEffectGimmickBehavior : IGimmickBehavior
    {
        public string Id => "gimmick_behavior_status_effect";
        public int Range { get; }

        /// <summary>The status effect to apply.</summary>
        public StatusEffectTemplate Effect { get; }

        public StatusEffectGimmickBehavior(StatusEffectTemplate effect, int range)
        {
            Effect = effect;
            Range = range;
        }

        public GimmickOutput GetOutput(GimmickContext context)
        {
            return new GimmickOutput
            {
                StatusEffect = Effect
            };
        }
    }

    /// <summary>
    /// Modify terrain in an area around the enemy.
    /// Example: Create hazard tiles, destroy cover.
    /// </summary>
    public class TerrainModifyGimmickBehavior : IGimmickBehavior
    {
        public string Id => "gimmick_behavior_terrain_modify";
        public int Range => 0; // Radius is the relevant measurement, not attack range

        /// <summary>Terrain type to set affected tiles to.</summary>
        public TerrainType TargetTerrain { get; }

        /// <summary>Radius in tiles around the enemy.</summary>
        public int Radius { get; }

        /// <summary>Shape of the affected area.</summary>
        public AreaShape Shape { get; }

        public TerrainModifyGimmickBehavior(TerrainType targetTerrain, int radius,
            AreaShape shape = AreaShape.Diamond)
        {
            TargetTerrain = targetTerrain;
            Radius = radius;
            Shape = shape;
        }

        public GimmickOutput GetOutput(GimmickContext context)
        {
            return new GimmickOutput
            {
                ModifyTerrain = true,
                TargetTerrain = TargetTerrain,
                TerrainRadius = Radius,
                TerrainShape = Shape
            };
        }
    }

    /// <summary>
    /// Grant temporary damage immunity to the enemy.
    /// Example: Shield phase, cocoon, barrier.
    /// </summary>
    public class ShieldGimmickBehavior : IGimmickBehavior
    {
        public string Id => "gimmick_behavior_shield";
        public int Range => 0; // Self-targeted

        /// <summary>How many turns the shield lasts.</summary>
        public int Duration { get; }

        public ShieldGimmickBehavior(int duration)
        {
            Duration = duration;
        }

        public GimmickOutput GetOutput(GimmickContext context)
        {
            return new GimmickOutput
            {
                ActivateShield = true,
                ShieldDuration = Duration
            };
        }
    }

    /// <summary>
    /// Heal the enemy.
    /// Example: Regeneration burst, life drain.
    /// </summary>
    public class HealGimmickBehavior : IGimmickBehavior
    {
        public string Id => "gimmick_behavior_heal";
        public int Range => 0; // Self-targeted

        /// <summary>Base healing amount before MAG scaling.</summary>
        public float BaseHealing { get; }

        public HealGimmickBehavior(float baseHealing)
        {
            BaseHealing = baseHealing;
        }

        public GimmickOutput GetOutput(GimmickContext context)
        {
            return new GimmickOutput
            {
                Healing = BaseHealing * (1 + context.OwnerMag * 0.01f)
            };
        }
    }

    /// <summary>
    /// Push or pull Rangers relative to the enemy.
    /// Example: Shockwave push, gravity pull, vortex.
    /// </summary>
    public class DisplacementGimmickBehavior : IGimmickBehavior
    {
        public string Id => "gimmick_behavior_displacement";
        public int Range { get; }

        /// <summary>How many tiles to move affected targets.</summary>
        public int Distance { get; }

        /// <summary>True = push away from enemy, false = pull toward enemy.</summary>
        public bool IsPush { get; }

        public DisplacementGimmickBehavior(int distance, bool isPush, int range)
        {
            Distance = distance;
            IsPush = isPush;
            Range = range;
        }

        public GimmickOutput GetOutput(GimmickContext context)
        {
            return new GimmickOutput
            {
                DisplacementDistance = Distance,
                DisplacementPush = IsPush
            };
        }
    }
}
