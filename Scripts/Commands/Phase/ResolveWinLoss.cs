using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Phase;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Command: Checks both win and loss conditions and returns the result.
    /// Composes CheckRangerDefeat and CheckVictoryCondition bricks.
    /// Called after combat resolution and after status effect ticks.
    /// </summary>
    public static class ResolveWinLoss
    {
        public static WinLossResult Execute(
            IReadOnlyList<Ranger> rangers,
            IReadOnlyList<Enemy> enemies,
            IReadOnlyCollection<string> defeatTargetIds,
            Func<IReadOnlyList<Ranger>, string> checkRangerDefeat = null,
            Func<IReadOnlyList<Enemy>, IReadOnlyCollection<string>, bool> checkVictoryCondition = null)
        {
            checkRangerDefeat ??= CheckRangerDefeat.Execute;
            checkVictoryCondition ??= CheckVictoryCondition.Execute;

            // Loss check first — a dead ranger is an immediate loss
            var fallenId = checkRangerDefeat(rangers);
            if (fallenId != null)
                return WinLossResult.Defeat(fallenId);

            // Win check — all defeat targets dead
            if (checkVictoryCondition(enemies, defeatTargetIds))
                return WinLossResult.Victory();

            return WinLossResult.NoEnd();
        }
    }
}
