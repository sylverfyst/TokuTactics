using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Grid;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Enemies.Gimmicks;
using TokuTactics.Entities.Weapons;
using TokuTactics.Commands.Gimmick;
using TokuTactics.Systems.GimmickResolution;

namespace TokuTactics.Tests.Systems.GimmickResolution
{
    public class GimmickResolverTests
    {
        // === Helpers ===

        private BattleGrid MakeGrid(int size = 10)
        {
            return new BattleGrid(size, size);
        }

        private void PlaceRangers(BattleGrid grid, params (string id, int col, int row)[] rangers)
        {
            foreach (var (id, col, row) in rangers)
                grid.PlaceUnit(id, new GridPosition(col, row));
        }

        private HashSet<string> RangerIds(params string[] ids)
        {
            return new HashSet<string>(ids);
        }

        // === Empty / No Effect ===

        public void Resolve_NullOutput_ReturnsEmpty()
        {
            var resolver = new GimmickResolver(MakeGrid());
            var result = resolver.Resolve(new GridPosition(5, 5), null, 2, RangerIds());

            Assert(!result.HasEffects, "Null output should produce empty resolution");
        }

        public void Resolve_EmptyOutput_ReturnsEmpty()
        {
            var resolver = new GimmickResolver(MakeGrid());
            var result = resolver.Resolve(
                new GridPosition(5, 5), GimmickOutput.None, 2, RangerIds());

            Assert(!result.HasEffects, "Empty output should produce empty resolution");
        }

        // === Damage ===

        public void Resolve_Damage_HitsTargetsInRange()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 4, 5), ("r2", 6, 5));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { Damage = 20f };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 2, RangerIds("r1", "r2"));

            Assert(result.DamageEffects.Count == 2, "Should hit both Rangers in range");
            Assert(result.DamageEffects.All(d => d.Damage == 20f), "Each should take 20 damage");
        }

        public void Resolve_Damage_MissesTargetsOutOfRange()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 1, 1), ("r2", 6, 5));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { Damage = 15f };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 2, RangerIds("r1", "r2"));

            Assert(result.DamageEffects.Count == 1, "Should only hit r2 in range");
            Assert(result.DamageEffects[0].TargetId == "r2", "Should hit r2");
        }

        public void Resolve_Damage_NoTargetsInRange()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 0, 0));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { Damage = 10f };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 1, RangerIds("r1"));

            Assert(result.DamageEffects.Count == 0, "No targets in range = no damage");
        }

        public void Resolve_Damage_Range0_HitsAdjacentOnly()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 4), ("r2", 3, 3));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { Damage = 10f };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds("r1", "r2"));

            Assert(result.DamageEffects.Count == 1, "Range 0 should hit adjacent only");
            Assert(result.DamageEffects[0].TargetId == "r1", "Should hit adjacent Ranger");
        }

        // === Status Effect ===

        public void Resolve_StatusEffect_AppliedToTargetsInRange()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 4, 5), ("r2", 6, 5));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var template = new StatusEffectTemplate(
                "poison", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 3);

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { StatusEffect = template };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 2, RangerIds("r1", "r2"));

            Assert(result.StatusEffects.Count == 2, "Should apply to both Rangers");
            Assert(result.StatusEffects.All(s => s.Template == template), "Same template");
        }

        // === Healing ===

        public void Resolve_Healing_SetsOwnerHealing()
        {
            var resolver = new GimmickResolver(MakeGrid());
            var output = new GimmickOutput { Healing = 30f };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            Assert(result.OwnerHealing == 30f, "Should set owner healing");
            Assert(result.HasEffects, "Should have effects");
        }

        // === Shield ===

        public void Resolve_Shield_SetsActivation()
        {
            var resolver = new GimmickResolver(MakeGrid());
            var output = new GimmickOutput { ActivateShield = true, ShieldDuration = 3 };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            Assert(result.ActivateShield, "Should activate shield");
            Assert(result.ShieldDuration == 3, "Duration should be 3");
        }

        // === Terrain Modification ===

        public void Resolve_TerrainModify_DiamondShape_Radius1()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                ModifyTerrain = true,
                TargetTerrain = TerrainType.Hazard,
                TerrainRadius = 1,
                TerrainShape = AreaShape.Diamond
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            Assert(result.TerrainChanges.Count == 4, "Diamond radius 1 = 4 cardinal tiles");
            Assert(result.TerrainChanges.All(t => t.NewTerrain == TerrainType.Hazard),
                "All should become hazard");
        }

        public void Resolve_TerrainModify_DiamondShape_Radius2()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                ModifyTerrain = true,
                TargetTerrain = TerrainType.Hazard,
                TerrainRadius = 2,
                TerrainShape = AreaShape.Diamond
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            // Diamond radius 2: Manhattan distance <= 2, excluding center = 12 tiles
            Assert(result.TerrainChanges.Count == 12,
                "Diamond radius 2 = 12 tiles (4 at dist 1 + 8 at dist 2)");
        }

        public void Resolve_TerrainModify_SquareShape()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                ModifyTerrain = true,
                TargetTerrain = TerrainType.Hazard,
                TerrainRadius = 1,
                TerrainShape = AreaShape.Square
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            // Square radius 1: 3x3 - 1 center = 8 tiles (includes diagonals)
            Assert(result.TerrainChanges.Count == 8, "Square radius 1 = 8 tiles");
        }

        public void Resolve_TerrainModify_CrossShape()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                ModifyTerrain = true,
                TargetTerrain = TerrainType.Hazard,
                TerrainRadius = 2,
                TerrainShape = AreaShape.Cross
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            // Cross radius 2: 2 tiles in each cardinal direction = 8 tiles
            Assert(result.TerrainChanges.Count == 8, "Cross radius 2 = 8 tiles");

            // Verify no diagonal tiles
            foreach (var change in result.TerrainChanges)
            {
                bool isCardinal = change.Position.Col == 5 || change.Position.Row == 5;
                Assert(isCardinal, "Cross shape should only include cardinal tiles");
            }
        }

        public void Resolve_TerrainModify_SkipsBlockingTiles()
        {
            var grid = MakeGrid();
            grid.SetTile(new GridPosition(5, 4), TerrainType.Wall);
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                ModifyTerrain = true,
                TargetTerrain = TerrainType.Hazard,
                TerrainRadius = 1
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            Assert(result.TerrainChanges.Count == 3, "Should skip wall tile");
            Assert(!result.TerrainChanges.Any(t => t.Position == new GridPosition(5, 4)),
                "Wall position should not be changed");
        }

        public void Resolve_TerrainModify_SkipsOccupiedTiles()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 4));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                ModifyTerrain = true,
                TargetTerrain = TerrainType.Hazard,
                TerrainRadius = 1
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds("r1"));

            Assert(!result.TerrainChanges.Any(t => t.Position == new GridPosition(5, 4)),
                "Occupied tile should not be changed");
        }

        // === Spawn ===

        public void Resolve_Spawn_FindsOpenPositions()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                SpawnCount = 3,
                SpawnEnemyDataId = "foot_basic"
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            Assert(result.Spawns.Count == 3, "Should find 3 spawn positions");
            Assert(result.Spawns.All(s => s.EnemyDataId == "foot_basic"), "Correct data ID");
            Assert(result.Spawns.All(s => s.Position != new GridPosition(5, 5)),
                "Should not spawn on owner position");
        }

        public void Resolve_Spawn_NoDuplicatePositions()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("enemy", new GridPosition(5, 5));
            // Block 3 of 4 adjacent tiles so ring 1 only has 1 open tile
            grid.SetTile(new GridPosition(4, 5), TerrainType.Wall);
            grid.SetTile(new GridPosition(6, 5), TerrainType.Wall);
            grid.SetTile(new GridPosition(5, 4), TerrainType.Wall);
            // (5,6) is open in ring 1

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                SpawnCount = 4,
                SpawnEnemyDataId = "foot_basic"
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 0, RangerIds());

            // Verify no duplicate positions
            var uniquePositions = new HashSet<GridPosition>(
                result.Spawns.Select(s => s.Position));
            Assert(uniquePositions.Count == result.Spawns.Count,
                "All spawn positions should be unique");
        }

        public void Resolve_Spawn_LimitedByAvailableSpace()
        {
            var grid = new BattleGrid(3, 3);
            grid.PlaceUnit("enemy", new GridPosition(1, 1));
            grid.PlaceUnit("a", new GridPosition(0, 0));
            grid.PlaceUnit("b", new GridPosition(0, 1));
            grid.PlaceUnit("c", new GridPosition(1, 0));
            grid.PlaceUnit("d", new GridPosition(2, 0));
            grid.PlaceUnit("e", new GridPosition(0, 2));
            grid.PlaceUnit("f", new GridPosition(2, 2));
            // Open: (2,1), (1,2)

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                SpawnCount = 5,
                SpawnEnemyDataId = "foot_basic"
            };

            var result = resolver.Resolve(
                new GridPosition(1, 1), output, 0, RangerIds());

            Assert(result.Spawns.Count == 2, "Should only spawn where space allows");
        }

        public void Resolve_Spawn_RespectsSearchRadius()
        {
            var grid = MakeGrid(20);
            grid.PlaceUnit("enemy", new GridPosition(10, 10));
            // Block everything within radius 2
            for (int r = 1; r <= 2; r++)
            {
                foreach (var pos in grid.GetTilesInRange(new GridPosition(10, 10), r))
                    grid.SetTile(pos, TerrainType.Wall);
            }

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                SpawnCount = 1,
                SpawnEnemyDataId = "foot_basic",
                SpawnSearchRadius = 2 // Only search up to radius 2
            };

            var result = resolver.Resolve(
                new GridPosition(10, 10), output, 0, RangerIds());

            Assert(result.Spawns.Count == 0,
                "Should not find spawns beyond search radius");
        }

        // === Displacement (Cardinal) ===

        public void Resolve_DisplacementPush_CardinalNorth()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 3));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 2, DisplacementPush = true };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 3, RangerIds("r1"));

            Assert(result.Displacements.Count == 1, "Should displace r1");
            Assert(result.Displacements[0].To == new GridPosition(5, 1),
                "Should push 2 tiles north");
        }

        public void Resolve_DisplacementPull_CardinalSouth()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 2));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 2, DisplacementPush = false };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 5, RangerIds("r1"));

            Assert(result.Displacements.Count == 1, "Should displace r1");
            Assert(result.Displacements[0].To == new GridPosition(5, 4),
                "Should pull 2 tiles south");
        }

        public void Resolve_DisplacementPush_Diagonal_StepsCardinal()
        {
            var grid = MakeGrid();
            // Target NE of owner: should step cardinal only (not diagonal)
            PlaceRangers(grid, ("r1", 7, 3));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 3, DisplacementPush = true };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 5, RangerIds("r1"));

            Assert(result.Displacements.Count == 1, "Should displace r1");

            var from = result.Displacements[0].From;
            var to = result.Displacements[0].To;

            // Verify movement happened
            Assert(to != from, "Should have moved");

            // Verify total Manhattan distance is exactly 3 steps from start
            int manhattanMoved = from.ManhattanDistance(to);
            Assert(manhattanMoved == 3,
                $"Should move exactly 3 cardinal steps, moved {manhattanMoved}");

            // Verify moved generally NE (away from owner at 5,5)
            Assert(to.Col >= from.Col, "Push should move east (away from col 5)");
            Assert(to.Row <= from.Row, "Push should move north (away from row 5)");
        }

        public void Resolve_DisplacementPush_Diagonal_45Degree()
        {
            var grid = MakeGrid();
            // Exactly 45 degrees: equal col and row distance
            PlaceRangers(grid, ("r1", 7, 3)); // +2 col, -2 row from owner
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 4, DisplacementPush = true };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 5, RangerIds("r1"));

            Assert(result.Displacements.Count == 1, "Should displace");
            var to = result.Displacements[0].To;

            // At 45 degrees with 4 cardinal steps: should alternate col/row
            // From (7,3), push NE: expect 2 col steps + 2 row steps
            Assert(to.Col > 7, "Should move further east");
            Assert(to.Row < 3, "Should move further north");
            int manhattanMoved = new GridPosition(7, 3).ManhattanDistance(to);
            Assert(manhattanMoved == 4, "Should move exactly 4 cardinal steps");
        }

        public void Resolve_DisplacementPush_StopsAtWall()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 4));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));
            grid.SetTile(new GridPosition(5, 2), TerrainType.Wall);

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 5, DisplacementPush = true };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 2, RangerIds("r1"));

            Assert(result.Displacements.Count == 1, "Should attempt displacement");
            Assert(result.Displacements[0].To == new GridPosition(5, 3),
                "Should stop one tile before wall");
        }

        public void Resolve_DisplacementPush_StopsAtGridEdge()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 1));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 5, DisplacementPush = true };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 5, RangerIds("r1"));

            Assert(result.Displacements[0].To == new GridPosition(5, 0),
                "Should stop at grid edge");
        }

        public void Resolve_DisplacementPull_StopsBeforeOccupied()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 2));
            grid.PlaceUnit("ally", new GridPosition(5, 3));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 3, DisplacementPush = false };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 5, RangerIds("r1"));

            Assert(result.Displacements.Count == 0,
                "Should not move through occupied tile — zero displacement filtered out");
        }

        public void Resolve_Displacement_NoMovement_NotIncluded()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 4));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));
            grid.SetTile(new GridPosition(5, 3), TerrainType.Wall);

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput { DisplacementDistance = 2, DisplacementPush = true };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 2, RangerIds("r1"));

            Assert(result.Displacements.Count == 0,
                "Zero displacement should not be included");
        }

        // === Combined Effects ===

        public void Resolve_CombinedEffects_AllApplied()
        {
            var grid = MakeGrid();
            PlaceRangers(grid, ("r1", 5, 4));
            grid.PlaceUnit("enemy", new GridPosition(5, 5));

            var resolver = new GimmickResolver(grid);
            var output = new GimmickOutput
            {
                Damage = 15f,
                ModifyTerrain = true,
                TargetTerrain = TerrainType.Hazard,
                TerrainRadius = 1,
                ActivateShield = true,
                ShieldDuration = 2
            };

            var result = resolver.Resolve(
                new GridPosition(5, 5), output, 2, RangerIds("r1"));

            Assert(result.DamageEffects.Count == 1, "Should have damage");
            Assert(result.TerrainChanges.Count > 0, "Should have terrain changes");
            Assert(result.ActivateShield, "Should activate shield");
            Assert(result.TotalEffectCount >= 3, "Should have multiple effect types");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new GimmickResolverTests();

            // Empty
            t.Resolve_NullOutput_ReturnsEmpty();
            t.Resolve_EmptyOutput_ReturnsEmpty();

            // Damage
            t.Resolve_Damage_HitsTargetsInRange();
            t.Resolve_Damage_MissesTargetsOutOfRange();
            t.Resolve_Damage_NoTargetsInRange();
            t.Resolve_Damage_Range0_HitsAdjacentOnly();

            // Status Effect
            t.Resolve_StatusEffect_AppliedToTargetsInRange();

            // Healing
            t.Resolve_Healing_SetsOwnerHealing();

            // Shield
            t.Resolve_Shield_SetsActivation();

            // Terrain (shapes)
            t.Resolve_TerrainModify_DiamondShape_Radius1();
            t.Resolve_TerrainModify_DiamondShape_Radius2();
            t.Resolve_TerrainModify_SquareShape();
            t.Resolve_TerrainModify_CrossShape();
            t.Resolve_TerrainModify_SkipsBlockingTiles();
            t.Resolve_TerrainModify_SkipsOccupiedTiles();

            // Spawn
            t.Resolve_Spawn_FindsOpenPositions();
            t.Resolve_Spawn_NoDuplicatePositions();
            t.Resolve_Spawn_LimitedByAvailableSpace();
            t.Resolve_Spawn_RespectsSearchRadius();

            // Displacement (cardinal)
            t.Resolve_DisplacementPush_CardinalNorth();
            t.Resolve_DisplacementPull_CardinalSouth();
            t.Resolve_DisplacementPush_Diagonal_StepsCardinal();
            t.Resolve_DisplacementPush_Diagonal_45Degree();
            t.Resolve_DisplacementPush_StopsAtWall();
            t.Resolve_DisplacementPush_StopsAtGridEdge();
            t.Resolve_DisplacementPull_StopsBeforeOccupied();
            t.Resolve_Displacement_NoMovement_NotIncluded();

            // Combined
            t.Resolve_CombinedEffects_AllApplied();

            System.Console.WriteLine("GimmickResolverTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
