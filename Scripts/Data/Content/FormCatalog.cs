using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Vertical slice form definitions.
    /// 
    /// Base form: Normal type, weak all-rounder, entry point for morphing.
    /// Blaze form: Fire/melee, high STR, heavy hitter.
    /// Torrent form: Water/ranged, balanced stats, long reach.
    /// Frost form: Ice/control, high MAG, status specialist.
    /// 
    /// All names are placeholders — rename to fit the game's fiction.
    /// </summary>
    public static class FormCatalog
    {
        // === Base Form ===

        public static FormData BaseForm() => new FormData(
            id: "form_base",
            name: "Base Form",
            type: ElementalType.Normal,
            baseStats: StatBlock.Create(str: 6, def: 6, spd: 6, mag: 4, cha: 5, lck: 5),
            statsPerLevel: StatBlock.Create(str: 1, def: 1, spd: 1, mag: 1, cha: 1, lck: 1),
            baseHealth: 60f,
            healthPerLevel: 5f,
            movementRange: 4,
            basicAttackRange: 1,
            basicAttackPower: 1.0f,
            cooldownDuration: 1,
            weaponA: new WeaponData(
                "wpn_base_sidearm", "Sidearm", 0.8f, 1, statusEffect: null),
            weaponB: new WeaponData(
                "wpn_base_baton", "Baton", 0.6f, 1, statusEffect: null));

        // === Blaze Form (Fire / Melee) ===

        public static FormData BlazeForm() => new FormData(
            id: "form_blaze",
            name: "Blaze Form",
            type: ElementalType.Blaze,
            baseStats: StatBlock.Create(str: 14, def: 10, spd: 7, mag: 5, cha: 6, lck: 6),
            statsPerLevel: StatBlock.Create(str: 3, def: 2, spd: 1, mag: 1, cha: 1, lck: 1),
            baseHealth: 100f,
            healthPerLevel: 10f,
            movementRange: 3,
            basicAttackRange: 1,
            basicAttackPower: 1.5f,
            cooldownDuration: 3,
            weaponA: BlazeWeaponA(),
            weaponB: BlazeWeaponB());

        /// <summary>Heavy Blade: high raw damage, no status effect. The straightforward option.</summary>
        public static WeaponData BlazeWeaponA() => new WeaponData(
            "wpn_blaze_heavy_blade", "Heavy Blade", 2.2f, 1, statusEffect: null);

        /// <summary>Flame Sword: moderate damage + burn DoT. The sustained damage option.</summary>
        public static WeaponData BlazeWeaponB() => new WeaponData(
            "wpn_blaze_flame_sword", "Flame Sword", 1.6f, 1,
            statusEffect: new StatusEffectTemplate(
                "eff_burn", new TurnStartTrigger(), new DamageOverTimeBehavior(8f), 3));

        // === Torrent Form (Water / Ranged) ===

        public static FormData TorrentForm() => new FormData(
            id: "form_torrent",
            name: "Torrent Form",
            type: ElementalType.Torrent,
            baseStats: StatBlock.Create(str: 10, def: 8, spd: 9, mag: 8, cha: 7, lck: 6),
            statsPerLevel: StatBlock.Create(str: 2, def: 1, spd: 2, mag: 2, cha: 1, lck: 1),
            baseHealth: 80f,
            healthPerLevel: 7f,
            movementRange: 4,
            basicAttackRange: 3,
            basicAttackPower: 1.2f,
            cooldownDuration: 3,
            weaponA: TorrentWeaponA(),
            weaponB: TorrentWeaponB());

        /// <summary>Hydro Cannon: long range, solid damage. The safe poke option.</summary>
        public static WeaponData TorrentWeaponA() => new WeaponData(
            "wpn_torrent_hydro_cannon", "Hydro Cannon", 1.8f, 3, statusEffect: null);

        /// <summary>Tide Staff: medium range, applies slow. The control option.</summary>
        public static WeaponData TorrentWeaponB() => new WeaponData(
            "wpn_torrent_tide_staff", "Tide Staff", 1.3f, 2,
            statusEffect: new StatusEffectTemplate(
                "eff_slow", new TurnStartTrigger(), new MovementReductionBehavior(0.5f), 2));

        // === Frost Form (Ice / Control) ===

        public static FormData FrostForm() => new FormData(
            id: "form_frost",
            name: "Frost Form",
            type: ElementalType.Frost,
            baseStats: StatBlock.Create(str: 6, def: 9, spd: 7, mag: 14, cha: 8, lck: 5),
            statsPerLevel: StatBlock.Create(str: 1, def: 2, spd: 1, mag: 3, cha: 1, lck: 1),
            baseHealth: 75f,
            healthPerLevel: 6f,
            movementRange: 3,
            basicAttackRange: 2,
            basicAttackPower: 1.0f,
            cooldownDuration: 4,
            weaponA: FrostWeaponA(),
            weaponB: FrostWeaponB());

        /// <summary>Ice Lance: moderate damage + stun. The lockdown option.</summary>
        public static WeaponData FrostWeaponA() => new WeaponData(
            "wpn_frost_ice_lance", "Ice Lance", 1.4f, 2,
            statusEffect: new StatusEffectTemplate(
                "eff_stun", new InstantTrigger(), new StunBehavior(), 1));

        /// <summary>Blizzard Rod: low damage + heavy slow. The area denial option.</summary>
        public static WeaponData FrostWeaponB() => new WeaponData(
            "wpn_frost_blizzard_rod", "Blizzard Rod", 1.0f, 2,
            statusEffect: new StatusEffectTemplate(
                "eff_deep_slow", new TurnStartTrigger(), new MovementReductionBehavior(0.3f), 3));
    }
}
