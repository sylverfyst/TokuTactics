using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Grid;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.CombatResolution;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Command: Orchestrates attack validation and execution.
    /// Pure static orchestrator - no state mutation beyond controlled side effects.
    ///
    /// This command handles the BCO flow for attacks, but delegates the actual
    /// combat resolution to CombatResolver (which handles damage, assists, etc).
    /// </summary>
    public static class ExecuteAttack
    {
        /// <summary>
        /// Executes an attack with full validation.
        /// </summary>
        /// <param name="attackerPos">Attacker's grid position</param>
        /// <param name="targetPos">Target's grid position</param>
        /// <param name="weaponRange">Weapon's range in tiles</param>
        /// <param name="actionBudget">The attacker's action budget</param>
        /// <param name="resolveCombat">Function to resolve the actual combat (delegates to CombatResolver)</param>
        /// <param name="validateRange">Optional brick injection for testing</param>
        /// <param name="checkBudget">Optional brick injection for testing</param>
        /// <param name="consumeAction">Optional brick injection for testing</param>
        /// <returns>Result indicating success or failure with combat details</returns>
        public static AttackResult Execute(
            GridPosition attackerPos,
            GridPosition targetPos,
            int weaponRange,
            ActionBudget actionBudget,
            // Combat resolution function - caller provides the actual combat logic
            Func<CombatResult> resolveCombat,
            // Optional brick injections for testing
            Func<GridPosition, GridPosition, int, bool> validateRange = null,
            Func<ActionBudget, bool> checkBudget = null,
            Func<ActionBudget, bool> consumeAction = null)
        {
            // Initialize defaults if not provided
            validateRange ??= ValidateAttackRange.Execute;
            checkBudget ??= CheckAttackBudget.Execute;
            consumeAction ??= ConsumeActionBudget.Execute;

            // Validate target is in attack range
            if (!validateRange(attackerPos, targetPos, weaponRange))
            {
                return AttackResult.CreateFailure("Target not in weapon range");
            }

            // Check action budget
            if (!checkBudget(actionBudget))
            {
                return AttackResult.CreateFailure("Unit has already used their action");
            }

            // Execute the attack via provided combat resolution function
            CombatResult combatResult = resolveCombat();
            if (combatResult == null)
            {
                return AttackResult.CreateFailure("Failed to resolve combat");
            }

            // Consume action budget
            consumeAction(actionBudget);

            return AttackResult.CreateSuccess(combatResult);
        }
    }
}
