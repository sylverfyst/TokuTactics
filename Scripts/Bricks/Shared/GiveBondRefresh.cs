using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Marks that a unit has given a tier 4 bond refresh this round.
    /// Can only give once per round. Returns false if already given.
    /// </summary>
    public static class GiveBondRefresh
    {
        public static bool Execute(ActionBudget budget)
        {
            if (budget.HasUsedBondRefresh) return false;
            budget.HasUsedBondRefresh = true;
            return true;
        }
    }
}
