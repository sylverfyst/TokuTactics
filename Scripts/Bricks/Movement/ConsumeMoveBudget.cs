using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Bricks.Movement
{
    /// <summary>
    /// Consumes the movement action from a unit's action budget.
    /// Returns false if the unit has already moved (guard check).
    /// </summary>
    public static class ConsumeMoveBudget
    {
        public static bool Execute(ActionBudget budget)
        {
            if (budget == null) return false;
            if (!budget.CanMove) return false;
            budget.CanMove = false;
            return true;
        }
    }
}
