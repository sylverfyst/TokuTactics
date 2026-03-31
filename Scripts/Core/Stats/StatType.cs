namespace TokuTactics.Core.Stats
{
    /// <summary>
    /// The six core stats that define all combat-relevant attributes.
    /// Each Ranger has a hidden proclivity toward exactly one stat.
    /// </summary>
    public enum StatType
    {
        /// <summary>Offensive damage output.</summary>
        STR,

        /// <summary>Damage reduction and health pool size.</summary>
        DEF,

        /// <summary>Turn order within phase and movement range.</summary>
        SPD,

        /// <summary>
        /// Unmorphed: boosts personal abilities.
        /// Morphed: reduces cooldown duration, scales weapon status effect potency.
        /// </summary>
        MAG,

        /// <summary>Bond growth rate and assist effectiveness.</summary>
        CHA,

        /// <summary>Crit chance, dodge chance, proclivity bonus trigger frequency.</summary>
        LCK
    }
}
