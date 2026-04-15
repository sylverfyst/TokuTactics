using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Gimmick;
using TokuTactics.Core.Grid;
using TokuTactics.Entities.Enemies.Gimmicks;

namespace TokuTactics.Commands.Gimmick
{
    /// <summary>
    /// Command: Translates a declarative GimmickOutput into a concrete GimmickResolution.
    /// Composes FindUnitsInRange, CalculateDisplacement, and FindPassableSpawnPositions bricks.
    /// Does NOT mutate game state — produces a resolution for the combat resolver to apply.
    /// </summary>
    public static class ResolveGimmickEffects
    {
        public static GimmickResolution Execute(
            BattleGrid grid,
            GridPosition ownerPosition,
            GimmickOutput output,
            int behaviorRange,
            HashSet<string> targetUnitIds,
            Func<BattleGrid, GridPosition, int, HashSet<string>, List<string>> findUnitsInRange = null,
            Func<BattleGrid, GridPosition, GridPosition, int, bool, GridPosition> calculateDisplacement = null,
            Func<BattleGrid, GridPosition, int, int, List<GridPosition>> findSpawnPositions = null)
        {
            findUnitsInRange ??= FindUnitsInRange.Execute;
            calculateDisplacement ??= CalculateDisplacement.Execute;
            findSpawnPositions ??= FindPassableSpawnPositions.Execute;

            if (output == null || !output.HasEffect)
                return GimmickResolution.Empty;

            var resolution = new GimmickResolution();

            var targetsInRange = findUnitsInRange(grid, ownerPosition, behaviorRange, targetUnitIds);

            // Damage
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

            // Status Effect
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

            // Healing (self)
            if (output.Healing > 0)
                resolution.OwnerHealing = output.Healing;

            // Shield (self)
            if (output.ActivateShield)
            {
                resolution.ActivateShield = true;
                resolution.ShieldDuration = output.ShieldDuration;
            }

            // Terrain Modification
            if (output.ModifyTerrain)
            {
                var candidates = grid.GetTilesInArea(
                    ownerPosition, output.TerrainRadius, output.TerrainShape);

                foreach (var pos in candidates)
                {
                    if (grid.IsTileBlocking(pos)) continue;
                    var tile = grid.GetTile(pos);
                    if (tile != null && !tile.IsOccupied)
                    {
                        resolution.TerrainChanges.Add(new TerrainChange
                        {
                            Position = pos,
                            NewTerrain = output.TargetTerrain
                        });
                    }
                }
            }

            // Spawn
            if (output.SpawnCount > 0)
            {
                var spawnPositions = findSpawnPositions(
                    grid, ownerPosition, output.SpawnCount, output.SpawnSearchRadius);

                foreach (var pos in spawnPositions)
                {
                    resolution.Spawns.Add(new SpawnEffect
                    {
                        Position = pos,
                        EnemyDataId = output.SpawnEnemyDataId
                    });
                }
            }

            // Displacement
            if (output.DisplacementDistance > 0)
            {
                foreach (var targetId in targetsInRange)
                {
                    var targetPos = grid.GetUnitPosition(targetId);
                    if (!targetPos.HasValue) continue;

                    var destination = calculateDisplacement(
                        grid, ownerPosition, targetPos.Value,
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
    }
}
