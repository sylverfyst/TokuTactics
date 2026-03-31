namespace TokuTactics.Core.Grid
{
    /// <summary>
    /// A single tile on the battle grid. Contains terrain, elevation, and occupancy state.
    /// Tiles are mutable — dynamic terrain changes modify them in place.
    /// 
    /// The grid owns tiles. External systems query tile state through the grid,
    /// never hold direct tile references (tiles may be replaced during terrain changes).
    /// 
    /// IMPORTANT: For movement/blocking/passability checks, use BattleGrid.IsTileBlocking()
    /// and BattleGrid.IsTilePassable(). These consult TerrainConfig as the single source of truth.
    /// Tile only stores raw data — the grid interprets it.
    /// </summary>
    public class Tile
    {
        /// <summary>This tile's position on the grid.</summary>
        public GridPosition Position { get; }

        /// <summary>Terrain type affecting movement cost and combat.</summary>
        public TerrainType Terrain { get; set; }

        /// <summary>
        /// Elevation level (0 = ground). Higher elevation grants range bonuses
        /// to ranged forms and affects line of sight.
        /// </summary>
        public int Elevation { get; set; }

        /// <summary>
        /// ID of the unit currently occupying this tile, or null if empty.
        /// Only one unit per tile.
        /// </summary>
        public string OccupantId { get; set; }

        /// <summary>Whether a unit is standing on this tile.</summary>
        public bool IsOccupied => OccupantId != null;

        /// <summary>Whether this tile provides cover against ranged attacks.</summary>
        public bool ProvidesCover => Terrain == TerrainType.Cover || Terrain == TerrainType.Destructible;

        /// <summary>Whether this tile deals damage to occupants.</summary>
        public bool IsHazard => Terrain == TerrainType.Hazard;

        /// <summary>Whether this tile can be destroyed.</summary>
        public bool IsDestructible => Terrain == TerrainType.Destructible;

        public Tile(GridPosition position, TerrainType terrain = TerrainType.Open, int elevation = 0)
        {
            Position = position;
            Terrain = terrain;
            Elevation = elevation;
        }
    }
}
