using TokuTactics.Entities.Rangers;

namespace TokuTactics.Bricks.Shared
{
    /// <summary>
    /// Returns true if the ranger is morphed and their current form's health is dead.
    /// Used to trigger demorph after status effect damage.
    /// </summary>
    public static class CheckFormDeath
    {
        public static bool Execute(Ranger ranger)
        {
            return ranger.MorphState == MorphState.Morphed
                && ranger.CurrentForm != null
                && !ranger.CurrentForm.Health.IsAlive;
        }
    }
}
