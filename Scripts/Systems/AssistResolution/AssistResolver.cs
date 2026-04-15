using System.Collections.Generic;
using TokuTactics.Bricks.Assist;
using TokuTactics.Commands.Assist;
using TokuTactics.Core.Grid;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Core.Assist;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Systems.AssistResolution
{
    /// <summary>
    /// Orchestrator: Resolves adjacency assists when a Ranger initiates combat.
    ///
    /// Finds adjacent morphed Rangers, checks eligibility, and delegates
    /// assist effect resolution to the ResolveAssistEffect command.
    /// Does NOT mutate game state — produces declarative AssistResolution.
    /// </summary>
    public class AssistResolver
    {
        private readonly BattleGrid _grid;
        private readonly BondTracker _bondTracker;

        /// <summary>Tunable: damage multiplier bonus at bond tier 1.</summary>
        public float BondTier1DamageBonus { get; set; } = 1.25f;

        /// <summary>Tunable: damage multiplier for tier 2+ pair attacks.</summary>
        public float PairAttackDamageMultiplier { get; set; } = 1.5f;

        public AssistResolver(BattleGrid grid, BondTracker bondTracker)
        {
            _grid = grid;
            _bondTracker = bondTracker;
        }

        /// <summary>
        /// Resolve all assists for an attack action.
        /// </summary>
        public Core.Assist.AssistResolution Resolve(
            string attackerId,
            GridPosition attackerPosition,
            float comboAssistMultiplier,
            IReadOnlyDictionary<string, AssistCandidateState> rangerStates)
        {
            var resolution = new Core.Assist.AssistResolution();

            rangerStates.TryGetValue(attackerId, out var attackerState);

            var adjacentUnitIds = _grid.GetAdjacentUnits(attackerPosition);

            foreach (var unitId in adjacentUnitIds)
            {
                if (!CheckAssistEligibility.Execute(unitId, attackerId, rangerStates))
                    continue;

                var candidateState = rangerStates[unitId];
                var bond = _bondTracker.GetBond(attackerId, unitId);

                var assist = ResolveAssistEffect.Execute(
                    attackerId, unitId, bond, candidateState, attackerState,
                    comboAssistMultiplier, BondTier1DamageBonus, PairAttackDamageMultiplier);

                resolution.Assists.Add(assist);
            }

            return resolution;
        }
    }
}
