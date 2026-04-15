using TokuTactics.Core.Health;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Returns the correct health pool for a ranger: the current form's pool if morphed,
    /// or the unmorphed pool otherwise.
    /// </summary>
    public static class GetTargetHealthPool
    {
        public static IHealthPool Execute(Ranger ranger)
        {
            if (ranger.MorphState == MorphState.Morphed && ranger.CurrentForm != null)
                return ranger.CurrentForm.Health;

            return ranger.UnmorphedHealth;
        }
    }
}
