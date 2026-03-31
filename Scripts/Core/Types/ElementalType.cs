namespace TokuTactics.Core.Types
{
    /// <summary>
    /// The nine elemental types. Eight combat types used by Rangers, forms, enemies,
    /// and zords, plus Normal which is purely neutral (no strengths, no weaknesses).
    /// 
    /// Normal is used for typeless foot soldiers and any entity that should have
    /// no type interactions. It is never strong or weak against anything.
    /// Do NOT add Normal to the TypeChart — its neutrality comes from having
    /// no entries, not from explicit neutral declarations.
    /// 
    /// Placeholder names — rename to fit the game's fiction.
    /// </summary>
    public enum ElementalType
    {
        Blaze,
        Torrent,
        Gale,
        Stone,
        Volt,
        Frost,
        Shadow,
        Radiant,

        /// <summary>
        /// Purely neutral type. No strengths, no weaknesses.
        /// Used for typeless foot soldiers and entities outside the type system.
        /// Never add matchup relationships for Normal in the TypeChart.
        /// </summary>
        Normal
    }

    /// <summary>
    /// The three possible outcomes of a type matchup check.
    /// </summary>
    public enum MatchupResult
    {
        Neutral,
        Strong,
        Weak,
        DoubleStrong,
        DoubleWeak
    }
}
