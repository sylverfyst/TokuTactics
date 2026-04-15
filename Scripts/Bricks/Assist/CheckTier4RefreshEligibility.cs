using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Bricks.Assist
{
    /// <summary>
    /// Checks if a tier 4 bond refresh is available.
    /// Assister must not have given or received a refresh this round.
    /// Attacker must not have already received a refresh this round.
    /// </summary>
    public static class CheckTier4RefreshEligibility
    {
        public static bool Execute(
            int bondTier,
            AssistCandidateState assisterState,
            AssistCandidateState attackerState)
        {
            if (bondTier < 4) return false;

            bool assisterCanGive = !assisterState.HasUsedBondRefresh
                && !assisterState.HasReceivedBondRefresh;

            bool attackerCanReceive = attackerState == null
                || !attackerState.HasReceivedBondRefresh;

            return assisterCanGive && attackerCanReceive;
        }
    }
}
