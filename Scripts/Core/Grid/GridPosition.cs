using System;

namespace TokuTactics.Core.Grid
{
    /// <summary>
    /// A position on the logical grid. Uses axial coordinates (col, row).
    /// The isometric visual layer converts these to screen space — this layer
    /// only knows about grid logic.
    /// 
    /// Immutable value type. Two positions are equal if col and row match.
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>, IComparable<GridPosition>
    {
        public int Col { get; }
        public int Row { get; }

        public GridPosition(int col, int row)
        {
            Col = col;
            Row = row;
        }

        /// <summary>
        /// Manhattan distance on the grid. Used for range checks before
        /// pathfinding. Does NOT account for elevation or obstacles.
        /// </summary>
        public int ManhattanDistance(GridPosition other)
        {
            return Math.Abs(Col - other.Col) + Math.Abs(Row - other.Row);
        }

        /// <summary>Adjacent position offsets (4-directional).</summary>
        public static readonly GridPosition[] CardinalOffsets = new[]
        {
            new GridPosition(0, -1),  // North
            new GridPosition(1, 0),   // East
            new GridPosition(0, 1),   // South
            new GridPosition(-1, 0)   // West
        };

        /// <summary>All 8 neighbor offsets including diagonals.</summary>
        public static readonly GridPosition[] AllOffsets = new[]
        {
            new GridPosition(0, -1),   // N
            new GridPosition(1, -1),   // NE
            new GridPosition(1, 0),    // E
            new GridPosition(1, 1),    // SE
            new GridPosition(0, 1),    // S
            new GridPosition(-1, 1),   // SW
            new GridPosition(-1, 0),   // W
            new GridPosition(-1, -1)   // NW
        };

        public GridPosition Add(GridPosition offset) => new GridPosition(Col + offset.Col, Row + offset.Row);

        public bool Equals(GridPosition other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Col, Row);
        public override string ToString() => $"({Col},{Row})";

        public int CompareTo(GridPosition other)
        {
            int rowComparison = Row.CompareTo(other.Row);
            if (rowComparison != 0) return rowComparison;
            return Col.CompareTo(other.Col);
        }

        public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
        public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
    }

    /// <summary>
    /// Terrain types that affect movement cost, combat, and abilities.
    /// New terrain types are data — add entries here and configure their
    /// properties on TerrainConfig.
    /// </summary>
    public enum TerrainType
    {
        /// <summary>Normal ground. No special effects.</summary>
        Open,

        /// <summary>Difficult terrain. Increased movement cost.</summary>
        Rough,

        /// <summary>Impassable. Cannot be moved through.</summary>
        Wall,

        /// <summary>Provides cover. Reduces incoming ranged damage.</summary>
        Cover,

        /// <summary>Hazard tile. Deals damage to units that enter or start turn on it.</summary>
        Hazard,

        /// <summary>Elevated terrain. Grants range bonus to ranged forms.</summary>
        HighGround,

        /// <summary>Destructible terrain. Can be destroyed by attacks or abilities.</summary>
        Destructible,

        /// <summary>Water/gap. Impassable by default, some forms may cross.</summary>
        Gap
    }

    /// <summary>
    /// Shape of an area effect on the grid.
    /// Used by gimmick terrain modification, AoE attacks, and any system
    /// that needs to select tiles in an area around a center point.
    /// </summary>
    public enum AreaShape
    {
        /// <summary>Manhattan distance diamond (default). The natural shape for grid tactics.</summary>
        Diamond,

        /// <summary>Chebyshev distance square. Includes diagonals.</summary>
        Square,

        /// <summary>Cardinal cross (+ shape). Only tiles directly along axes from center.</summary>
        Cross
    }
}
