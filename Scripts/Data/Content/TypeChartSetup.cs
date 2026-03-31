using TokuTactics.Core.Types;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Configures the type matchup chart for the vertical slice.
    /// 
    /// Core triangle for the slice: Blaze > Frost > Torrent > Blaze.
    /// Additional matchups filled in for completeness. Normal has no entries —
    /// its neutrality comes from the absence of relationships.
    /// 
    /// All matchups are bidirectional: AddStrength(A, B) means A is strong against B
    /// AND B is weak against A.
    /// </summary>
    public static class TypeChartSetup
    {
        public static TypeChart Create()
        {
            var chart = new TypeChart();

            // === Core Triangle (vertical slice focus) ===
            chart.AddStrength(ElementalType.Blaze, ElementalType.Frost);
            chart.AddStrength(ElementalType.Frost, ElementalType.Torrent);
            chart.AddStrength(ElementalType.Torrent, ElementalType.Blaze);

            // === Extended Matchups ===
            chart.AddStrength(ElementalType.Blaze, ElementalType.Gale);
            chart.AddStrength(ElementalType.Torrent, ElementalType.Stone);
            chart.AddStrength(ElementalType.Gale, ElementalType.Stone);
            chart.AddStrength(ElementalType.Volt, ElementalType.Torrent);
            chart.AddStrength(ElementalType.Volt, ElementalType.Gale);
            chart.AddStrength(ElementalType.Frost, ElementalType.Gale);
            chart.AddStrength(ElementalType.Stone, ElementalType.Volt);
            chart.AddStrength(ElementalType.Shadow, ElementalType.Radiant);
            chart.AddStrength(ElementalType.Radiant, ElementalType.Shadow);

            // Normal: no entries. Neutral against everything by design.

            return chart;
        }
    }
}
