using TokuTactics.Core.Combat;
using TokuTactics.Entities.Enemies;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Checks whether a combat target should fire a reactive gimmick after being hit.
    /// Returns true if: target is an enemy, alive, has a non-voluntary gimmick, and the attack landed.
    /// </summary>
    public static class ValidateReactiveGimmick
    {
        public static bool Execute(ICombatTarget target, bool wasDodged)
        {
            if (wasDodged) return false;
            if (target is not Enemy enemy) return false;
            if (!enemy.IsAlive) return false;
            if (enemy.IsGimmickVoluntary) return false;

            return true;
        }
    }
}
