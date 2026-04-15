using System.Collections.Generic;
using System.Linq;
using TokuTactics.Entities.Enemies;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Command: Validates mission inputs and resolves the defeat target set.
    /// If no defeat targets are specified, defaults to all enemy IDs.
    /// </summary>
    public static class InitializeMission
    {
        public static InitializeMissionResult Execute(
            IReadOnlyList<Enemy> enemies,
            HashSet<string> defeatTargetIds = null)
        {
            var resolvedTargets = defeatTargetIds ?? new HashSet<string>(
                enemies.Select(e => e.Id));

            return new InitializeMissionResult
            {
                DefeatTargetIds = resolvedTargets
            };
        }
    }
}
