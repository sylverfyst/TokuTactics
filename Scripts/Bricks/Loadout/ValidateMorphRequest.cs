using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.LoadoutSelection;

namespace TokuTactics.Bricks.Loadout
{
    /// <summary>
    /// Validates if a ranger can morph. Must be unmorphed and alive.
    /// Returns Invalid if preconditions fail, or the appropriate MorphRequestResult.
    /// </summary>
    public static class ValidateMorphRequest
    {
        public static MorphRequestResult Execute(Ranger ranger, bool isLoadoutLocked)
        {
            if (ranger.MorphState != MorphState.Unmorphed)
                return MorphRequestResult.Invalid;

            if (!ranger.IsAlive)
                return MorphRequestResult.Invalid;

            if (!isLoadoutLocked)
                return MorphRequestResult.NeedsLoadout;

            return MorphRequestResult.MorphComplete;
        }
    }
}
