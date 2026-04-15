// Bricks/Combat Index
// Re-exports all combat bricks for discovery

namespace TokuTactics.Bricks.Combat
{
    // CalculateBaseDamage - Pure STR vs DEF damage calculation
    // RollDodge - LCK-based dodge chance
    // RollCrit - LCK-based crit chance
    // ApplyTypeMatchup - Type effectiveness multipliers
    // CalculateSameTypeBonus - STAB bonus
    // ApplyComboScaling - Combo multiplier
    // ApplyDamageToEnemy - Routes damage to enemy via Enemy.TakeDamage
    // ApplyDamageToRanger - Routes damage to correct ranger health pool
    // ApplyStatusEffect - Applies a StatusEffectInstance to a tracker
    // CalculateStatusPotency - MAG stat to status potency calculation
    // ValidateReactiveGimmick - Checks if enemy should fire reactive gimmick after hit
    // CheckAttackBudget - Checks if unit has actions remaining
    // ConsumeActionBudget - Consumes the action from a unit's budget
    // ValidateAttackRange - Checks if target is within weapon range (Manhattan distance)
}
