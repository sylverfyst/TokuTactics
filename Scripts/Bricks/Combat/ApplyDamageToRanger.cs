using TokuTactics.Entities.Rangers;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Applies damage to a ranger's correct health pool: the current form's pool
    /// if morphed, or the unmorphed pool otherwise.
    /// </summary>
    public static class ApplyDamageToRanger
    {
        public static void Execute(Ranger ranger, int damage)
        {
            if (ranger.MorphState == MorphState.Morphed && ranger.CurrentForm != null)
                ranger.CurrentForm.Health.TakeDamage(damage);
            else
                ranger.UnmorphedHealth.TakeDamage(damage);
        }
    }
}
