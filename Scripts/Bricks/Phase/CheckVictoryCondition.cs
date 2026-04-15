using System.Collections.Generic;
using System.Linq;
using TokuTactics.Entities.Enemies;

namespace TokuTactics.Bricks.Phase
{
    /// <summary>
    /// Returns true if all defeat target enemies are dead.
    /// </summary>
    public static class CheckVictoryCondition
    {
        public static bool Execute(IReadOnlyList<Enemy> enemies, IReadOnlyCollection<string> defeatTargetIds)
        {
            return defeatTargetIds.All(
                id => enemies.Any(e => e.Id == id && !e.IsAlive));
        }
    }
}
