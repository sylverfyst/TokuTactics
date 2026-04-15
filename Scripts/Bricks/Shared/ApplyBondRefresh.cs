using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Applies a tier 4 bond refresh to an ActionBudget.
    /// Grants one additional move + action. Can only receive once per round.
    /// Returns false if already received this round.
    /// </summary>
    public static class ApplyBondRefresh
    {
        public static bool Execute(ActionBudget budget)
        {
            if (budget.HasReceivedBondRefresh) return false;
            budget.HasReceivedBondRefresh = true;
            budget.CanAct = true;
            budget.CanMove = true;
            return true;
        }
    }
}
