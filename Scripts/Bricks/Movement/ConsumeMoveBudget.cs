using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Bricks.Movement
{
    /// <summary>
    /// Brick: Consumes the movement action from a unit's action budget.
    /// NOTE: This brick mutates state (ActionBudget), which is acceptable for
    /// budget consumption as it's a controlled side effect.
    /// </summary>
    public static class ConsumeMoveBudget
    {
        /// <summary>
        /// Consumes the movement action from the budget.
        /// </summary>
        /// <param name="budget">The unit's action budget</param>
        public static void Execute(ActionBudget budget)
        {
            if (budget == null)
            {
                return;
            }

            budget.ConsumeMove();
        }
    }
}
