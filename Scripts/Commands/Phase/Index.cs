// Commands/Phase Index
// Re-exports all phase transition commands for discovery

namespace TokuTactics.Commands.Phase
{
    // ExecutePhaseTransition - Transitions from end of player phase through enemy phase to next player turn
    // PhaseTransitionResult - Result struct for ExecutePhaseTransition
    // InitializeMission - Validates inputs and resolves defeat target set
    // InitializeMissionResult - Result struct for InitializeMission
    // ExecuteRoundStart - Processes round-start effects (cooldowns, status effects, combos) and checks win/loss
    // RoundStartResult - Result struct for ExecuteRoundStart
    // ResolveWinLoss - Checks both win and loss conditions
    // WinLossResult - Result struct for ResolveWinLoss
    // ProcessRoundStatusEffects - Ticks status effects on all units and applies health changes
    // StatusEffectRoundResult - Result struct for ProcessRoundStatusEffects
    // DemorphEventData - Event data for form death/demorph
    // AggressionEventData - Event data for enemy aggression trigger
}
