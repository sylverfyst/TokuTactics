using System;
using System.Collections.Generic;
using System.Linq;

namespace TokuTactics.Core.Grid
{
    /// <summary>
    /// The logical battle grid. Owns all spatial truth — tile state, occupancy,
    /// pathfinding, movement range, adjacency, and line of sight.
    /// 
    /// The Godot rendering layer reads from this grid to position isometric tiles
    /// and 2D sprites. This class has zero Godot dependencies — pure C#, fully testable.
    /// 
    /// Grid uses axial coordinates (col, row). The isometric projection is a
    /// rendering concern handled by the presentation layer.
    /// 
    /// Design: isometric 3D backgrounds with 2D sprites. Elevation is logical
    /// (affects range and movement cost) but visually represented by the 3D
    /// background tileset. Sprites sit on top of the isometric surface.
    /// </summary>
    public class BattleGrid
    {
        private readonly Tile[,] _tiles;
        private readonly TerrainConfig _terrainConfig;

        /// <summary>
        /// Reverse lookup: unit ID → grid position.
        /// Maintained alongside tile occupancy for O(1) unit lookups.
        /// Without this, every FindUnit/GetUnitPosition/MoveUnit call
        /// would scan the entire grid.
        /// </summary>
        private readonly Dictionary<string, GridPosition> _unitPositions = new();

        /// <summary>Grid width in tiles (columns).</summary>
        public int Width { get; }

        /// <summary>Grid height in tiles (rows).</summary>
        public int Height { get; }

        /// <summary>Terrain configuration for movement costs and combat modifiers.</summary>
        public TerrainConfig TerrainConfig => _terrainConfig;

        /// <summary>
        /// Whether diagonal movement is allowed. False = 4-directional (cardinal only).
        /// Cardinal movement is the standard for tactics games with isometric projection
        /// because diagonal movement on an isometric grid creates ambiguous visual distances.
        /// </summary>
        public bool AllowDiagonalMovement { get; set; } = false;

        public BattleGrid(int width, int height, TerrainConfig terrainConfig = null)
        {
            Width = width;
            Height = height;
            _terrainConfig = terrainConfig ?? new TerrainConfig();
            _tiles = new Tile[width, height];

            // Initialize all tiles as open ground at elevation 0
            for (int col = 0; col < width; col++)
            {
                for (int row = 0; row < height; row++)
                {
                    _tiles[col, row] = new Tile(new GridPosition(col, row));
                }
            }
        }

        // === Tile Access ===

        /// <summary>
        /// Get a tile at the specified position. Returns null if out of bounds.
        /// </summary>
        public Tile GetTile(GridPosition pos)
        {
            if (!IsInBounds(pos)) return null;
            return _tiles[pos.Col, pos.Row];
        }

        /// <summary>
        /// Set tile properties at a position. Used during map setup.
        /// </summary>
        public void SetTile(GridPosition pos, TerrainType terrain, int elevation = 0)
        {
            if (!IsInBounds(pos)) return;
            _tiles[pos.Col, pos.Row].Terrain = terrain;
            _tiles[pos.Col, pos.Row].Elevation = elevation;
        }

        /// <summary>Check if a position is within grid bounds.</summary>
        public bool IsInBounds(GridPosition pos)
        {
            return pos.Col >= 0 && pos.Col < Width && pos.Row >= 0 && pos.Row < Height;
        }

        /// <summary>
        /// Check if a tile's terrain blocks all movement.
        /// This is the authoritative check — reads from TerrainConfig, not Tile properties.
        /// Use this instead of Tile.IsBlocking for movement/placement decisions.
        /// </summary>
        public bool IsTileBlocking(GridPosition pos)
        {
            var tile = GetTile(pos);
            if (tile == null) return true;
            return _terrainConfig.Get(tile.Terrain).BlocksMovement;
        }

        /// <summary>
        /// Check if a tile can be walked onto (terrain allows + not occupied).
        /// Authoritative version that reads from TerrainConfig.
        /// </summary>
        public bool IsTilePassable(GridPosition pos)
        {
            var tile = GetTile(pos);
            if (tile == null) return false;
            return !_terrainConfig.Get(tile.Terrain).BlocksMovement && !tile.IsOccupied;
        }

        // === Occupancy ===

        /// <summary>
        /// Place a unit on a tile. Returns false if tile is occupied or impassable.
        /// </summary>
        public bool PlaceUnit(string unitId, GridPosition pos)
        {
            var tile = GetTile(pos);
            if (tile == null || tile.IsOccupied || IsTileBlocking(pos)) return false;

            tile.OccupantId = unitId;
            _unitPositions[unitId] = pos;
            return true;
        }

        /// <summary>
        /// Remove a unit from its current tile.
        /// </summary>
        public bool RemoveUnit(string unitId)
        {
            if (!_unitPositions.ContainsKey(unitId)) return false;

            var pos = _unitPositions[unitId];
            var tile = GetTile(pos);
            if (tile != null) tile.OccupantId = null;

            _unitPositions.Remove(unitId);
            return true;
        }

        /// <summary>
        /// Move a unit from its current position to a new position.
        /// Returns false if the target tile is occupied or impassable.
        /// Does NOT validate movement range — caller must check first.
        /// </summary>
        public bool MoveUnit(string unitId, GridPosition to)
        {
            if (!_unitPositions.ContainsKey(unitId)) return false;

            var fromPos = _unitPositions[unitId];
            var fromTile = GetTile(fromPos);
            var toTile = GetTile(to);

            if (toTile == null || toTile.IsOccupied || IsTileBlocking(to)) return false;

            if (fromTile != null) fromTile.OccupantId = null;
            toTile.OccupantId = unitId;
            _unitPositions[unitId] = to;
            return true;
        }

        /// <summary>
        /// Find the tile a unit is currently on. Returns null if not found.
        /// O(1) via position index.
        /// </summary>
        public Tile FindUnit(string unitId)
        {
            if (!_unitPositions.ContainsKey(unitId)) return null;
            return GetTile(_unitPositions[unitId]);
        }

        /// <summary>
        /// Get the position of a unit. Returns null if not found.
        /// O(1) via position index.
        /// </summary>
        public GridPosition? GetUnitPosition(string unitId)
        {
            if (_unitPositions.ContainsKey(unitId))
                return _unitPositions[unitId];
            return null;
        }

        /// <summary>
        /// Get all unit IDs currently on the grid.
        /// </summary>
        public IReadOnlyCollection<string> AllUnitIds => _unitPositions.Keys;

        // === Adjacency ===

        /// <summary>
        /// Get all valid neighbor positions for a tile.
        /// Respects AllowDiagonalMovement setting and grid bounds.
        /// </summary>
        public List<GridPosition> GetNeighbors(GridPosition pos)
        {
            var offsets = AllowDiagonalMovement ? GridPosition.AllOffsets : GridPosition.CardinalOffsets;
            var neighbors = new List<GridPosition>();

            foreach (var offset in offsets)
            {
                var neighbor = pos.Add(offset);
                if (IsInBounds(neighbor))
                    neighbors.Add(neighbor);
            }

            return neighbors;
        }

        /// <summary>
        /// Check if two positions are adjacent (within 1 tile, respecting movement mode).
        /// </summary>
        public bool AreAdjacent(GridPosition a, GridPosition b)
        {
            var offsets = AllowDiagonalMovement ? GridPosition.AllOffsets : GridPosition.CardinalOffsets;

            foreach (var offset in offsets)
            {
                if (a.Add(offset) == b) return true;
            }
            return false;
        }

        /// <summary>
        /// Get all units adjacent to a position. For assist and bond trigger checks.
        /// </summary>
        public List<string> GetAdjacentUnits(GridPosition pos)
        {
            var units = new List<string>();
            foreach (var neighbor in GetNeighbors(pos))
            {
                var tile = GetTile(neighbor);
                if (tile != null && tile.IsOccupied)
                    units.Add(tile.OccupantId);
            }
            return units;
        }

        // === Movement Range (Dijkstra) ===

        /// <summary>
        /// Calculate all tiles reachable from a position within a movement budget.
        /// Uses Dijkstra's algorithm with terrain-aware movement costs.
        /// 
        /// Returns a dictionary of reachable positions mapped to their movement cost.
        /// The starting position is included with cost 0.
        /// 
        /// Units can move through friendly units but not stop on occupied tiles.
        /// The moveThroughIds set specifies which unit IDs can be moved through
        /// (typically allies).
        /// </summary>
        public Dictionary<GridPosition, int> GetMovementRange(
            GridPosition start, int movementBudget, HashSet<string> moveThroughIds = null)
        {
            var costs = new Dictionary<GridPosition, int>();
            var frontier = new SortedSet<(int Cost, int Order, GridPosition Pos)>();
            int insertOrder = 0;

            costs[start] = 0;
            frontier.Add((0, insertOrder++, start));

            while (frontier.Count > 0)
            {
                var current = frontier.Min;
                frontier.Remove(current);
                var (currentCost, _, currentPos) = current;

                if (currentCost > costs.GetValueOrDefault(currentPos, int.MaxValue))
                    continue;

                foreach (var neighborPos in GetNeighbors(currentPos))
                {
                    var neighborTile = GetTile(neighborPos);
                    if (neighborTile == null) continue;

                    // Can't enter blocking terrain (reads from TerrainConfig)
                    if (IsTileBlocking(neighborPos)) continue;

                    // Can move through friendly units but not enemies
                    if (neighborTile.IsOccupied)
                    {
                        bool canMoveThrough = moveThroughIds != null &&
                            moveThroughIds.Contains(neighborTile.OccupantId);
                        if (!canMoveThrough) continue;
                    }

                    var currentTile = GetTile(currentPos);
                    int stepCost = _terrainConfig.GetMovementCost(currentTile, neighborTile);
                    if (stepCost == int.MaxValue) continue;

                    // Overflow-safe addition
                    int totalCost = SafeAdd(currentCost, stepCost);
                    if (totalCost > movementBudget) continue;

                    if (totalCost < costs.GetValueOrDefault(neighborPos, int.MaxValue))
                    {
                        costs[neighborPos] = totalCost;
                        frontier.Add((totalCost, insertOrder++, neighborPos));
                    }
                }
            }

            return costs;
        }

        /// <summary>
        /// Get tiles a unit can actually stop on (reachable AND not occupied).
        /// This is what the UI highlights as valid move targets.
        /// </summary>
        public List<GridPosition> GetValidMoveTargets(
            GridPosition start, int movementBudget, HashSet<string> moveThroughIds = null)
        {
            var range = GetMovementRange(start, movementBudget, moveThroughIds);

            return range.Keys
                .Where(pos =>
                {
                    if (pos == start) return false; // Can't "move" to current position
                    var tile = GetTile(pos);
                    return tile != null && !tile.IsOccupied;
                })
                .ToList();
        }

        // === Pathfinding (A*) ===

        /// <summary>
        /// Find the shortest path from start to goal using A*.
        /// Returns the path as a list of positions (including start and goal),
        /// or null if no path exists.
        /// 
        /// Uses terrain-aware movement costs. The path respects occupancy —
        /// units can path through moveThroughIds but the goal must be unoccupied.
        /// </summary>
        public List<GridPosition> FindPath(
            GridPosition start, GridPosition goal,
            int maxCost = int.MaxValue, HashSet<string> moveThroughIds = null)
        {
            if (!IsInBounds(start) || !IsInBounds(goal)) return null;

            var goalTile = GetTile(goal);
            if (goalTile == null || IsTileBlocking(goal) || goalTile.IsOccupied) return null;

            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            var costSoFar = new Dictionary<GridPosition, int>();
            var frontier = new SortedSet<(int Priority, int Order, GridPosition Pos)>();
            int insertOrder = 0;

            costSoFar[start] = 0;
            frontier.Add((0, insertOrder++, start));

            while (frontier.Count > 0)
            {
                var current = frontier.Min;
                frontier.Remove(current);
                var currentPos = current.Pos;

                if (currentPos == goal)
                    return ReconstructPath(cameFrom, start, goal);

                foreach (var neighborPos in GetNeighbors(currentPos))
                {
                    var neighborTile = GetTile(neighborPos);
                    if (neighborTile == null || IsTileBlocking(neighborPos)) continue;

                    // Goal tile must be unoccupied, but intermediate tiles with
                    // friendly units can be pathed through
                    if (neighborTile.IsOccupied && neighborPos != goal)
                    {
                        bool canMoveThrough = moveThroughIds != null &&
                            moveThroughIds.Contains(neighborTile.OccupantId);
                        if (!canMoveThrough) continue;
                    }
                    else if (neighborTile.IsOccupied && neighborPos == goal)
                    {
                        continue; // Can't path to occupied goal
                    }

                    var currentTile = GetTile(currentPos);
                    int stepCost = _terrainConfig.GetMovementCost(currentTile, neighborTile);
                    if (stepCost == int.MaxValue) continue;

                    // Overflow-safe addition
                    int newCost = SafeAdd(costSoFar[currentPos], stepCost);
                    if (newCost > maxCost) continue;

                    if (newCost < costSoFar.GetValueOrDefault(neighborPos, int.MaxValue))
                    {
                        costSoFar[neighborPos] = newCost;
                        int priority = SafeAdd(newCost, neighborPos.ManhattanDistance(goal));
                        cameFrom[neighborPos] = currentPos;
                        frontier.Add((priority, insertOrder++, neighborPos));
                    }
                }
            }

            return null; // No path found
        }

        private List<GridPosition> ReconstructPath(
            Dictionary<GridPosition, GridPosition> cameFrom,
            GridPosition start, GridPosition goal)
        {
            var path = new List<GridPosition>();
            var current = goal;

            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(start);
            path.Reverse();
            return path;
        }

        // === Line of Sight ===

        /// <summary>
        /// Check if there is clear line of sight from one position to another.
        /// Uses Bresenham's line algorithm on the grid.
        /// 
        /// Line of sight is blocked by:
        /// - Tiles with BlocksLineOfSight terrain (walls, destructibles)
        /// - Tiles with elevation higher than BOTH the source and target elevations
        ///   (a hill between two valleys blocks sight)
        /// 
        /// Source and target tiles themselves never block their own LoS.
        /// </summary>
        public bool HasLineOfSight(GridPosition from, GridPosition to)
        {
            if (from == to) return true;

            var fromTile = GetTile(from);
            var toTile = GetTile(to);
            if (fromTile == null || toTile == null) return false;

            int maxElevation = Math.Max(fromTile.Elevation, toTile.Elevation);

            // Bresenham's line
            var line = GetLine(from, to);

            // Check intermediate tiles (skip first and last — source and target)
            for (int i = 1; i < line.Count - 1; i++)
            {
                var tile = GetTile(line[i]);
                if (tile == null) return false;

                var props = _terrainConfig.Get(tile.Terrain);
                if (props.BlocksLineOfSight) return false;

                // Elevation blocking: tile higher than both endpoints blocks sight
                if (tile.Elevation > maxElevation) return false;
            }

            return true;
        }

        /// <summary>
        /// Bresenham's line algorithm. Returns all grid positions along the line
        /// from start to end (inclusive).
        /// </summary>
        public static List<GridPosition> GetLine(GridPosition from, GridPosition to)
        {
            var line = new List<GridPosition>();

            int col0 = from.Col, row0 = from.Row;
            int col1 = to.Col, row1 = to.Row;

            int dc = Math.Abs(col1 - col0);
            int dr = Math.Abs(row1 - row0);
            int sc = col0 < col1 ? 1 : -1;
            int sr = row0 < row1 ? 1 : -1;
            int err = dc - dr;

            while (true)
            {
                line.Add(new GridPosition(col0, row0));
                if (col0 == col1 && row0 == row1) break;

                int e2 = 2 * err;
                if (e2 > -dr)
                {
                    err -= dr;
                    col0 += sc;
                }
                if (e2 < dc)
                {
                    err += dc;
                    row0 += sr;
                }
            }

            return line;
        }

        // === Range Queries ===

        /// <summary>
        /// Get all tiles within range from a position using Manhattan distance (diamond shape).
        /// This is the default for attack range checks.
        /// Does NOT check line of sight — call HasLineOfSight separately for ranged attacks.
        /// </summary>
        public List<GridPosition> GetTilesInRange(GridPosition center, int range)
        {
            return GetTilesInArea(center, range, AreaShape.Diamond);
        }

        /// <summary>
        /// Get all tiles within range of a center point using the specified area shape.
        /// Excludes the center tile itself.
        /// 
        /// Diamond: Manhattan distance (|dc| + |dr| &lt;= range). Natural grid shape.
        /// Square: Chebyshev distance (max(|dc|, |dr|) &lt;= range). Includes diagonals.
        /// Cross: Cardinal axes only (dc == 0 or dr == 0, within range).
        /// </summary>
        public List<GridPosition> GetTilesInArea(GridPosition center, int range, AreaShape shape)
        {
            var tiles = new List<GridPosition>();

            for (int col = center.Col - range; col <= center.Col + range; col++)
            {
                for (int row = center.Row - range; row <= center.Row + range; row++)
                {
                    var pos = new GridPosition(col, row);
                    if (!IsInBounds(pos)) continue;
                    if (pos == center) continue;

                    int dc = Math.Abs(col - center.Col);
                    int dr = Math.Abs(row - center.Row);

                    bool inArea = shape switch
                    {
                        AreaShape.Diamond => (dc + dr) <= range,
                        AreaShape.Square => Math.Max(dc, dr) <= range,
                        AreaShape.Cross => (dc == 0 || dr == 0) && (dc + dr) <= range,
                        _ => (dc + dr) <= range
                    };

                    if (inArea)
                        tiles.Add(pos);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Get all tiles within range that have line of sight from the center.
        /// This is the actual set of attackable tiles for ranged forms.
        /// </summary>
        public List<GridPosition> GetTilesInRangeWithLoS(GridPosition center, int range)
        {
            return GetTilesInRange(center, range)
                .Where(pos => HasLineOfSight(center, pos))
                .ToList();
        }

        /// <summary>
        /// Get the MAXIMUM possible attack range from a position, including elevation bonus.
        /// This is for UI range preview only — the actual range bonus for a specific target
        /// depends on the target's elevation and is calculated per-target by the combat resolver.
        /// </summary>
        public int GetEffectiveRange(GridPosition attackerPos, int baseRange)
        {
            var tile = GetTile(attackerPos);
            if (tile == null) return baseRange;

            return baseRange + (tile.Elevation * _terrainConfig.RangeBonusPerElevation);
        }

        /// <summary>
        /// Get the actual range bonus for a specific attacker→target pair.
        /// Only grants bonus if the attacker is above the target.
        /// Used by the combat resolver for actual range checks.
        /// </summary>
        public int GetRangeBonusAgainst(GridPosition attackerPos, GridPosition targetPos, int baseRange)
        {
            var attackerTile = GetTile(attackerPos);
            var targetTile = GetTile(targetPos);
            if (attackerTile == null || targetTile == null) return baseRange;

            int bonus = _terrainConfig.GetElevationRangeBonus(attackerTile.Elevation, targetTile.Elevation);
            return baseRange + bonus;
        }

        // === Terrain Queries ===

        /// <summary>
        /// Get all tiles with a specific terrain type. Used by the monster gimmick
        /// system to find hazard tiles, destructibles, etc.
        /// </summary>
        public List<GridPosition> GetTilesOfType(TerrainType terrain)
        {
            var tiles = new List<GridPosition>();

            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    if (_tiles[col, row].Terrain == terrain)
                        tiles.Add(_tiles[col, row].Position);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Destroy a destructible tile. Changes it to Open terrain.
        /// Returns false if the tile isn't destructible.
        /// </summary>
        public bool DestroyTerrain(GridPosition pos)
        {
            var tile = GetTile(pos);
            if (tile == null || !tile.IsDestructible) return false;

            tile.Terrain = TerrainType.Open;
            return true;
        }

        /// <summary>
        /// Set a tile to hazard terrain. Used by monster gimmicks and abilities.
        /// Does not affect blocking tiles.
        /// </summary>
        public void SetHazard(GridPosition pos)
        {
            var tile = GetTile(pos);
            if (tile == null || IsTileBlocking(pos)) return;

            tile.Terrain = TerrainType.Hazard;
        }

        // === Helpers ===

        /// <summary>
        /// Overflow-safe integer addition. Returns int.MaxValue instead of wrapping.
        /// Used in pathfinding and movement range to prevent negative costs from overflow.
        /// </summary>
        private static int SafeAdd(int a, int b)
        {
            if (a == int.MaxValue || b == int.MaxValue) return int.MaxValue;
            long result = (long)a + b;
            return result > int.MaxValue ? int.MaxValue : (int)result;
        }
    }
}
