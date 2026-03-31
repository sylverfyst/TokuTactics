using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Zords;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Vertical slice zord definitions.
    /// 
    /// Five base zords, one per core Ranger, matching their innate types.
    /// These are the starting zords — recruitable zords come later in the campaign.
    /// 
    /// Ground combat is the focus of the vertical slice. Zords are defined
    /// here for completeness and mecha phase testing, but their stat balance
    /// is preliminary.
    /// 
    /// All names are placeholders.
    /// </summary>
    public static class ZordCatalog
    {
        public static ZordData RedZord() => new ZordData(
            id: "zord_red",
            name: "Red Lion Zord",
            type: ElementalType.Blaze,
            baseStats: StatBlock.Create(str: 16, def: 12, spd: 8, mag: 6),
            statsPerLevel: StatBlock.Create(str: 3, def: 2, spd: 1, mag: 1),
            baseHealth: 200f,
            healthPerLevel: 20f,
            movementRange: 4,
            basicAttackRange: 1,
            basicAttackPower: 2.0f,
            personalAbility: null); // Zord abilities defined later

        public static ZordData BlueZord() => new ZordData(
            id: "zord_blue",
            name: "Blue Shark Zord",
            type: ElementalType.Torrent,
            baseStats: StatBlock.Create(str: 12, def: 10, spd: 10, mag: 10),
            statsPerLevel: StatBlock.Create(str: 2, def: 2, spd: 2, mag: 2),
            baseHealth: 180f,
            healthPerLevel: 18f,
            movementRange: 5,
            basicAttackRange: 2,
            basicAttackPower: 1.6f,
            personalAbility: null);

        public static ZordData YellowZord() => new ZordData(
            id: "zord_yellow",
            name: "Yellow Eagle Zord",
            type: ElementalType.Volt,
            baseStats: StatBlock.Create(str: 10, def: 8, spd: 14, mag: 8),
            statsPerLevel: StatBlock.Create(str: 2, def: 1, spd: 3, mag: 1),
            baseHealth: 160f,
            healthPerLevel: 15f,
            movementRange: 6,
            basicAttackRange: 2,
            basicAttackPower: 1.4f,
            personalAbility: null);

        public static ZordData GreenZord() => new ZordData(
            id: "zord_green",
            name: "Green Bear Zord",
            type: ElementalType.Gale,
            baseStats: StatBlock.Create(str: 12, def: 14, spd: 8, mag: 6),
            statsPerLevel: StatBlock.Create(str: 2, def: 3, spd: 1, mag: 1),
            baseHealth: 220f,
            healthPerLevel: 22f,
            movementRange: 3,
            basicAttackRange: 1,
            basicAttackPower: 1.8f,
            personalAbility: null);

        public static ZordData PinkZord() => new ZordData(
            id: "zord_pink",
            name: "Pink Crane Zord",
            type: ElementalType.Frost,
            baseStats: StatBlock.Create(str: 8, def: 10, spd: 10, mag: 14),
            statsPerLevel: StatBlock.Create(str: 1, def: 2, spd: 2, mag: 3),
            baseHealth: 170f,
            healthPerLevel: 16f,
            movementRange: 4,
            basicAttackRange: 3,
            basicAttackPower: 1.2f,
            personalAbility: null);

        /// <summary>Get all five base zords as an array.</summary>
        public static ZordData[] AllBaseZords() => new[]
        {
            RedZord(), BlueZord(), YellowZord(), GreenZord(), PinkZord()
        };
    }
}
