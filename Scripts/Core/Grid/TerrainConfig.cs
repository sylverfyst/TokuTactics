using System.Collections.Generic;

namespace TokuTactics.Core.Grid
{
    /// <summary>
    /// Data-driven configuration for terrain properties.
    /// Movement costs, range bonuses, hazard damage — all tunable without code changes.
    /// 
    /// The grid reads from this config when calculating pathfinding and combat modifiers.
    /// </summary>
    public class TerrainConfig
    {
        private readonly Dictionary<TerrainType, TerrainProperties> _properties = new();

        /// <summary>Tunable: bonus range tiles per elevation level for ranged forms.</summary>
        public int RangeBonusPerElevation { get; set; } = 1;

        /// <summary>Tunable: movement cost to climb one elevation level.</summary>
        public int ElevationClimbCost { get; set; } = 1;

        /// <summary>Tunable: cover damage reduction (multiplier, 0.7 = 30% reduction).</summary>
        public float CoverDamageMultiplier { get; set; } = 0.7f;

        public TerrainConfig()
        {
            // Defaults — override with Configure()
            _properties[TerrainType.Open] = new TerrainProperties { MovementCost = 1 };
            _properties[TerrainType.Rough] = new TerrainProperties { MovementCost = 2 };
            _properties[TerrainType.Wall] = new TerrainProperties { MovementCost = int.MaxValue, BlocksMovement = true, BlocksLineOfSight = true };
            _properties[TerrainType.Cover] = new TerrainProperties { MovementCost = 1, BlocksLineOfSight = false };
            _properties[TerrainType.Hazard] = new TerrainProperties { MovementCost = 1, HazardDamage = 10f };
            _properties[TerrainType.HighGround] = new TerrainProperties { MovementCost = 1 };
            _properties[TerrainType.Destructible] = new TerrainProperties { MovementCost = int.MaxValue, BlocksMovement = true, BlocksLineOfSight = true };
            _properties[TerrainType.Gap] = new TerrainProperties { MovementCost = int.MaxValue, BlocksMovement = true };
        }

        /// <summary>
        /// Configure properties for a terrain type.
        /// </summary>
        public void Configure(TerrainType type, TerrainProperties properties)
        {
            _properties[type] = properties;
        }

        /// <summary>
        /// Get properties for a terrain type. Returns Open defaults if not configured.
        /// </summary>
        public TerrainProperties Get(TerrainType type)
        {
            return _properties.ContainsKey(type) ? _properties[type] : _properties[TerrainType.Open];
        }

        /// <summary>
        /// Calculate the total movement cost to enter a tile, including elevation change.
        /// Returns int.MaxValue if impassable. Overflow-safe.
        /// </summary>
        public int GetMovementCost(Tile from, Tile to)
        {
            var props = Get(to.Terrain);
            if (props.BlocksMovement) return int.MaxValue;

            int baseCost = props.MovementCost;
            int elevationDiff = to.Elevation - from.Elevation;

            if (elevationDiff > 0)
            {
                long climbCost = (long)elevationDiff * ElevationClimbCost;
                long total = (long)baseCost + climbCost;
                return total > int.MaxValue ? int.MaxValue : (int)total;
            }

            // Descending is free (no extra cost)
            return baseCost;
        }

        /// <summary>
        /// Calculate range bonus from elevation advantage.
        /// Attacker on higher ground gets bonus range.
        /// </summary>
        public int GetElevationRangeBonus(int attackerElevation, int targetElevation)
        {
            int diff = attackerElevation - targetElevation;
            return diff > 0 ? diff * RangeBonusPerElevation : 0;
        }
    }

    /// <summary>
    /// Properties for a single terrain type. All fields tunable.
    /// </summary>
    public class TerrainProperties
    {
        /// <summary>Base movement cost to enter this tile (1 = normal).</summary>
        public int MovementCost { get; set; } = 1;

        /// <summary>Whether this terrain completely blocks movement.</summary>
        public bool BlocksMovement { get; set; }

        /// <summary>Whether this terrain blocks line of sight for ranged attacks.</summary>
        public bool BlocksLineOfSight { get; set; }

        /// <summary>Damage dealt per turn to units standing on this tile (0 = no damage).</summary>
        public float HazardDamage { get; set; }
    }
}
