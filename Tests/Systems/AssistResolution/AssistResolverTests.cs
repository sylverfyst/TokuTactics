using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Grid;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Tests.Systems.AssistResolution
{
    public class AssistResolverTests
    {
        // === Helpers ===

        private BattleGrid MakeGrid(int size = 10)
        {
            return new BattleGrid(size, size);
        }

        private BondTracker MakeBondTracker()
        {
            return new BondTracker();
        }

        private AssistCandidateState MakeMorphedRanger(
            string formId = "form_blaze", string baseFormId = "form_base",
            float weaponPower = 10f, float str = 12f, float cha = 8f,
            bool isBaseForm = false,
            bool usedRefresh = false, bool receivedRefresh = false)
        {
            return new AssistCandidateState
            {
                IsMorphed = true,
                CurrentFormId = formId,
                BaseFormId = baseFormId,
                IsInBaseForm = isBaseForm,
                WeaponBasePower = weaponPower,
                Str = str,
                Cha = cha,
                HasUsedBondRefresh = usedRefresh,
                HasReceivedBondRefresh = receivedRefresh
            };
        }

        private AssistCandidateState MakeUnmorphedRanger()
        {
            return new AssistCandidateState { IsMorphed = false };
        }

        private void SetBondTier(BondTracker tracker, string a, string b, int tier)
        {
            // Pump experience to reach the desired tier
            var bond = tracker.GetBond(a, b);
            if (tier >= 1) bond.AddExperience(50, tracker.TierThresholds);
            if (tier >= 2) bond.AddExperience(100, tracker.TierThresholds);
            if (tier >= 3) bond.AddExperience(200, tracker.TierThresholds);
            if (tier >= 4) bond.AddExperience(350, tracker.TierThresholds);
        }

        // === No Assists ===

        public void Resolve_NoAdjacentRangers_Empty()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            // No one adjacent

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f,
                new Dictionary<string, AssistCandidateState>());

            Assert(!result.HasAssists, "No adjacent Rangers = no assists");
        }

        public void Resolve_AdjacentUnmorphed_NoAssist()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeUnmorphedRanger()
            };

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(!result.HasAssists, "Unmorphed Ranger cannot assist");
        }

        public void Resolve_SelfNotIncluded()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["attacker"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(!result.HasAssists, "Attacker should not assist themselves");
        }

        public void Resolve_EnemyAdjacentIgnored()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("enemy_1", new GridPosition(5, 4)); // Adjacent but not in ranger states

            var states = new Dictionary<string, AssistCandidateState>();
            // enemy_1 is NOT in rangerStates

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(!result.HasAssists, "Enemies should not assist");
        }

        public void Resolve_NotAdjacent_NoAssist()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(3, 3)); // Not adjacent

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(!result.HasAssists, "Non-adjacent Ranger cannot assist");
        }

        // === Tier 0 (Basic Assist) ===

        public void Resolve_Tier0_BasicAssist()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger(weaponPower: 15f, str: 10f)
            };

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(result.HasAssists, "Should have assist");
            Assert(result.Assists.Count == 1, "One assist");

            var assist = result.Assists[0];
            Assert(assist.AssisterId == "ally", "Correct assister");
            Assert(assist.BondTier == 0, "Should be tier 0");
            Assert(!assist.IsPairAttack, "No pair attack at tier 0");
            Assert(!assist.ForceToBaseForm, "No disruption at tier 0");
            Assert(!assist.CanRefreshPartner, "No refresh at tier 0");
            Assert(assist.DamageMultiplier == 1.0f, "Full combo * no bond bonus = 1.0");
        }

        // === Tier 1 (Damage Bonus) ===

        public void Resolve_Tier1_IncreasedDamage()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 1);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(assist.BondTier == 1, "Should be tier 1");
            Assert(assist.DamageMultiplier == 1.25f, "Tier 1 bonus = 1.25");
            Assert(!assist.IsPairAttack, "No pair attack at tier 1");
        }

        // === Tier 2 (Pair Attack + Disruption) ===

        public void Resolve_Tier2_PairAttackWithDisruption()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 2);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger(formId: "form_shadow")
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(assist.BondTier == 2, "Should be tier 2");
            Assert(assist.IsPairAttack, "Tier 2 triggers pair attack");
            Assert(assist.ForceToBaseForm, "Tier 2 forces base form");
            Assert(assist.VacatedFormId == "form_shadow", "Should vacate current form");
            Assert(assist.DamageMultiplier == 1.5f, "Pair attack multiplier");
            Assert(result.HasFormDisruptions, "Should have disruptions");
        }

        public void Resolve_Tier2_AlreadyBaseForm_NoDisruption()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 2);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger(formId: "form_base", isBaseForm: true)
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(assist.IsPairAttack, "Still a pair attack");
            Assert(!assist.ForceToBaseForm, "Already in base form — no disruption");
            Assert(assist.VacatedFormId == null, "No form to vacate");
            Assert(!result.HasFormDisruptions, "No disruptions");
        }

        public void Resolve_Tier2_UsesBaseFormForDamage()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 2);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger(
                    formId: "form_shadow", baseFormId: "form_base_ally")
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(assist.ForceToBaseForm, "Should disrupt");
            Assert(assist.VacatedFormId == "form_shadow", "Vacated form is the current form");
            Assert(assist.AssisterFormId == "form_base_ally",
                "Pair attack damage should use base form, not vacated form");
        }

        // === Demorphed ===

        public void Resolve_DemorphedRanger_NoAssist()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("demorphed", new GridPosition(5, 4));

            // Demorphed = not morphed, different from unmorphed but same assist eligibility
            var states = new Dictionary<string, AssistCandidateState>
            {
                ["demorphed"] = new AssistCandidateState { IsMorphed = false }
            };

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(!result.HasAssists, "Demorphed Ranger cannot assist");
        }

        // === Tier 3 (Pair Attack, No Disruption) ===

        public void Resolve_Tier3_PairAttackNoDisruption()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 3);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger(formId: "form_shadow")
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(assist.BondTier == 3, "Should be tier 3");
            Assert(assist.IsPairAttack, "Tier 3 is a pair attack");
            Assert(!assist.ForceToBaseForm, "Tier 3 does NOT disrupt");
            Assert(assist.VacatedFormId == null, "No form vacated");
            Assert(assist.DamageMultiplier == 1.5f, "Pair attack multiplier");
            Assert(assist.AssisterFormId == "form_shadow", "Uses current form, not base");
        }

        // === Tier 4 (Pair Attack + Refresh) ===

        public void Resolve_Tier4_PairAttackWithRefresh()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 4);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["attacker"] = MakeMorphedRanger(),
                ["ally"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(assist.BondTier == 4, "Should be tier 4");
            Assert(assist.IsPairAttack, "Tier 4 is a pair attack");
            Assert(!assist.ForceToBaseForm, "Tier 4 does not disrupt (tier 3 replaces)");
            Assert(assist.CanRefreshPartner, "Tier 4 can refresh");
            Assert(result.HasRefreshOpportunities, "Should have refresh opportunities");
        }

        public void Resolve_Tier4_AlreadyUsedRefresh_NoRefresh()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 4);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["attacker"] = MakeMorphedRanger(),
                ["ally"] = MakeMorphedRanger(usedRefresh: true)
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(!assist.CanRefreshPartner, "Already used refresh this round");
        }

        public void Resolve_Tier4_AssisterReceivedRefresh_NoRefresh()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 4);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["attacker"] = MakeMorphedRanger(),
                ["ally"] = MakeMorphedRanger(receivedRefresh: true)
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(!assist.CanRefreshPartner,
                "Assister already received a refresh — can't give one too");
        }

        public void Resolve_Tier4_AttackerAlreadyReceivedRefresh_NoRefresh()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 4);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["attacker"] = MakeMorphedRanger(receivedRefresh: true),
                ["ally"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            var assist = result.Assists[0];
            Assert(!assist.CanRefreshPartner,
                "Attacker already received a refresh this round — nobody can give another");
        }

        // === Combo Scaling ===

        public void Resolve_ComboScaling_ReducesAssistDamage()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, MakeBondTracker());
            // Combo assist multiplier of 0.8 = attacker is mid-chain
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 0.8f, states);

            var assist = result.Assists[0];
            // Tier 0: no bond bonus, so damage = 0.8 * 1.0 = 0.8
            Assert(assist.DamageMultiplier == 0.8f, "Combo scaling should reduce assist damage");
        }

        public void Resolve_ComboScaling_WithBondBonus()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally", new GridPosition(5, 4));

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally", 1);

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 0.8f, states);

            var assist = result.Assists[0];
            // Tier 1: 0.8 combo * 1.25 bond = 1.0
            Assert(assist.DamageMultiplier == 1.0f,
                "Combo 0.8 * bond 1.25 = 1.0");
        }

        // === Multiple Assists ===

        public void Resolve_MultipleAdjacentRangers()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("ally_a", new GridPosition(5, 4)); // North
            grid.PlaceUnit("ally_b", new GridPosition(4, 5)); // West
            grid.PlaceUnit("ally_c", new GridPosition(6, 5)); // East

            var tracker = MakeBondTracker();
            SetBondTier(tracker, "attacker", "ally_a", 1);
            SetBondTier(tracker, "attacker", "ally_b", 3);
            // ally_c stays at tier 0

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["ally_a"] = MakeMorphedRanger(),
                ["ally_b"] = MakeMorphedRanger(),
                ["ally_c"] = MakeMorphedRanger()
            };

            var resolver = new AssistResolver(grid, tracker);
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(result.Assists.Count == 3, "Three adjacent morphed Rangers = 3 assists");

            var assistA = result.Assists.First(a => a.AssisterId == "ally_a");
            var assistB = result.Assists.First(a => a.AssisterId == "ally_b");
            var assistC = result.Assists.First(a => a.AssisterId == "ally_c");

            Assert(assistA.BondTier == 1, "ally_a at tier 1");
            Assert(assistB.BondTier == 3 && assistB.IsPairAttack, "ally_b at tier 3, pair attack");
            Assert(assistC.BondTier == 0 && !assistC.IsPairAttack, "ally_c at tier 0, basic");
        }

        public void Resolve_MixedMorphStates()
        {
            var grid = MakeGrid();
            grid.PlaceUnit("attacker", new GridPosition(5, 5));
            grid.PlaceUnit("morphed", new GridPosition(5, 4));
            grid.PlaceUnit("unmorphed", new GridPosition(4, 5));

            var states = new Dictionary<string, AssistCandidateState>
            {
                ["morphed"] = MakeMorphedRanger(),
                ["unmorphed"] = MakeUnmorphedRanger()
            };

            var resolver = new AssistResolver(grid, MakeBondTracker());
            var result = resolver.Resolve("attacker", new GridPosition(5, 5), 1.0f, states);

            Assert(result.Assists.Count == 1, "Only morphed Ranger assists");
            Assert(result.Assists[0].AssisterId == "morphed", "Correct assister");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new AssistResolverTests();

            // No assists
            t.Resolve_NoAdjacentRangers_Empty();
            t.Resolve_AdjacentUnmorphed_NoAssist();
            t.Resolve_SelfNotIncluded();
            t.Resolve_EnemyAdjacentIgnored();
            t.Resolve_NotAdjacent_NoAssist();
            t.Resolve_DemorphedRanger_NoAssist();

            // Tier 0
            t.Resolve_Tier0_BasicAssist();

            // Tier 1
            t.Resolve_Tier1_IncreasedDamage();

            // Tier 2
            t.Resolve_Tier2_PairAttackWithDisruption();
            t.Resolve_Tier2_AlreadyBaseForm_NoDisruption();
            t.Resolve_Tier2_UsesBaseFormForDamage();

            // Tier 3
            t.Resolve_Tier3_PairAttackNoDisruption();

            // Tier 4
            t.Resolve_Tier4_PairAttackWithRefresh();
            t.Resolve_Tier4_AlreadyUsedRefresh_NoRefresh();
            t.Resolve_Tier4_AssisterReceivedRefresh_NoRefresh();
            t.Resolve_Tier4_AttackerAlreadyReceivedRefresh_NoRefresh();

            // Combo scaling
            t.Resolve_ComboScaling_ReducesAssistDamage();
            t.Resolve_ComboScaling_WithBondBonus();

            // Multiple assists
            t.Resolve_MultipleAdjacentRangers();
            t.Resolve_MixedMorphStates();

            System.Console.WriteLine("AssistResolverTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
