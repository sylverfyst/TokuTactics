using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Bricks.Movement
{
    /// <summary>
    /// Restores the movement action on a unit's action budget.
    /// Used when undoing a move before committing to an attack.
    /// Returns false if budget is null or unit can already move.
    /// </summary>
    public static class RestoreMoveBudget
    {
        public static bool Execute(ActionBudget budget)
        {
            if (budget == null) return false;
            if (budget.CanMove) return false;
            budget.CanMove = true;
            return true;
        }
    }
}
