using TokuTactics.Core.Types;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Frozen parameter contract for ResolveDamageRoll command.
    /// All inputs needed to resolve a complete damage roll including dodge, crit,
    /// type matchup, same-type bonus, and combo scaling.
    /// </summary>
    public record ResolveDamageRollParams
    {
        public float AttackerStr { get; init; }
        public float AttackerLck { get; init; }
        public float DefenderDef { get; init; }
        public float DefenderLck { get; init; }
        public float ActionPower { get; init; }
        public ElementalType AttackType { get; init; }
        public ElementalType DefenderType { get; init; }
        public DualType? DefenderDualType { get; init; }
        public float ComboMultiplier { get; init; }
        public bool HasSameTypeBonus { get; init; }
    }
}
