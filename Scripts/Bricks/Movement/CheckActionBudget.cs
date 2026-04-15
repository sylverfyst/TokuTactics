using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Bricks.Movement
{
    /// <summary>
    /// Brick: Checks if a unit has movement actions remaining in their budget.
    /// </summary>
    public static class CheckActionBudget
    {
        /// <summary>
        /// Checks if the unit can perform a movement action.
        /// </summary>
        /// <param name="budget">The unit's action budget for this turn</param>
        /// <returns>True if the unit can move</returns>
        public static bool Execute(ActionBudget budget)
        {
            if (budget == null)
            {
                return false;
            }

            return budget.CanMove;
        }
    }
}
