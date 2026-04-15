using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Consumes the action for morphing. Ends the turn entirely —
    /// sets CanMove, CanAct, and CanFormSwitch all to false.
    /// Returns false if the unit cannot act.
    /// </summary>
    public static class ConsumeMorphAction
    {
        public static bool Execute(ActionBudget budget)
        {
            if (!budget.CanAct) return false;
            budget.CanMove = false;
            budget.CanAct = false;
            budget.CanFormSwitch = false;
            return true;
        }
    }
}
