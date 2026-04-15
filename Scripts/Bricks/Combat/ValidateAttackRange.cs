using TokuTactics.Core.Grid;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Brick: Validates if a target position is within weapon range.
    /// </summary>
    public static class ValidateAttackRange
    {
        /// <summary>
        /// Checks if the target is within the weapon's attack range.
        /// </summary>
        /// <param name="attackerPos">Attacker's grid position</param>
        /// <param name="targetPos">Target's grid position</param>
        /// <param name="weaponRange">Weapon's range in tiles</param>
        /// <returns>True if target is in range</returns>
        public static bool Execute(GridPosition attackerPos, GridPosition targetPos, int weaponRange)
        {
            // Calculate Manhattan distance
            int distance = attackerPos.ManhattanDistance(targetPos);

            return distance <= weaponRange;
        }
    }
}
