using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Brick: Checks if a unit has actions remaining in their budget.
    /// </summary>
    public static class CheckAttackBudget
    {
        /// <summary>
        /// Checks if the unit can perform an attack action.
        /// </summary>
        /// <param name="budget">The unit's action budget for this turn</param>
        /// <returns>True if the unit can act</returns>
        public static bool Execute(ActionBudget budget)
        {
            if (budget == null)
            {
                return false;
            }

            return budget.CanAct;
        }
    }
}
