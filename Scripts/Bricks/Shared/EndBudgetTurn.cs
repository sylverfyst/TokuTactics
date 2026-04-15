using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Force ends the turn — sets all action flags to false.
    /// </summary>
    public static class EndBudgetTurn
    {
        public static void Execute(ActionBudget budget)
        {
            budget.CanMove = false;
            budget.CanAct = false;
            budget.CanFormSwitch = false;
        }
    }
}
