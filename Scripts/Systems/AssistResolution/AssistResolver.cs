using System.Collections.Generic;
using TokuTactics.Core.Grid;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Systems.AssistResolution
{
    /// <summary>
    /// Resolves adjacency assists when a Ranger initiates combat.
    /// 
    /// Finds all adjacent morphed Rangers, checks bond tiers, and produces
    /// a declarative AssistResolution describing what each assist contributes.
    /// Does NOT mutate game state — the combat resolver reads the resolution
    /// and applies damage, bond experience, form disruption, and action refreshes.
    /// 
    /// Bond tier effects:
    /// - Tier 0: Basic assist. Assist damage at base scaling.
    /// - Tier 1: Increased assist damage (BondTier1DamageBonus multiplier).
    /// - Tier 2: Pair attack auto-triggers. Assister is forced to base form —
    ///   vacated form goes on cooldown. Higher damage but costly.
    /// - Tier 3: Pair attack with current form. Replaces tier 2 — no disruption,
    ///   no cooldown. The partnership has matured.
    /// - Tier 4: Everything from tier 3, plus: can refresh partner's action
    ///   (once per round per character).
    /// 
    /// Assist damage is subject to combo scaling via the attacker's ComboScaler.
    /// Assists during chains deal higher damage than the chaining Ranger's scaled
    /// damage but are still reduced. No laundering full damage through assists.
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
        /// 
        /// attackerId: the Ranger initiating the attack.
        /// attackerPosition: grid position of the attacker.
        /// comboAssistMultiplier: from ComboScaler.AssistDamageMultiplier — scales assist damage
        ///   based on how deep in the chain the attacker is.
        /// rangerStates: lookup of Ranger states needed for resolution. The combat system
        ///   provides this so the resolver doesn't directly depend on the Ranger entity.
        /// </summary>
        public AssistResolution Resolve(
            string attackerId,
            GridPosition attackerPosition,
            float comboAssistMultiplier,
            IReadOnlyDictionary<string, AssistCandidateState> rangerStates)
        {
            var resolution = new AssistResolution();

            // Look up attacker state for tier 4 refresh validation
            rangerStates.TryGetValue(attackerId, out var attackerState);

            // Find all units adjacent to the attacker
            var adjacentUnitIds = _grid.GetAdjacentUnits(attackerPosition);

            foreach (var unitId in adjacentUnitIds)
            {
                // Only Rangers can assist — skip enemies
                if (!rangerStates.ContainsKey(unitId)) continue;

                var candidateState = rangerStates[unitId];

                // Only morphed Rangers can assist
                if (!candidateState.IsMorphed) continue;

                // Can't assist yourself
                if (unitId == attackerId) continue;

                var bond = _bondTracker.GetBond(attackerId, unitId);
                var assist = ResolveAssist(
                    attackerId, unitId, bond, candidateState,
                    attackerState, comboAssistMultiplier);

                resolution.Assists.Add(assist);
            }

            return resolution;
        }

        /// <summary>
        /// Resolve a single assist from one adjacent Ranger.
        /// </summary>
        private AssistEffect ResolveAssist(
            string attackerId,
            string assisterId,
            BondState bond,
            AssistCandidateState assisterState,
            AssistCandidateState attackerState,
            float comboAssistMultiplier)
        {
            var effect = new AssistEffect
            {
                AssisterId = assisterId,
                AttackerId = attackerId,
                BondTier = bond.Tier,
                AssisterFormId = assisterState.CurrentFormId,
                AssisterWeaponPower = assisterState.WeaponBasePower,
                AssisterStr = assisterState.Str,
                ChaMultiplier = assisterState.Cha,
                AssisterDualType = assisterState.AssisterDualType
            };

            // === Base Assist Damage ===
            float bondDamageMultiplier = 1.0f;

            if (bond.Tier >= 1)
                bondDamageMultiplier = BondTier1DamageBonus;

            // Tier 2+ pair attacks override the bond damage multiplier
            if (bond.Tier >= 2)
                bondDamageMultiplier = PairAttackDamageMultiplier;

            effect.DamageMultiplier = comboAssistMultiplier * bondDamageMultiplier;
            effect.IsPairAttack = bond.Tier >= 2;

            // === Tier 2 Form Disruption ===
            // At exactly tier 2, the pair attack forces the assister to base form.
            // The pair attack uses the BASE form for damage, not the vacated form.
            // At tier 3+, this is replaced — no disruption, uses current form.
            if (bond.Tier == 2)
            {
                if (!assisterState.IsInBaseForm)
                {
                    effect.ForceToBaseForm = true;
                    effect.VacatedFormId = assisterState.CurrentFormId;
                    // Pair attack damage uses base form, not the vacated form
                    effect.AssisterFormId = assisterState.BaseFormId;
                }
            }

            // === Tier 4 Action Refresh ===
            // The assister can refresh the attacker's action — once per round per character.
            // Assister must not have given a refresh, and must not have received one.
            // Attacker must not have already received a refresh this round.
            if (bond.Tier >= 4)
            {
                bool assisterCanGive = !assisterState.HasUsedBondRefresh
                    && !assisterState.HasReceivedBondRefresh;
                bool attackerCanReceive = attackerState == null
                    || !attackerState.HasReceivedBondRefresh;

                effect.CanRefreshPartner = assisterCanGive && attackerCanReceive;
            }

            return effect;
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
