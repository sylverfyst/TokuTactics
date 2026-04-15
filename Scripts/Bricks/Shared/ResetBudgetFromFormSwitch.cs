using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Resets action economy after a form switch.
    /// Grants a new move + action. CanFormSwitch stays as-is.
    /// </summary>
    public static class ResetBudgetFromFormSwitch
    {
        public static void Execute(ActionBudget budget)
        {
            budget.CanMove = true;
            budget.CanAct = true;
        }
    }
}
