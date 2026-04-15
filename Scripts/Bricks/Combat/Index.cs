// Re-export all combat bricks for discovery
namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Index for all combat brick operations.
    /// All brick classes are directly importable from this namespace.
    /// </summary>
    public static class CombatBricks
    {
        // This class serves as a discovery point for all combat bricks.
        // Individual bricks are accessible via their static classes:
        // - CalculateBaseDamage
        // - RollDodge
        // - ApplyTypeMatchup
        // - CalculateSameTypeBonus
        // - RollCrit
        // - ApplyComboScaling
        // - ApplyDamageToEnemy
        // - ApplyDamageToRanger
        // - ApplyStatusEffect
        // - CalculateStatusPotency
        // - ValidateReactiveGimmick
    }
}
