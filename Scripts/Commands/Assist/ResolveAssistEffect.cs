using System;
using TokuTactics.Bricks.Assist;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Commands.Assist
{
    /// <summary>
    /// Command: Builds a complete AssistEffect from bond state and candidate states.
    /// Composes CheckAssistEligibility (already filtered by caller),
    /// CalculateAssistDamageMultiplier, ResolveTier2Disruption, and CheckTier4RefreshEligibility bricks.
    /// </summary>
    public static class ResolveAssistEffect
    {
        public static AssistEffect Execute(
            string attackerId,
            string assisterId,
            BondState bond,
            AssistCandidateState assisterState,
            AssistCandidateState attackerState,
            float comboAssistMultiplier,
            float tier1Bonus,
            float pairAttackMultiplier,
            Func<int, float, float, float, float> calculateDamageMultiplier = null,
            Func<int, AssistCandidateState, string> resolveTier2 = null,
            Func<int, AssistCandidateState, AssistCandidateState, bool> checkTier4 = null)
        {
            calculateDamageMultiplier ??= CalculateAssistDamageMultiplier.Execute;
            resolveTier2 ??= ResolveTier2Disruption.Execute;
            checkTier4 ??= CheckTier4RefreshEligibility.Execute;

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

            // Damage multiplier
            effect.DamageMultiplier = calculateDamageMultiplier(
                bond.Tier, comboAssistMultiplier, tier1Bonus, pairAttackMultiplier);
            effect.IsPairAttack = bond.Tier >= 2;

            // Tier 2 form disruption
            var vacatedFormId = resolveTier2(bond.Tier, assisterState);
            if (vacatedFormId != null)
            {
                effect.ForceToBaseForm = true;
                effect.VacatedFormId = vacatedFormId;
                effect.AssisterFormId = assisterState.BaseFormId;
            }

            // Tier 4 refresh
            effect.CanRefreshPartner = checkTier4(bond.Tier, assisterState, attackerState);

            return effect;
        }
    }
}
