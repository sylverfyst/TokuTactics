using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Consumes the action from a unit's action budget.
    /// Returns false if the unit has already acted (guard check).
    /// </summary>
    public static class ConsumeActionBudget
    {
        public static bool Execute(ActionBudget budget)
        {
            if (budget == null) return false;
            if (!budget.CanAct) return false;
            budget.CanAct = false;
            return true;
        }
    }
}
