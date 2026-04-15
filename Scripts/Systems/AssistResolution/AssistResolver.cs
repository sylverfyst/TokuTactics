using System.Collections.Generic;
using TokuTactics.Bricks.Assist;
using TokuTactics.Commands.Assist;
using TokuTactics.Core.Grid;
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
        public AssistResolution Resolve(
            string attackerId,
            GridPosition attackerPosition,
            float comboAssistMultiplier,
            IReadOnlyDictionary<string, AssistCandidateState> rangerStates)
        {
            var resolution = new AssistResolution();

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

    // === Input Data ===

    /// <summary>
    /// State of a potential assister, provided by the combat system.
    /// This decouples the resolver from the Ranger entity — the combat system
    /// extracts what the resolver needs and passes it in.
    /// </summary>
    public class AssistCandidateState
    {
        /// <summary>Whether this Ranger is morphed (only morphed Rangers can assist).</summary>
        public bool IsMorphed { get; set; }

        /// <summary>ID of the Ranger's current form.</summary>
        public string CurrentFormId { get; set; }

        /// <summary>ID of the Ranger's base form (used for tier 2 pair attacks).</summary>
        public string BaseFormId { get; set; }

        /// <summary>Whether the Ranger is currently in their base form.</summary>
        public bool IsInBaseForm { get; set; }

        /// <summary>Base power of the Ranger's current weapon.</summary>
        public float WeaponBasePower { get; set; }

        /// <summary>Ranger's current STR stat (for damage calculation).</summary>
        public float Str { get; set; }

        /// <summary>Ranger's current CHA stat (for bond experience scaling).</summary>
        public float Cha { get; set; }

        /// <summary>Ranger's current dual type (innate + form) for assist damage matchups.</summary>
        public Core.Types.DualType AssisterDualType { get; set; }

        /// <summary>Whether this Ranger has already given a tier 4 refresh this round.</summary>
        public bool HasUsedBondRefresh { get; set; }

        /// <summary>Whether this Ranger has already received a tier 4 refresh this round.</summary>
        public bool HasReceivedBondRefresh { get; set; }
    }

    // === Output Data ===

    /// <summary>
    /// Complete resolution of all assists for a single attack action.
    /// The combat resolver iterates these and applies effects.
    /// </summary>
    public class AssistResolution
    {
        /// <summary>All individual assist effects, one per adjacent morphed Ranger.</summary>
        public List<AssistEffect> Assists { get; } = new();

        /// <summary>Whether any assists were resolved.</summary>
        public bool HasAssists => Assists.Count > 0;

        /// <summary>Whether any pair attacks triggered.</summary>
        public bool HasPairAttacks => Assists.Exists(a => a.IsPairAttack);

        /// <summary>Whether any tier 2 form disruptions occurred.</summary>
        public bool HasFormDisruptions => Assists.Exists(a => a.ForceToBaseForm);

        /// <summary>Whether any tier 4 refreshes are available.</summary>
        public bool HasRefreshOpportunities => Assists.Exists(a => a.CanRefreshPartner);

        public static AssistResolution Empty => new AssistResolution();
    }

    /// <summary>
    /// A single assist from one adjacent Ranger.
    /// Describes what the assist contributes and any side effects.
    /// </summary>
    public class AssistEffect
    {
        /// <summary>ID of the Ranger providing the assist.</summary>
        public string AssisterId { get; set; }

        /// <summary>ID of the Ranger receiving the assist (the attacker).</summary>
        public string AttackerId { get; set; }

        /// <summary>Bond tier between the two Rangers.</summary>
        public int BondTier { get; set; }

        /// <summary>Form the assister is using for this assist.</summary>
        public string AssisterFormId { get; set; }

        /// <summary>Base weapon power of the assister's current weapon.</summary>
        public float AssisterWeaponPower { get; set; }

        /// <summary>Assister's STR stat for damage calculation.</summary>
        public float AssisterStr { get; set; }

        /// <summary>Assister's CHA for bond experience scaling.</summary>
        public float ChaMultiplier { get; set; }

        /// <summary>Assister's dual type for damage matchup calculation.</summary>
        public Core.Types.DualType AssisterDualType { get; set; }

        /// <summary>
        /// Combined damage multiplier for this assist.
        /// Incorporates combo scaling (from the attacker's chain position)
        /// and bond tier bonus.
        /// </summary>
        public float DamageMultiplier { get; set; }

        /// <summary>Whether this assist is a pair attack (tier 2+).</summary>
        public bool IsPairAttack { get; set; }

        /// <summary>
        /// Whether the assister is forced to base form (tier 2 only).
        /// False at tier 3+ where pair attacks don't disrupt.
        /// </summary>
        public bool ForceToBaseForm { get; set; }

        /// <summary>
        /// The form ID being vacated due to tier 2 disruption.
        /// The combat resolver puts this form on cooldown.
        /// Null if no disruption.
        /// </summary>
        public string VacatedFormId { get; set; }

        /// <summary>
        /// Whether this assister can refresh the attacker's action (tier 4).
        /// The combat resolver checks this and calls ActionBudget.ApplyBondRefresh().
        /// </summary>
        public bool CanRefreshPartner { get; set; }
    }
}
