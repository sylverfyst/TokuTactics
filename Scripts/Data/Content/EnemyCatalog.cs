using TokuTactics.Core.Grid;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Enemies.Gimmicks;
using TokuTactics.Entities.Enemies.Gimmicks.Triggers;
using TokuTactics.Entities.Enemies.Gimmicks.Behaviors;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Vertical slice enemy definitions.
    /// 
    /// Foot soldiers: Putty (typeless), Blaze Grunt (fire-typed).
    /// Monster: Frost Wyrm — ice terrain hazard gimmick, exercises terrain modification.
    /// Lieutenant: Shadow Commander — dark blade weapon + rotating poison aura gimmick.
    /// 
    /// All names are placeholders.
    /// </summary>
    public static class EnemyCatalog
    {
        // === Foot Soldiers ===

        /// <summary>
        /// Typeless grunt. No type advantage or disadvantage. Filler enemy.
        /// </summary>
        public static EnemyData Putty() => new EnemyData(
            id: "foot_putty",
            name: "Putty",
            tier: EnemyTier.FootSoldier,
            type: null, // Normal — neutral against everything
            stats: StatBlock.Create(str: 5, def: 3, spd: 4, mag: 2),
            maxHealth: 25f,
            basicAttackPower: 1.0f,
            basicAttackRange: 1,
            movementRange: 4,
            behaviorTreeId: "bt_grunt",
            aggressiveBehaviorTreeId: "bt_grunt_aggressive",
            aggressionThreshold: 0.3f);

        /// <summary>
        /// Fire-typed grunt. Strong against Frost, weak against Torrent.
        /// Creates type tension — fielding the wrong form gets punished.
        /// </summary>
        public static EnemyData BlazeGrunt() => new EnemyData(
            id: "foot_blaze_grunt",
            name: "Blaze Grunt",
            tier: EnemyTier.FootSoldier,
            type: ElementalType.Blaze,
            stats: StatBlock.Create(str: 7, def: 3, spd: 5, mag: 3),
            maxHealth: 30f,
            basicAttackPower: 1.2f,
            basicAttackRange: 1,
            movementRange: 3,
            behaviorTreeId: "bt_grunt",
            aggressiveBehaviorTreeId: "bt_grunt_aggressive",
            aggressionThreshold: 0.25f);

        // === Monster of the Week ===

        /// <summary>
        /// Frost Wyrm: ice-typed monster that converts terrain to hazard tiles.
        /// 
        /// Gimmick: every 2 turns, creates a cross of ice hazard tiles around itself.
        /// This forces Rangers to reposition constantly and creates zoning pressure.
        /// The gimmick has its own action budget — the wyrm can basic attack AND
        /// create hazards in the same turn.
        /// 
        /// Type matchup: Frost is weak to Blaze (the melee form), so Rangers must
        /// close to melee range through the hazard field to exploit the weakness.
        /// </summary>
        public static EnemyData FrostWyrm() => new EnemyData(
            id: "monster_frost_wyrm",
            name: "Frost Wyrm",
            tier: EnemyTier.Monster,
            type: ElementalType.Frost,
            stats: StatBlock.Create(str: 12, def: 10, spd: 5, mag: 9),
            maxHealth: 180f,
            basicAttackPower: 1.4f,
            basicAttackRange: 2,
            movementRange: 2,
            behaviorTreeId: "bt_monster_wyrm",
            aggressiveBehaviorTreeId: "bt_monster_wyrm_aggressive",
            aggressionThreshold: 0.35f,
            gimmick: FrostWyrmGimmick());

        /// <summary>
        /// Frost Wyrm's gimmick: ice hazard cross every 2 turns.
        /// Cross shape forces positional play — cardinal lines from the wyrm are dangerous.
        /// </summary>
        public static GimmickData FrostWyrmGimmick() => new GimmickData(
            id: "gimmick_frost_wyrm_ice_field",
            name: "Ice Field",
            trigger: new EveryNTurnsGimmickTrigger(2),
            behavior: new TerrainModifyGimmickBehavior(
                TerrainType.Hazard, radius: 2, AreaShape.Cross),
            cooldown: 0); // No cooldown — the EveryNTurns trigger IS the pacing

        // === Lieutenant ===

        /// <summary>
        /// Shadow Commander: recurring lieutenant with a dark blade and rotating gimmicks.
        /// 
        /// Weapon: Dark Blade — melee, applies bleed DoT. Fixed across all encounters.
        /// This is the Shadow Commander's identity — the player learns to respect the bleed.
        /// 
        /// Gimmick: rotates per encounter. For the vertical slice, uses Poison Aura —
        /// TurnStart trigger, applies poison to adjacent Rangers.
        /// 
        /// Chooses 2 of 3 actions per turn via utility scoring:
        /// basic attack, Dark Blade, or Poison Aura.
        /// </summary>
        public static EnemyData ShadowCommander() => new EnemyData(
            id: "lt_shadow_commander",
            name: "Shadow Commander",
            tier: EnemyTier.Lieutenant,
            type: ElementalType.Shadow,
            stats: StatBlock.Create(str: 14, def: 12, spd: 8, mag: 10),
            maxHealth: 220f,
            basicAttackPower: 1.3f,
            basicAttackRange: 1,
            movementRange: 3,
            behaviorTreeId: "bt_lieutenant",
            aggressiveBehaviorTreeId: "bt_lieutenant_aggressive",
            aggressionThreshold: 0.25f,
            usesUtilityScoring: true,
            weapon: ShadowCommanderWeapon(),
            gimmick: ShadowCommanderGimmick());

        /// <summary>
        /// Dark Blade: the Shadow Commander's signature weapon.
        /// Moderate damage + bleed DoT. The bleed is the real threat —
        /// it punishes Rangers who don't rotate forms to clear the effect.
        /// </summary>
        public static WeaponData ShadowCommanderWeapon() => new WeaponData(
            "wpn_lt_dark_blade", "Dark Blade", 1.8f, 1,
            statusEffect: new StatusEffectTemplate(
                "eff_bleed", new TurnStartTrigger(), new DamageOverTimeBehavior(6f), 3));

        /// <summary>
        /// Vertical slice gimmick for the Shadow Commander: Poison Aura.
        /// TurnStart trigger — every turn the commander is alive, adjacent Rangers
        /// take poison. Cooldown of 2 means it fires every other turn after first use.
        /// 
        /// In subsequent encounters, this would be swapped for a different gimmick
        /// (the "rotating scheme" design). For the vertical slice, this is the only one.
        /// </summary>
        public static GimmickData ShadowCommanderGimmick() => new GimmickData(
            id: "gimmick_lt_poison_aura",
            name: "Poison Aura",
            trigger: new TurnStartGimmickTrigger(),
            behavior: new StatusEffectGimmickBehavior(
                new StatusEffectTemplate(
                    "eff_poison", new TurnStartTrigger(), new DamageOverTimeBehavior(5f), 2),
                range: 1),
            cooldown: 2);

        // === Lieutenant Variant Helpers ===

        /// <summary>
        /// Create a Shadow Commander variant with a different gimmick.
        /// This is the "rotating scheme" pattern from the GDD — the weapon (Dark Blade)
        /// and stats are the lieutenant's fixed identity, the gimmick changes each encounter.
        /// 
        /// Usage for a new episode:
        ///   EnemyCatalog.ShadowCommanderWithGimmick(
        ///     new GimmickData("shield_phase", "Shield Phase",
        ///         new HealthThresholdGimmickTrigger(0.5f),
        ///         new ShieldGimmickBehavior(duration: 3)))
        /// 
        /// The returned EnemyData gets a unique ID based on the gimmick.
        /// </summary>
        public static EnemyData ShadowCommanderWithGimmick(GimmickData gimmick) => new EnemyData(
            id: $"lt_shadow_commander_{gimmick.Id}",
            name: "Shadow Commander",
            tier: EnemyTier.Lieutenant,
            type: ElementalType.Shadow,
            stats: StatBlock.Create(str: 14, def: 12, spd: 8, mag: 10),
            maxHealth: 220f,
            basicAttackPower: 1.3f,
            basicAttackRange: 1,
            movementRange: 3,
            behaviorTreeId: "bt_lieutenant",
            aggressiveBehaviorTreeId: "bt_lieutenant_aggressive",
            aggressionThreshold: 0.25f,
            usesUtilityScoring: true,
            weapon: ShadowCommanderWeapon(),
            gimmick: gimmick);
    }
}
