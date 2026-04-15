using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Resets an ActionBudget to a fresh turn state.
    /// All actions available, refresh flags cleared.
    /// </summary>
    public static class StartBudgetTurn
    {
        public static void Execute(ActionBudget budget)
        {
            budget.CanMove = true;
            budget.CanAct = true;
            budget.CanFormSwitch = true;
            budget.HasUsedBondRefresh = false;
            budget.HasReceivedBondRefresh = false;
        }
    }
}
