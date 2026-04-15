using System.Collections.Generic;
using TokuTactics.Commands.Gimmick;
using TokuTactics.Core.Grid;
using TokuTactics.Entities.Enemies.Gimmicks;

namespace TokuTactics.Systems.GimmickResolution
{
    /// <summary>
    /// Orchestrator: Translates a declarative GimmickOutput into a concrete GimmickResolution.
    /// Delegates spatial resolution to the ResolveGimmickEffects command.
    /// Does NOT mutate game state — produces a resolution for the combat resolver to apply.
    /// </summary>
    public class GimmickResolver
    {
        private readonly BattleGrid _grid;

        public GimmickResolver(BattleGrid grid)
        {
            _grid = grid;
        }

        /// <summary>
        /// Resolve a gimmick output into concrete effects.
        /// </summary>
        public Commands.Gimmick.GimmickResolution Resolve(
            GridPosition ownerPosition,
            GimmickOutput output,
            int behaviorRange,
            HashSet<string> targetUnitIds)
        {
            return ResolveGimmickEffects.Execute(
                _grid, ownerPosition, output, behaviorRange, targetUnitIds);
        }
    }
}
