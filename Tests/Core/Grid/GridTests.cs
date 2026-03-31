using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Core.Grid
{
    public class GridTests
    {
        // === GridPosition ===

        public void GridPosition_Equality()
        {
            var a = new GridPosition(3, 5);
            var b = new GridPosition(3, 5);
            var c = new GridPosition(3, 6);

            Assert(a == b, "Same coords should be equal");
            Assert(a != c, "Different coords should not be equal");
            Assert(a.Equals(b), "Equals should match ==");
        }

        public void GridPosition_ManhattanDistance()
        {
            var a = new GridPosition(0, 0);
            var b = new GridPosition(3, 4);

            Assert(a.ManhattanDistance(b) == 7, "Distance should be 7");
            Assert(b.ManhattanDistance(a) == 7, "Distance should be symmetric");
            Assert(a.ManhattanDistance(a) == 0, "Self distance should be 0");
        }

        public void GridPosition_Add()
        {
            var pos = new GridPosition(2, 3);
            var offset = new GridPosition(1, -1);

            var result = pos.Add(offset);

            Assert(result == new GridPosition(3, 2), "Should add coords");
        }

        // === Tile ===

        public void Tile_DefaultsToOpen()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(0, 0);
            var tile = grid.GetTile(pos);

            Assert(tile.Terrain == TerrainType.Open, "Should default to Open");
            Assert(tile.Elevation == 0, "Should default to elevation 0");
            Assert(!tile.IsOccupied, "Should not be occupied");
            Assert(grid.IsTilePassable(pos), "Open ground should be passable");
        }

        public void Tile_WallIsBlocking()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(0, 0);
            grid.SetTile(pos, TerrainType.Wall);

            Assert(grid.IsTileBlocking(pos), "Wall should block");
            Assert(!grid.IsTilePassable(pos), "Wall should not be passable");
        }

        public void Tile_CoverProvidesCover()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(0, 0);
            grid.SetTile(pos, TerrainType.Cover);
            var tile = grid.GetTile(pos);

            Assert(tile.ProvidesCover, "Cover should provide cover");
            Assert(!grid.IsTileBlocking(pos), "Cover should not block movement");
        }

        public void Tile_OccupiedNotPassable()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(0, 0);
            grid.PlaceUnit("unit_1", pos);

            Assert(grid.GetTile(pos).IsOccupied, "Should be occupied");
            Assert(!grid.IsTilePassable(pos), "Occupied tile should not be passable");
        }

        // === BattleGrid Basics ===

        public void Grid_CreatesAllTiles()
        {
            var grid = new BattleGrid(10, 8);

            Assert(grid.Width == 10, "Width should be 10");
            Assert(grid.Height == 8, "Height should be 8");

            for (int c = 0; c < 10; c++)
                for (int r = 0; r < 8; r++)
                    Assert(grid.GetTile(new GridPosition(c, r)) != null, $"Tile ({c},{r}) should exist");
        }

        public void Grid_OutOfBounds_ReturnsNull()
        {
            var grid = new BattleGrid(5, 5);

            Assert(grid.GetTile(new GridPosition(-1, 0)) == null, "Negative col should be null");
            Assert(grid.GetTile(new GridPosition(0, -1)) == null, "Negative row should be null");
            Assert(grid.GetTile(new GridPosition(5, 0)) == null, "Col at width should be null");
            Assert(grid.GetTile(new GridPosition(0, 5)) == null, "Row at height should be null");
        }

        public void Grid_SetTile_ChangesProperties()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(2, 3);

            grid.SetTile(pos, TerrainType.Rough, elevation: 2);

            var tile = grid.GetTile(pos);
            Assert(tile.Terrain == TerrainType.Rough, "Terrain should be Rough");
            Assert(tile.Elevation == 2, "Elevation should be 2");
        }

        // === Occupancy ===

        public void PlaceUnit_Succeeds_OnOpenTile()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(2, 2);

            bool result = grid.PlaceUnit("ranger_red", pos);

            Assert(result, "Should succeed");
            Assert(grid.GetTile(pos).OccupantId == "ranger_red", "Should be occupied by red");
        }

        public void PlaceUnit_Fails_OnOccupiedTile()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(2, 2);
            grid.PlaceUnit("ranger_red", pos);

            bool result = grid.PlaceUnit("ranger_blue", pos);

            Assert(!result, "Should fail on occupied tile");
        }

        public void PlaceUnit_Fails_OnWall()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(2, 2);
            grid.SetTile(pos, TerrainType.Wall);

            bool result = grid.PlaceUnit("ranger_red", pos);

            Assert(!result, "Should fail on wall");
        }

        public void RemoveUnit_Succeeds()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(2, 2);
            grid.PlaceUnit("ranger_red", pos);

            bool result = grid.RemoveUnit("ranger_red");

            Assert(result, "Should succeed");
            Assert(!grid.GetTile(pos).IsOccupied, "Tile should be free");
        }

        public void MoveUnit_Succeeds()
        {
            var grid = new BattleGrid(5, 5);
            var from = new GridPosition(1, 1);
            var to = new GridPosition(3, 3);
            grid.PlaceUnit("ranger_red", from);

            bool result = grid.MoveUnit("ranger_red", to);

            Assert(result, "Should succeed");
            Assert(!grid.GetTile(from).IsOccupied, "Old tile should be free");
            Assert(grid.GetTile(to).OccupantId == "ranger_red", "New tile should be occupied");
        }

        public void MoveUnit_Fails_ToOccupied()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("ranger_red", new GridPosition(1, 1));
            grid.PlaceUnit("ranger_blue", new GridPosition(2, 2));

            bool result = grid.MoveUnit("ranger_red", new GridPosition(2, 2));

            Assert(!result, "Should fail moving to occupied tile");
        }

        public void FindUnit_ReturnsCorrectTile()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(3, 4);
            grid.PlaceUnit("ranger_red", pos);

            var tile = grid.FindUnit("ranger_red");

            Assert(tile != null, "Should find unit");
            Assert(tile.Position == pos, "Should be at correct position");
        }

        public void GetUnitPosition_ReturnsPosition()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(3, 4);
            grid.PlaceUnit("ranger_red", pos);

            var result = grid.GetUnitPosition("ranger_red");

            Assert(result.HasValue, "Should find unit");
            Assert(result.Value == pos, "Should match position");
        }

        public void GetUnitPosition_ReturnsNull_WhenNotFound()
        {
            var grid = new BattleGrid(5, 5);

            var result = grid.GetUnitPosition("nonexistent");

            Assert(!result.HasValue, "Should return null for missing unit");
        }

        // === Adjacency ===

        public void GetNeighbors_Cardinal_Returns4()
        {
            var grid = new BattleGrid(5, 5);
            grid.AllowDiagonalMovement = false;

            var neighbors = grid.GetNeighbors(new GridPosition(2, 2));

            Assert(neighbors.Count == 4, "Center tile should have 4 cardinal neighbors");
        }

        public void GetNeighbors_Cardinal_CornerReturns2()
        {
            var grid = new BattleGrid(5, 5);
            grid.AllowDiagonalMovement = false;

            var neighbors = grid.GetNeighbors(new GridPosition(0, 0));

            Assert(neighbors.Count == 2, "Corner should have 2 cardinal neighbors");
        }

        public void GetNeighbors_Diagonal_Returns8()
        {
            var grid = new BattleGrid(5, 5);
            grid.AllowDiagonalMovement = true;

            var neighbors = grid.GetNeighbors(new GridPosition(2, 2));

            Assert(neighbors.Count == 8, "Center tile should have 8 neighbors with diagonals");
        }

        public void AreAdjacent_CardinalTrue()
        {
            var grid = new BattleGrid(5, 5);

            Assert(grid.AreAdjacent(new GridPosition(2, 2), new GridPosition(2, 3)), "S should be adjacent");
            Assert(grid.AreAdjacent(new GridPosition(2, 2), new GridPosition(3, 2)), "E should be adjacent");
        }

        public void AreAdjacent_DiagonalFalse_InCardinalMode()
        {
            var grid = new BattleGrid(5, 5);
            grid.AllowDiagonalMovement = false;

            Assert(!grid.AreAdjacent(new GridPosition(2, 2), new GridPosition(3, 3)),
                "Diagonal should not be adjacent in cardinal mode");
        }

        public void GetAdjacentUnits_FindsNeighboringUnits()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("ranger_red", new GridPosition(2, 2));
            grid.PlaceUnit("ranger_blue", new GridPosition(2, 3)); // Adjacent S
            grid.PlaceUnit("ranger_green", new GridPosition(4, 4)); // Not adjacent

            var adjacent = grid.GetAdjacentUnits(new GridPosition(2, 2));

            Assert(adjacent.Count == 1, "Should find 1 adjacent unit");
            Assert(adjacent[0] == "ranger_blue", "Should find blue");
        }

        // === Movement Range ===

        public void GetMovementRange_OpenTerrain_DiamondShape()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);

            var range = grid.GetMovementRange(start, 2);

            // Manhattan distance 2 on open terrain = diamond of 13 tiles
            Assert(range.ContainsKey(start), "Should include start");
            Assert(range[start] == 0, "Start cost should be 0");
            Assert(range.ContainsKey(new GridPosition(4, 2)), "Should reach 2 north");
            Assert(range.ContainsKey(new GridPosition(6, 4)), "Should reach 2 east");
            Assert(!range.ContainsKey(new GridPosition(4, 1)), "Should not reach 3 north");
        }

        public void GetMovementRange_RoughTerrain_CostsMore()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);
            grid.SetTile(new GridPosition(4, 3), TerrainType.Rough); // North is rough

            var range = grid.GetMovementRange(start, 2);

            // Rough costs 2, so moving north takes 2 of the budget
            Assert(range.ContainsKey(new GridPosition(4, 3)), "Should reach rough tile");
            Assert(range[new GridPosition(4, 3)] == 2, "Rough tile should cost 2");
            Assert(!range.ContainsKey(new GridPosition(4, 2)), "Should not reach past rough with budget 2");
        }

        public void GetMovementRange_WallBlocks()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);
            grid.SetTile(new GridPosition(4, 3), TerrainType.Wall);

            var range = grid.GetMovementRange(start, 3);

            Assert(!range.ContainsKey(new GridPosition(4, 3)), "Should not enter wall");
            Assert(!range.ContainsKey(new GridPosition(4, 2)), "Should not pass through wall");
        }

        public void GetMovementRange_EnemyBlocks()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);
            grid.PlaceUnit("enemy_1", new GridPosition(4, 3));

            var range = grid.GetMovementRange(start, 3);

            Assert(!range.ContainsKey(new GridPosition(4, 3)), "Should not enter enemy tile");
        }

        public void GetMovementRange_AllyPassThrough()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);
            grid.PlaceUnit("ally_1", new GridPosition(4, 3));

            var allies = new HashSet<string> { "ally_1" };
            var range = grid.GetMovementRange(start, 3, allies);

            Assert(range.ContainsKey(new GridPosition(4, 3)), "Should be able to pass through ally");
            Assert(range.ContainsKey(new GridPosition(4, 2)), "Should reach past ally");
        }

        public void GetMovementRange_ElevationCosts()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);
            grid.SetTile(new GridPosition(4, 3), TerrainType.Open, elevation: 2);

            var range = grid.GetMovementRange(start, 2);

            // Climbing 2 elevation costs 1 (base) + 2 (climb) = 3, exceeds budget of 2
            Assert(!range.ContainsKey(new GridPosition(4, 3)),
                "Should not afford to climb 2 elevation with budget 2");
        }

        public void GetValidMoveTargets_ExcludesOccupied()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);
            grid.PlaceUnit("ally_1", new GridPosition(4, 3));

            var allies = new HashSet<string> { "ally_1" };
            var targets = grid.GetValidMoveTargets(start, 3, allies);

            // Can path THROUGH ally but cannot STOP on ally's tile
            Assert(!targets.Contains(new GridPosition(4, 3)),
                "Should not be able to stop on ally's tile");
            Assert(targets.Contains(new GridPosition(4, 2)),
                "Should be able to stop past ally");
        }

        public void GetValidMoveTargets_ExcludesStart()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(4, 4);

            var targets = grid.GetValidMoveTargets(start, 2);

            Assert(!targets.Contains(start), "Should not include starting position");
        }

        // === Pathfinding ===

        public void FindPath_StraightLine()
        {
            var grid = new BattleGrid(9, 9);
            var start = new GridPosition(1, 4);
            var goal = new GridPosition(5, 4);

            var path = grid.FindPath(start, goal);

            Assert(path != null, "Should find a path");
            Assert(path[0] == start, "Path should start at start");
            Assert(path[path.Count - 1] == goal, "Path should end at goal");
            Assert(path.Count == 5, "Straight line should be 5 tiles");
        }

        public void FindPath_AroundWall()
        {
            var grid = new BattleGrid(9, 9);
            // Wall across row 4, columns 3-5
            grid.SetTile(new GridPosition(3, 4), TerrainType.Wall);
            grid.SetTile(new GridPosition(4, 4), TerrainType.Wall);
            grid.SetTile(new GridPosition(5, 4), TerrainType.Wall);

            var start = new GridPosition(4, 3);
            var goal = new GridPosition(4, 5);

            var path = grid.FindPath(start, goal);

            Assert(path != null, "Should find path around wall");
            Assert(path.Count > 3, "Path should be longer than direct route");
            Assert(!path.Any(p => grid.IsTileBlocking(p)), "Path should not cross wall");
        }

        public void FindPath_NoPath_ReturnsNull()
        {
            var grid = new BattleGrid(5, 5);
            // Surround the goal with walls
            grid.SetTile(new GridPosition(2, 1), TerrainType.Wall);
            grid.SetTile(new GridPosition(2, 3), TerrainType.Wall);
            grid.SetTile(new GridPosition(1, 2), TerrainType.Wall);
            grid.SetTile(new GridPosition(3, 2), TerrainType.Wall);

            var path = grid.FindPath(new GridPosition(0, 0), new GridPosition(2, 2));

            Assert(path == null, "Should return null when no path exists");
        }

        public void FindPath_ToOccupiedTile_ReturnsNull()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("enemy", new GridPosition(3, 3));

            var path = grid.FindPath(new GridPosition(0, 0), new GridPosition(3, 3));

            Assert(path == null, "Should not path to occupied tile");
        }

        public void FindPath_ThroughAlly()
        {
            var grid = new BattleGrid(9, 9);
            grid.PlaceUnit("ally_1", new GridPosition(4, 4));

            var allies = new HashSet<string> { "ally_1" };
            var path = grid.FindPath(new GridPosition(4, 2), new GridPosition(4, 6),
                moveThroughIds: allies);

            Assert(path != null, "Should find path through ally");
            Assert(path.Contains(new GridPosition(4, 4)), "Path should go through ally's tile");
        }

        public void FindPath_MaxCostLimits()
        {
            var grid = new BattleGrid(9, 9);

            var path = grid.FindPath(new GridPosition(0, 0), new GridPosition(8, 8), maxCost: 3);

            Assert(path == null, "Should not find path within cost limit");
        }

        // === Line of Sight ===

        public void HasLineOfSight_ClearPath()
        {
            var grid = new BattleGrid(9, 9);

            bool los = grid.HasLineOfSight(new GridPosition(1, 1), new GridPosition(5, 5));

            Assert(los, "Clear path should have LoS");
        }

        public void HasLineOfSight_BlockedByWall()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(3, 3), TerrainType.Wall);

            bool los = grid.HasLineOfSight(new GridPosition(1, 1), new GridPosition(5, 5));

            Assert(!los, "Wall should block LoS");
        }

        public void HasLineOfSight_BlockedByHighElevation()
        {
            var grid = new BattleGrid(9, 9);
            // Source at elevation 0, target at elevation 0, hill at elevation 3 between
            grid.SetTile(new GridPosition(3, 1), TerrainType.Open, elevation: 3);

            bool los = grid.HasLineOfSight(new GridPosition(1, 1), new GridPosition(5, 1));

            Assert(!los, "Hill higher than both endpoints should block LoS");
        }

        public void HasLineOfSight_NotBlockedByLowerElevation()
        {
            var grid = new BattleGrid(9, 9);
            // Source at elevation 2, target at elevation 2, hill at elevation 1 between
            grid.SetTile(new GridPosition(1, 1), TerrainType.Open, elevation: 2);
            grid.SetTile(new GridPosition(5, 1), TerrainType.Open, elevation: 2);
            grid.SetTile(new GridPosition(3, 1), TerrainType.Open, elevation: 1);

            bool los = grid.HasLineOfSight(new GridPosition(1, 1), new GridPosition(5, 1));

            Assert(los, "Hill lower than endpoints should not block LoS");
        }

        public void HasLineOfSight_SamePosition()
        {
            var grid = new BattleGrid(5, 5);
            var pos = new GridPosition(2, 2);

            Assert(grid.HasLineOfSight(pos, pos), "Same position should always have LoS");
        }

        public void HasLineOfSight_BlockedByDestructible()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(3, 3), TerrainType.Destructible);

            bool los = grid.HasLineOfSight(new GridPosition(1, 1), new GridPosition(5, 5));

            Assert(!los, "Destructible should block LoS");
        }

        public void HasLineOfSight_ClearAfterDestruction()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(3, 3), TerrainType.Destructible);
            grid.DestroyTerrain(new GridPosition(3, 3));

            bool los = grid.HasLineOfSight(new GridPosition(1, 1), new GridPosition(5, 5));

            Assert(los, "Destroyed terrain should not block LoS");
        }

        // === Range Queries ===

        public void GetTilesInRange_ManhattanDiamond()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInRange(new GridPosition(4, 4), 1);

            Assert(tiles.Count == 4, "Range 1 should give 4 tiles");
            Assert(tiles.Contains(new GridPosition(4, 3)), "Should include N");
            Assert(tiles.Contains(new GridPosition(5, 4)), "Should include E");
        }

        public void GetTilesInRange_ExcludesCenter()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInRange(new GridPosition(4, 4), 2);

            Assert(!tiles.Contains(new GridPosition(4, 4)), "Should not include center");
        }

        public void GetTilesInRangeWithLoS_FiltersBlockedTiles()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(4, 3), TerrainType.Wall);

            var tiles = grid.GetTilesInRangeWithLoS(new GridPosition(4, 4), 2);

            Assert(!tiles.Contains(new GridPosition(4, 2)),
                "Tile behind wall should be excluded by LoS check");
        }

        public void GetEffectiveRange_WithElevation()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(4, 4), TerrainType.HighGround, elevation: 2);

            int effective = grid.GetEffectiveRange(new GridPosition(4, 4), 3);

            Assert(effective == 5, "Base 3 + elevation 2 * 1 bonus per level = 5");
        }

        // === Area Shape Queries ===

        public void GetTilesInArea_Diamond_Radius1()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInArea(new GridPosition(4, 4), 1, AreaShape.Diamond);

            Assert(tiles.Count == 4, "Diamond radius 1 = 4 cardinal tiles");
        }

        public void GetTilesInArea_Diamond_Radius2()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInArea(new GridPosition(4, 4), 2, AreaShape.Diamond);

            Assert(tiles.Count == 12, "Diamond radius 2 = 12 tiles");
        }

        public void GetTilesInArea_Square_Radius1()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInArea(new GridPosition(4, 4), 1, AreaShape.Square);

            Assert(tiles.Count == 8, "Square radius 1 = 3x3 - 1 = 8 tiles");
        }

        public void GetTilesInArea_Square_Radius2()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInArea(new GridPosition(4, 4), 2, AreaShape.Square);

            Assert(tiles.Count == 24, "Square radius 2 = 5x5 - 1 = 24 tiles");
        }

        public void GetTilesInArea_Cross_Radius1()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInArea(new GridPosition(4, 4), 1, AreaShape.Cross);

            Assert(tiles.Count == 4, "Cross radius 1 = 4 cardinal tiles (same as diamond r1)");
        }

        public void GetTilesInArea_Cross_Radius2()
        {
            var grid = new BattleGrid(9, 9);

            var tiles = grid.GetTilesInArea(new GridPosition(4, 4), 2, AreaShape.Cross);

            Assert(tiles.Count == 8, "Cross radius 2 = 2 tiles per cardinal direction = 8");
            foreach (var pos in tiles)
            {
                bool isCardinal = pos.Col == 4 || pos.Row == 4;
                Assert(isCardinal, $"Cross should only include cardinal tiles, got {pos}");
            }
        }

        public void GetTilesInArea_EdgeClipping()
        {
            var grid = new BattleGrid(5, 5);

            // Corner position — many tiles would be out of bounds
            var tiles = grid.GetTilesInArea(new GridPosition(0, 0), 2, AreaShape.Diamond);

            // Full diamond radius 2 = 12, but clipped to corner
            // In-bounds: (1,0), (2,0), (0,1), (1,1), (0,2) = 5
            Assert(tiles.Count == 5, "Should clip to grid bounds");
            foreach (var pos in tiles)
            {
                Assert(grid.IsInBounds(pos), $"All tiles should be in bounds, got {pos}");
            }
        }

        public void GetTilesInArea_Range0_Empty()
        {
            var grid = new BattleGrid(9, 9);

            var diamond = grid.GetTilesInArea(new GridPosition(4, 4), 0, AreaShape.Diamond);
            var square = grid.GetTilesInArea(new GridPosition(4, 4), 0, AreaShape.Square);
            var cross = grid.GetTilesInArea(new GridPosition(4, 4), 0, AreaShape.Cross);

            Assert(diamond.Count == 0, "Diamond range 0 = empty (center excluded)");
            Assert(square.Count == 0, "Square range 0 = empty");
            Assert(cross.Count == 0, "Cross range 0 = empty");
        }

        public void GetTilesInRange_MatchesDiamond()
        {
            var grid = new BattleGrid(9, 9);

            var range = grid.GetTilesInRange(new GridPosition(4, 4), 2);
            var diamond = grid.GetTilesInArea(new GridPosition(4, 4), 2, AreaShape.Diamond);

            Assert(range.Count == diamond.Count,
                "GetTilesInRange should produce same results as GetTilesInArea with Diamond");
        }

        // === Terrain Queries ===

        public void GetTilesOfType_FindsAll()
        {
            var grid = new BattleGrid(5, 5);
            grid.SetTile(new GridPosition(1, 1), TerrainType.Hazard);
            grid.SetTile(new GridPosition(3, 3), TerrainType.Hazard);

            var hazards = grid.GetTilesOfType(TerrainType.Hazard);

            Assert(hazards.Count == 2, "Should find 2 hazard tiles");
        }

        public void DestroyTerrain_ChangesToOpen()
        {
            var grid = new BattleGrid(5, 5);
            grid.SetTile(new GridPosition(2, 2), TerrainType.Destructible);

            bool result = grid.DestroyTerrain(new GridPosition(2, 2));

            Assert(result, "Should succeed");
            Assert(grid.GetTile(new GridPosition(2, 2)).Terrain == TerrainType.Open,
                "Should be Open after destruction");
        }

        public void DestroyTerrain_NonDestructible_Fails()
        {
            var grid = new BattleGrid(5, 5);
            grid.SetTile(new GridPosition(2, 2), TerrainType.Wall);

            bool result = grid.DestroyTerrain(new GridPosition(2, 2));

            Assert(!result, "Should fail on non-destructible terrain");
        }

        // === Bresenham's Line ===

        public void GetLine_Horizontal()
        {
            var line = BattleGrid.GetLine(new GridPosition(0, 0), new GridPosition(4, 0));

            Assert(line.Count == 5, "Horizontal line should have 5 points");
            Assert(line[0] == new GridPosition(0, 0), "Should start at origin");
            Assert(line[4] == new GridPosition(4, 0), "Should end at (4,0)");
        }

        public void GetLine_Diagonal()
        {
            var line = BattleGrid.GetLine(new GridPosition(0, 0), new GridPosition(3, 3));

            Assert(line[0] == new GridPosition(0, 0), "Should start at origin");
            Assert(line[line.Count - 1] == new GridPosition(3, 3), "Should end at (3,3)");
        }

        public void GetLine_SinglePoint()
        {
            var line = BattleGrid.GetLine(new GridPosition(2, 2), new GridPosition(2, 2));

            Assert(line.Count == 1, "Same point should return 1 point");
        }

        public void GetLine_Steep()
        {
            // More vertical than horizontal — exercises the other Bresenham branch
            var line = BattleGrid.GetLine(new GridPosition(0, 0), new GridPosition(1, 4));

            Assert(line[0] == new GridPosition(0, 0), "Should start at origin");
            Assert(line[line.Count - 1] == new GridPosition(1, 4), "Should end at (1,4)");
            Assert(line.Count >= 5, "Steep line should have at least 5 points");
        }

        // === Missing Adjacency Test ===

        public void AreAdjacent_DiagonalTrue_InDiagonalMode()
        {
            var grid = new BattleGrid(5, 5);
            grid.AllowDiagonalMovement = true;

            Assert(grid.AreAdjacent(new GridPosition(2, 2), new GridPosition(3, 3)),
                "Diagonal should be adjacent in diagonal mode");
        }

        // === Cover LoS ===

        public void HasLineOfSight_NotBlockedByCover()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(3, 3), TerrainType.Cover);

            bool los = grid.HasLineOfSight(new GridPosition(1, 1), new GridPosition(5, 5));

            Assert(los, "Cover should NOT block LoS — it reduces damage, not visibility");
        }

        // === Range Bonus Per-Target ===

        public void GetRangeBonusAgainst_HigherAttacker_GetsBonus()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(2, 2), TerrainType.Open, elevation: 3);
            grid.SetTile(new GridPosition(5, 5), TerrainType.Open, elevation: 0);

            int effective = grid.GetRangeBonusAgainst(
                new GridPosition(2, 2), new GridPosition(5, 5), 2);

            Assert(effective == 5, "Base 2 + 3 elevation bonus = 5");
        }

        public void GetRangeBonusAgainst_LowerAttacker_NoBonus()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(2, 2), TerrainType.Open, elevation: 0);
            grid.SetTile(new GridPosition(5, 5), TerrainType.Open, elevation: 3);

            int effective = grid.GetRangeBonusAgainst(
                new GridPosition(2, 2), new GridPosition(5, 5), 2);

            Assert(effective == 2, "No bonus when attacker is lower");
        }

        public void GetRangeBonusAgainst_SameElevation_NoBonus()
        {
            var grid = new BattleGrid(9, 9);
            grid.SetTile(new GridPosition(2, 2), TerrainType.Open, elevation: 2);
            grid.SetTile(new GridPosition(5, 5), TerrainType.Open, elevation: 2);

            int effective = grid.GetRangeBonusAgainst(
                new GridPosition(2, 2), new GridPosition(5, 5), 3);

            Assert(effective == 3, "Same elevation = no bonus");
        }

        // === Unit Index ===

        public void UnitIndex_TracksAfterMove()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("ranger_red", new GridPosition(1, 1));
            grid.MoveUnit("ranger_red", new GridPosition(3, 3));

            var pos = grid.GetUnitPosition("ranger_red");

            Assert(pos.HasValue, "Should find unit after move");
            Assert(pos.Value == new GridPosition(3, 3), "Should be at new position");
        }

        public void UnitIndex_ClearsAfterRemove()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("ranger_red", new GridPosition(1, 1));
            grid.RemoveUnit("ranger_red");

            var pos = grid.GetUnitPosition("ranger_red");

            Assert(!pos.HasValue, "Should not find removed unit");
        }

        public void AllUnitIds_TracksAll()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("a", new GridPosition(0, 0));
            grid.PlaceUnit("b", new GridPosition(1, 1));
            grid.PlaceUnit("c", new GridPosition(2, 2));

            Assert(grid.AllUnitIds.Count == 3, "Should track 3 units");

            grid.RemoveUnit("b");

            Assert(grid.AllUnitIds.Count == 2, "Should track 2 after removal");
        }

        // === IsTileBlocking/IsTilePassable ===

        public void IsTileBlocking_CustomTerrainConfig()
        {
            var config = new TerrainConfig();
            config.Configure(TerrainType.Rough, new TerrainProperties
            {
                MovementCost = 1,
                BlocksMovement = true // Custom: rough blocks in this config
            });

            var grid = new BattleGrid(5, 5, config);
            grid.SetTile(new GridPosition(2, 2), TerrainType.Rough);

            Assert(grid.IsTileBlocking(new GridPosition(2, 2)),
                "Custom config should make rough blocking");
        }

        public void IsTilePassable_EmptyOpen_True()
        {
            var grid = new BattleGrid(5, 5);

            Assert(grid.IsTilePassable(new GridPosition(2, 2)), "Empty open tile should be passable");
        }

        public void IsTilePassable_Occupied_False()
        {
            var grid = new BattleGrid(5, 5);
            grid.PlaceUnit("unit", new GridPosition(2, 2));

            Assert(!grid.IsTilePassable(new GridPosition(2, 2)), "Occupied tile should not be passable");
        }

        public void IsTileBlocking_OutOfBounds_True()
        {
            var grid = new BattleGrid(5, 5);

            Assert(grid.IsTileBlocking(new GridPosition(-1, -1)),
                "Out of bounds should be blocking");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new GridTests();

            // GridPosition
            tests.GridPosition_Equality();
            tests.GridPosition_ManhattanDistance();
            tests.GridPosition_Add();

            // Tile
            tests.Tile_DefaultsToOpen();
            tests.Tile_WallIsBlocking();
            tests.Tile_CoverProvidesCover();
            tests.Tile_OccupiedNotPassable();

            // BattleGrid Basics
            tests.Grid_CreatesAllTiles();
            tests.Grid_OutOfBounds_ReturnsNull();
            tests.Grid_SetTile_ChangesProperties();

            // Occupancy
            tests.PlaceUnit_Succeeds_OnOpenTile();
            tests.PlaceUnit_Fails_OnOccupiedTile();
            tests.PlaceUnit_Fails_OnWall();
            tests.RemoveUnit_Succeeds();
            tests.MoveUnit_Succeeds();
            tests.MoveUnit_Fails_ToOccupied();
            tests.FindUnit_ReturnsCorrectTile();
            tests.GetUnitPosition_ReturnsPosition();
            tests.GetUnitPosition_ReturnsNull_WhenNotFound();

            // Unit Index
            tests.UnitIndex_TracksAfterMove();
            tests.UnitIndex_ClearsAfterRemove();
            tests.AllUnitIds_TracksAll();

            // IsTileBlocking/IsTilePassable
            tests.IsTileBlocking_CustomTerrainConfig();
            tests.IsTilePassable_EmptyOpen_True();
            tests.IsTilePassable_Occupied_False();
            tests.IsTileBlocking_OutOfBounds_True();

            // Adjacency
            tests.GetNeighbors_Cardinal_Returns4();
            tests.GetNeighbors_Cardinal_CornerReturns2();
            tests.GetNeighbors_Diagonal_Returns8();
            tests.AreAdjacent_CardinalTrue();
            tests.AreAdjacent_DiagonalFalse_InCardinalMode();
            tests.AreAdjacent_DiagonalTrue_InDiagonalMode();
            tests.GetAdjacentUnits_FindsNeighboringUnits();

            // Movement Range
            tests.GetMovementRange_OpenTerrain_DiamondShape();
            tests.GetMovementRange_RoughTerrain_CostsMore();
            tests.GetMovementRange_WallBlocks();
            tests.GetMovementRange_EnemyBlocks();
            tests.GetMovementRange_AllyPassThrough();
            tests.GetMovementRange_ElevationCosts();
            tests.GetValidMoveTargets_ExcludesOccupied();
            tests.GetValidMoveTargets_ExcludesStart();

            // Pathfinding
            tests.FindPath_StraightLine();
            tests.FindPath_AroundWall();
            tests.FindPath_NoPath_ReturnsNull();
            tests.FindPath_ToOccupiedTile_ReturnsNull();
            tests.FindPath_ThroughAlly();
            tests.FindPath_MaxCostLimits();

            // Line of Sight
            tests.HasLineOfSight_ClearPath();
            tests.HasLineOfSight_BlockedByWall();
            tests.HasLineOfSight_BlockedByHighElevation();
            tests.HasLineOfSight_NotBlockedByLowerElevation();
            tests.HasLineOfSight_SamePosition();
            tests.HasLineOfSight_BlockedByDestructible();
            tests.HasLineOfSight_ClearAfterDestruction();
            tests.HasLineOfSight_NotBlockedByCover();

            // Range Queries
            tests.GetTilesInRange_ManhattanDiamond();
            tests.GetTilesInRange_ExcludesCenter();
            tests.GetTilesInRangeWithLoS_FiltersBlockedTiles();
            tests.GetEffectiveRange_WithElevation();
            tests.GetRangeBonusAgainst_HigherAttacker_GetsBonus();
            tests.GetRangeBonusAgainst_LowerAttacker_NoBonus();
            tests.GetRangeBonusAgainst_SameElevation_NoBonus();

            // Area Shapes
            tests.GetTilesInArea_Diamond_Radius1();
            tests.GetTilesInArea_Diamond_Radius2();
            tests.GetTilesInArea_Square_Radius1();
            tests.GetTilesInArea_Square_Radius2();
            tests.GetTilesInArea_Cross_Radius1();
            tests.GetTilesInArea_Cross_Radius2();
            tests.GetTilesInArea_EdgeClipping();
            tests.GetTilesInArea_Range0_Empty();
            tests.GetTilesInRange_MatchesDiamond();

            // Terrain
            tests.GetTilesOfType_FindsAll();
            tests.DestroyTerrain_ChangesToOpen();
            tests.DestroyTerrain_NonDestructible_Fails();

            // Bresenham
            tests.GetLine_Horizontal();
            tests.GetLine_Diagonal();
            tests.GetLine_SinglePoint();
            tests.GetLine_Steep();

            System.Console.WriteLine("GridTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
