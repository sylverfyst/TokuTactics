using System;
using System.Collections.Generic;
using TokuTactics.Core.Grid;
using TokuTactics.Entities.Enemies.Gimmicks;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Systems.GimmickResolution
{
    /// <summary>
    /// Translates a declarative GimmickOutput into a concrete GimmickResolution
    /// that lists exactly what happens to whom.
    /// 
    /// The resolver uses the grid for spatial queries (who's in range, which tiles
    /// to modify, where to displace targets) but does NOT mutate game state itself.
    /// It produces a resolution that the combat resolver applies through proper channels
    /// (damage pipeline, status effect system, grid operations).
    /// 
    /// This separation means gimmick resolution is fully testable with just a grid
    /// and a list of target positions — no combat system wiring needed.
    /// </summary>
    public class GimmickResolver
    {
        private readonly BattleGrid _grid;

        public GimmickResolver(BattleGrid grid)
        {
            _grid = grid;
        }

        /// <summary>
        /// Resolve a gimmick output into concrete effects.
        /// 
        /// ownerPosition: where the gimmick-owning enemy is on the grid.
        /// output: the declarative output from the gimmick behavior.
        /// behaviorRange: the range of the gimmick behavior (how far effects reach).
        /// targetUnitIds: IDs of units that can be targeted (typically Rangers).
        ///   The caller filters this — the resolver just checks which are in range.
        /// </summary>
        public GimmickResolution Resolve(
            GridPosition ownerPosition,
            GimmickOutput output,
            int behaviorRange,
            HashSet<string> targetUnitIds)
        {
            if (output == null || !output.HasEffect)
                return GimmickResolution.Empty;

            var resolution = new GimmickResolution();

            // Find targets in range
            var targetsInRange = FindTargetsInRange(ownerPosition, behaviorRange, targetUnitIds);

            // === Damage ===
            if (output.Damage > 0)
            {
                foreach (var targetId in targetsInRange)
                {
                    resolution.DamageEffects.Add(new DamageEffect
                    {
                        TargetId = targetId,
                        Damage = output.Damage
                    });
                }
            }

            // === Status Effect ===
            if (output.StatusEffect != null)
            {
                foreach (var targetId in targetsInRange)
                {
                    resolution.StatusEffects.Add(new StatusEffectApplication
                    {
                        TargetId = targetId,
                        Template = output.StatusEffect
                    });
                }
            }

            // === Healing (self) ===
            if (output.Healing > 0)
            {
                resolution.OwnerHealing = output.Healing;
            }

            // === Shield (self) ===
            if (output.ActivateShield)
            {
                resolution.ActivateShield = true;
                resolution.ShieldDuration = output.ShieldDuration;
            }

            // === Terrain Modification ===
            if (output.ModifyTerrain)
            {
                var tiles = FindTilesForTerrainModification(
                    ownerPosition, output.TerrainRadius, output.TerrainShape);

                foreach (var pos in tiles)
                {
                    resolution.TerrainChanges.Add(new TerrainChange
                    {
                        Position = pos,
                        NewTerrain = output.TargetTerrain
                    });
                }
            }

            // === Spawn ===
            if (output.SpawnCount > 0)
            {
                var spawnPositions = FindSpawnPositions(
                    ownerPosition, output.SpawnCount, output.SpawnSearchRadius);

                foreach (var pos in spawnPositions)
                {
                    resolution.Spawns.Add(new SpawnEffect
                    {
                        Position = pos,
                        EnemyDataId = output.SpawnEnemyDataId
                    });
                }
            }

            // === Displacement ===
            if (output.DisplacementDistance > 0)
            {
                foreach (var targetId in targetsInRange)
                {
                    var targetPos = _grid.GetUnitPosition(targetId);
                    if (!targetPos.HasValue) continue;

                    var destination = CalculateDisplacementDestination(
                        ownerPosition, targetPos.Value,
                        output.DisplacementDistance, output.DisplacementPush);

                    if (destination != targetPos.Value)
                    {
                        resolution.Displacements.Add(new DisplacementEffect
                        {
                            TargetId = targetId,
                            From = targetPos.Value,
                            To = destination
                        });
                    }
                }
            }

            return resolution;
        }

        // === Spatial Queries ===

        /// <summary>
        /// Find which target units are within range of the owner.
        /// </summary>
        private List<string> FindTargetsInRange(
            GridPosition ownerPosition, int range,
            HashSet<string> targetUnitIds)
        {
            var inRange = new List<string>();

            if (range <= 0)
            {
                // Range 0 = self/aura. For aura effects, hit adjacent targets.
                // Self-only effects (heal, shield) are handled separately.
                var adjacent = _grid.GetAdjacentUnits(ownerPosition);
                foreach (var id in adjacent)
                {
                    if (targetUnitIds.Contains(id))
                        inRange.Add(id);
                }
            }
            else
            {
                foreach (var targetId in targetUnitIds)
                {
                    var pos = _grid.GetUnitPosition(targetId);
                    if (!pos.HasValue) continue;

                    if (ownerPosition.ManhattanDistance(pos.Value) <= range)
                        inRange.Add(targetId);
                }
            }

            return inRange;
        }

        /// <summary>
        /// Find tiles for terrain modification within a radius and shape.
        /// Only modifies passable, non-occupied tiles (don't trap units in walls).
        /// </summary>
        private List<GridPosition> FindTilesForTerrainModification(
            GridPosition center, int radius, AreaShape shape)
        {
            var tiles = new List<GridPosition>();

            var candidates = _grid.GetTilesInArea(center, radius, shape);
            foreach (var pos in candidates)
            {
                if (_grid.IsTileBlocking(pos)) continue;

                var tile = _grid.GetTile(pos);
                if (tile != null && !tile.IsOccupied)
                    tiles.Add(pos);
            }

            return tiles;
        }

        /// <summary>
        /// Find open tiles near the owner to spawn foot soldiers.
        /// Returns up to 'count' positions, searching expanding rings from the owner
        /// up to maxSearchRadius. Uses a HashSet to prevent duplicate positions
        /// across rings (inner rings are subsets of outer rings).
        /// </summary>
        private List<GridPosition> FindSpawnPositions(
            GridPosition center, int count, int maxSearchRadius)
        {
            var positions = new List<GridPosition>();
            var seen = new HashSet<GridPosition>();

            for (int radius = 1; positions.Count < count && radius <= maxSearchRadius; radius++)
            {
                var candidates = _grid.GetTilesInRange(center, radius);
                foreach (var pos in candidates)
                {
                    if (positions.Count >= count) break;
                    if (seen.Contains(pos)) continue;
                    seen.Add(pos);

                    if (_grid.IsTilePassable(pos))
                        positions.Add(pos);
                }
            }

            return positions;
        }

        /// <summary>
        /// Calculate where a unit ends up after being displaced.
        /// Push: moves away from owner. Pull: moves toward owner.
        /// 
        /// Uses Bresenham-style cardinal stepping so displacement always follows
        /// a valid cardinal path on the grid. No diagonal shortcuts — each step
        /// is to an adjacent tile the movement system would recognize.
        /// 
        /// Stops at the last valid position if blocked by wall, grid edge, or occupied tile.
        /// </summary>
        private GridPosition CalculateDisplacementDestination(
            GridPosition ownerPos, GridPosition targetPos,
            int distance, bool isPush)
        {
            // Compute the raw direction vector
            int rawDc = isPush ? (targetPos.Col - ownerPos.Col) : (ownerPos.Col - targetPos.Col);
            int rawDr = isPush ? (targetPos.Row - ownerPos.Row) : (ownerPos.Row - targetPos.Row);

            // No direction = no displacement
            if (rawDc == 0 && rawDr == 0) return targetPos;

            // Generate cardinal steps using Bresenham-style error tracking.
            // This produces a sequence of (0,±1) and (±1,0) steps that approximates
            // the displacement direction without diagonal movement.
            int absDc = Math.Abs(rawDc);
            int absDr = Math.Abs(rawDr);
            int signDc = Math.Sign(rawDc);
            int signDr = Math.Sign(rawDr);

            var current = targetPos;
            int stepsRemaining = distance;

            // Bresenham error tracking — same principle as line drawing
            // but we generate one cardinal step at a time
            int error = absDc - absDr;

            while (stepsRemaining > 0)
            {
                GridPosition next;

                if (absDc == 0)
                {
                    // Pure vertical
                    next = new GridPosition(current.Col, current.Row + signDr);
                }
                else if (absDr == 0)
                {
                    // Pure horizontal
                    next = new GridPosition(current.Col + signDc, current.Row);
                }
                else if (error > 0 || (error == 0 && absDc >= absDr))
                {
                    // Step along dominant axis (col)
                    next = new GridPosition(current.Col + signDc, current.Row);
                    error -= absDr;
                }
                else
                {
                    // Step along minor axis (row)
                    next = new GridPosition(current.Col, current.Row + signDr);
                    error += absDc;
                }

                if (!_grid.IsInBounds(next)) break;
                if (_grid.IsTileBlocking(next)) break;

                var tile = _grid.GetTile(next);
                if (tile != null && tile.IsOccupied) break;

                current = next;
                stepsRemaining--;
            }

            return current;
        }
    }

    // === Resolution Data ===

    /// <summary>
    /// Concrete resolution of a gimmick — lists exactly what happens to whom.
    /// The combat resolver reads this and applies effects through proper channels.
    /// Each field is a list because gimmicks are AoE — they can hit multiple targets.
    /// </summary>
    public class GimmickResolution
    {
        /// <summary>Damage to deal to specific targets.</summary>
        public List<DamageEffect> DamageEffects { get; } = new();

        /// <summary>Status effects to apply to specific targets.</summary>
        public List<StatusEffectApplication> StatusEffects { get; } = new();

        /// <summary>Units to displace to new positions.</summary>
        public List<DisplacementEffect> Displacements { get; } = new();

        /// <summary>Tiles to change terrain on.</summary>
        public List<TerrainChange> TerrainChanges { get; } = new();

        /// <summary>Enemies to spawn at specific positions.</summary>
        public List<SpawnEffect> Spawns { get; } = new();

        /// <summary>Healing to apply to the gimmick owner.</summary>
        public float OwnerHealing { get; set; }

        /// <summary>Whether to activate a shield on the owner.</summary>
        public bool ActivateShield { get; set; }

        /// <summary>Duration of the shield in turns.</summary>
        public int ShieldDuration { get; set; }

        /// <summary>Whether this resolution has any effects at all.</summary>
        public bool HasEffects => DamageEffects.Count > 0 || StatusEffects.Count > 0
            || Displacements.Count > 0 || TerrainChanges.Count > 0
            || Spawns.Count > 0 || OwnerHealing > 0 || ActivateShield;

        /// <summary>Total number of discrete effects in this resolution.</summary>
        public int TotalEffectCount => DamageEffects.Count + StatusEffects.Count
            + Displacements.Count + TerrainChanges.Count + Spawns.Count
            + (OwnerHealing > 0 ? 1 : 0) + (ActivateShield ? 1 : 0);

        public static GimmickResolution Empty => new GimmickResolution();
    }

    /// <summary>Damage to deal to a specific target.</summary>
    public class DamageEffect
    {
        public string TargetId { get; set; }
        public float Damage { get; set; }
    }

    /// <summary>Status effect to apply to a specific target.</summary>
    public class StatusEffectApplication
    {
        public string TargetId { get; set; }
        public StatusEffectTemplate Template { get; set; }
    }

    /// <summary>Move a unit from one position to another.</summary>
    public class DisplacementEffect
    {
        public string TargetId { get; set; }
        public GridPosition From { get; set; }
        public GridPosition To { get; set; }
    }

    /// <summary>Change a tile's terrain type.</summary>
    public class TerrainChange
    {
        public GridPosition Position { get; set; }
        public TerrainType NewTerrain { get; set; }
    }

    /// <summary>Spawn an enemy at a specific position.</summary>
    public class SpawnEffect
    {
        public GridPosition Position { get; set; }
        public string EnemyDataId { get; set; }
    }
}
