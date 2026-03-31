namespace TokuTactics.Core.Types
{
    /// <summary>
    /// Represents the dual type of a morphed Ranger: their innate type + their current form's type.
    /// When both types match (same-type bonus), IsSameType is true.
    /// </summary>
    public readonly struct DualType
    {
        public ElementalType RangerType { get; }
        public ElementalType FormType { get; }
        public bool IsSameType => RangerType == FormType;

        public DualType(ElementalType rangerType, ElementalType formType)
        {
            RangerType = rangerType;
            FormType = formType;
        }

        /// <summary>
        /// Creates a single-type representation for unmorphed Rangers or single-typed enemies.
        /// </summary>
        public static DualType Single(ElementalType type) => new DualType(type, type);
    }
}
