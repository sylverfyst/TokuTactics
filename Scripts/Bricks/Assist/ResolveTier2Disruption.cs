using TokuTactics.Core.Assist;

namespace TokuTactics.Bricks.Assist
{
    /// <summary>
    /// Determines if a tier 2 pair attack forces the assister to base form.
    /// Only applies at exactly tier 2 (tier 3+ has no disruption).
    /// Returns the vacated form ID, or null if no disruption.
    /// </summary>
    public static class ResolveTier2Disruption
    {
        public static string Execute(int bondTier, AssistCandidateState assisterState)
        {
            if (bondTier != 2) return null;
            if (assisterState.IsInBaseForm) return null;

            return assisterState.CurrentFormId;
        }
    }
}
