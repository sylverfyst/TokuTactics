using TokuTactics.Entities.Enemies;

namespace TokuTactics.Bricks.Combat
{
    /// <summary>
    /// Applies damage to an enemy via Enemy.TakeDamage, which handles shields
    /// and aggression thresholds. Returns the EnemyDamageEvent.
    /// </summary>
    public static class ApplyDamageToEnemy
    {
        public static EnemyDamageEvent Execute(Enemy enemy, int damage)
        {
            return enemy.TakeDamage(damage);
        }
    }
}
