// Bricks/Shared Index
// Re-exports all domain-agnostic shared bricks for discovery

namespace TokuTactics.Bricks.Shared
{
    // ValidateMissionActive - Returns true if mission state is Active
    // ApplyEffectOutputToHealth - Applies damage/healing from one EffectOutput to one health pool
    // GetTargetHealthPool - Returns correct health pool for a ranger (form vs unmorphed)
    // CheckFormDeath - Returns true if morphed ranger's current form health is dead
    // StartBudgetTurn - Resets ActionBudget to fresh turn state
    // ConsumeMorphAction - Consumes action for morphing, ends turn entirely
    // ResetBudgetFromFormSwitch - Restores CanMove and CanAct after form switch
    // ApplyBondRefresh - Applies tier 4 bond refresh (once per round)
    // GiveBondRefresh - Marks unit as having given a bond refresh (once per round)
    // EndBudgetTurn - Force ends the turn, sets all flags to false
}
