using System.Collections.Generic;
using TokuTactics.Core.Grid;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Vertical slice map definition.
    /// 
    /// "Frozen Outpost" — a 12x10 mid-campaign map with terrain variety.
    /// Rangers enter from the south. Foot soldiers hold the middle.
    /// The Frost Wyrm occupies the northern high ground.
    /// The Shadow Commander lurks behind cover on the east flank.
    /// 
    /// Terrain exercises: walls, rough, cover, hazard, high ground, destructible, gap.
    /// The map creates natural lanes and forces choices about which path to take.
    /// </summary>
    public static class MapCatalog
    {
        public static MapDefinition FrozenOutpost()
        {
            var map = new MapDefinition
            {
                Id = "map_frozen_outpost",
                Name = "Frozen Outpost",
                Width = 12,
                Height = 10
            };

            // === Terrain ===

            // Western wall line — forces Rangers east or through the gap
            map.SetTerrain(0, 4, TerrainType.Wall);
            map.SetTerrain(1, 4, TerrainType.Wall);
            map.SetTerrain(2, 4, TerrainType.Wall);
            map.SetTerrain(3, 4, TerrainType.Gap); // Gap in the wall — some forms might cross

            // Central rough patch — slows movement through the middle
            map.SetTerrain(5, 4, TerrainType.Rough);
            map.SetTerrain(6, 4, TerrainType.Rough);
            map.SetTerrain(5, 5, TerrainType.Rough);
            map.SetTerrain(6, 5, TerrainType.Rough);

            // Eastern cover — the Shadow Commander's flank
            map.SetTerrain(9, 3, TerrainType.Cover);
            map.SetTerrain(10, 3, TerrainType.Cover);
            map.SetTerrain(9, 2, TerrainType.Cover);

            // Northern high ground — Frost Wyrm's perch, grants range bonus
            map.SetTerrain(5, 1, TerrainType.HighGround);
            map.SetTerrain(6, 1, TerrainType.HighGround);
            map.SetTerrain(5, 0, TerrainType.HighGround);
            map.SetTerrain(6, 0, TerrainType.HighGround);
            map.SetElevation(5, 1, 2);
            map.SetElevation(6, 1, 2);
            map.SetElevation(5, 0, 2);
            map.SetElevation(6, 0, 2);

            // Destructible barricades — can be broken to open new paths
            map.SetTerrain(4, 6, TerrainType.Destructible);
            map.SetTerrain(7, 6, TerrainType.Destructible);

            // Starting hazard tile — ice patch from the Wyrm's presence
            map.SetTerrain(5, 2, TerrainType.Hazard);
            map.SetTerrain(6, 2, TerrainType.Hazard);

            // === Spawn Positions ===

            // Rangers — southern area
            map.RangerSpawns.Add(new GridPosition(3, 9));
            map.RangerSpawns.Add(new GridPosition(5, 9));
            map.RangerSpawns.Add(new GridPosition(7, 9));
            map.RangerSpawns.Add(new GridPosition(4, 8));
            map.RangerSpawns.Add(new GridPosition(6, 8));

            // Enemy spawns are defined per-episode on EpisodeDefinition,
            // not per-map. The same map can host different episodes.

            return map;
        }
    }

    /// <summary>
    /// Data definition for a map. Describes terrain, elevation, and spawn positions.
    /// The combat system uses this to initialize a BattleGrid and place units.
    /// </summary>
    public class MapDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>Terrain overrides. Tiles not listed default to Open.</summary>
        public List<TerrainPlacement> TerrainPlacements { get; } = new();

        /// <summary>Elevation overrides. Tiles not listed default to 0.</summary>
        public List<ElevationPlacement> ElevationPlacements { get; } = new();

        /// <summary>
        /// Ranger starting positions (ordered: index 0 = first Ranger).
        /// These are spatial (tied to map geometry) so they live here.
        /// Enemy spawns vary per episode and live on EpisodeDefinition.
        /// </summary>
        public List<GridPosition> RangerSpawns { get; } = new();

        public void SetTerrain(int col, int row, TerrainType terrain)
        {
            TerrainPlacements.Add(new TerrainPlacement(new GridPosition(col, row), terrain));
        }

        public void SetElevation(int col, int row, int elevation)
        {
            ElevationPlacements.Add(new ElevationPlacement(new GridPosition(col, row), elevation));
        }

        /// <summary>
        /// Build a BattleGrid from this map definition.
        /// Applies all terrain and elevation overrides.
        /// Does NOT place units — the combat system does that separately.
        /// </summary>
        public BattleGrid BuildGrid(TerrainConfig terrainConfig = null)
        {
            var grid = new BattleGrid(Width, Height, terrainConfig);

            foreach (var placement in TerrainPlacements)
            {
                grid.SetTile(placement.Position, placement.Terrain);
            }

            foreach (var placement in ElevationPlacements)
            {
                var tile = grid.GetTile(placement.Position);
                if (tile != null)
                    tile.Elevation = placement.Elevation;
            }

            return grid;
        }
    }

    public class TerrainPlacement
    {
        public GridPosition Position { get; }
        public TerrainType Terrain { get; }

        public TerrainPlacement(GridPosition position, TerrainType terrain)
        {
            Position = position;
            Terrain = terrain;
        }
    }

    public class ElevationPlacement
    {
        public GridPosition Position { get; }
        public int Elevation { get; }

        public ElevationPlacement(GridPosition position, int elevation)
        {
            Position = position;
            Elevation = elevation;
        }
    }
}
