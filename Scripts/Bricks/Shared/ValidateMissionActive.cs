using TokuTactics.Core.Phase;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Checks whether the mission state allows further actions.
    /// Returns true if the mission is Active.
    /// </summary>
    public static class ValidateMissionActive
    {
        public static bool Execute(MissionState state)
        {
            return state == MissionState.Active;
        }
    }
}
