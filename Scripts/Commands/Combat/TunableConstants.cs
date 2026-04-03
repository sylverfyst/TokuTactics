namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Frozen game balance constants for damage resolution.
    /// All tunable parameters that affect combat calculations.
    /// These can be modified for game balance without changing code structure.
    /// </summary>
    public record TunableConstants
    {
        public float BaseDodge { get; init; } = 0.02f;
        public float LckDodgeScale { get; init; } = 0.003f;
        public float BaseCrit { get; init; } = 0.05f;
        public float LckCritScale { get; init; } = 0.005f;
        public float CritMultiplier { get; init; } = 1.5f;
        public float SameTypeBonus { get; init; } = 1.25f;
        public float StrongMultiplier { get; init; } = 1.5f;
        public float WeakMultiplier { get; init; } = 0.5f;
        public float DoubleStrongMultiplier { get; init; } = 2.0f;
        public float DoubleWeakMultiplier { get; init; } = 0.25f;
    }
}
