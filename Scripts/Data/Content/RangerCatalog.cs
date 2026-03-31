using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Data.Content.PersonalAbilities;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Vertical slice Ranger definitions.
    /// 
    /// Five core Rangers, each with a unique innate type and personal ability.
    /// Only three are needed for the vertical slice scenario, but all five
    /// are defined for completeness and bond testing.
    /// 
    /// Proclivities are randomized per save — they're assigned at save creation,
    /// not defined here. The catalog provides defaults for testing.
    /// 
    /// All names are placeholders.
    /// </summary>
    public static class RangerCatalog
    {
        /// <summary>
        /// Ranger Red: Blaze innate type. The aggressive frontliner.
        /// Personal ability: Scout Push — shove enemies away during scouting.
        /// </summary>
        public static RangerDefinition Red() => new RangerDefinition
        {
            Id = "ranger_red",
            Name = "Red",
            IntrinsicType = ElementalType.Blaze,
            PersonalAbility = new ScoutPush(),
            UnmorphedStats = StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4, cha: 5, lck: 5),
            UnmorphedMaxHealth = 50f,
            BaseFormData = FormCatalog.BaseForm(),
            DefaultProclivity = StatType.STR
        };

        /// <summary>
        /// Ranger Blue: Torrent innate type. The balanced tactician.
        /// Personal ability: Rally — boost adjacent allies' defense.
        /// </summary>
        public static RangerDefinition Blue() => new RangerDefinition
        {
            Id = "ranger_blue",
            Name = "Blue",
            IntrinsicType = ElementalType.Torrent,
            PersonalAbility = new Rally(),
            UnmorphedStats = StatBlock.Create(str: 6, def: 6, spd: 7, mag: 6, cha: 7, lck: 5),
            UnmorphedMaxHealth = 45f,
            BaseFormData = FormCatalog.BaseForm(),
            DefaultProclivity = StatType.CHA
        };

        /// <summary>
        /// Ranger Yellow: Volt innate type. The fast striker.
        /// Personal ability: Scout Push (placeholder — would be unique in full game).
        /// </summary>
        public static RangerDefinition Yellow() => new RangerDefinition
        {
            Id = "ranger_yellow",
            Name = "Yellow",
            IntrinsicType = ElementalType.Volt,
            PersonalAbility = new ScoutPush(),
            UnmorphedStats = StatBlock.Create(str: 6, def: 4, spd: 9, mag: 5, cha: 6, lck: 7),
            UnmorphedMaxHealth = 40f,
            BaseFormData = FormCatalog.BaseForm(),
            DefaultProclivity = StatType.SPD
        };

        /// <summary>
        /// Ranger Green: Gale innate type. The mobile flanker.
        /// Personal ability: Rally (placeholder — would be unique in full game).
        /// </summary>
        public static RangerDefinition Green() => new RangerDefinition
        {
            Id = "ranger_green",
            Name = "Green",
            IntrinsicType = ElementalType.Gale,
            PersonalAbility = new Rally(),
            UnmorphedStats = StatBlock.Create(str: 7, def: 5, spd: 8, mag: 5, cha: 5, lck: 6),
            UnmorphedMaxHealth = 45f,
            BaseFormData = FormCatalog.BaseForm(),
            DefaultProclivity = StatType.DEF
        };

        /// <summary>
        /// Ranger Pink: Frost innate type. The support caster.
        /// Personal ability: Rally (placeholder — would be unique in full game).
        /// </summary>
        public static RangerDefinition Pink() => new RangerDefinition
        {
            Id = "ranger_pink",
            Name = "Pink",
            IntrinsicType = ElementalType.Frost,
            PersonalAbility = new Rally(),
            UnmorphedStats = StatBlock.Create(str: 4, def: 5, spd: 6, mag: 9, cha: 8, lck: 5),
            UnmorphedMaxHealth = 40f,
            BaseFormData = FormCatalog.BaseForm(),
            DefaultProclivity = StatType.MAG
        };

        /// <summary>Get all five Rangers as an array.</summary>
        public static RangerDefinition[] AllRangers() => new[]
        {
            Red(), Blue(), Yellow(), Green(), Pink()
        };
    }

    /// <summary>
    /// Data needed to construct a Ranger entity.
    /// Separated from the Ranger class so content definitions don't
    /// depend on entity constructors or runtime state.
    /// </summary>
    public class RangerDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ElementalType IntrinsicType { get; set; }
        public IPersonalAbility PersonalAbility { get; set; }
        public StatBlock UnmorphedStats { get; set; }
        public float UnmorphedMaxHealth { get; set; }

        /// <summary>
        /// Base form data for this Ranger. All Rangers use the same base form template
        /// but each gets their own FormInstance with independent level/health.
        /// </summary>
        public FormData BaseFormData { get; set; }

        /// <summary>
        /// Default proclivity for testing. In a real save, this is randomized.
        /// </summary>
        public StatType DefaultProclivity { get; set; }
    }
}
