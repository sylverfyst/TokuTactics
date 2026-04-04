using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Types;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Command: Resolves a complete damage roll by orchestrating combat bricks.
    ///
    /// Flow:
    /// 1. Roll dodge (defender LCK)
    /// 2. Calculate base damage (STR vs DEF + power)
    /// 3. Apply type matchup (type chart lookup)
    /// 4. Apply same-type bonus (if applicable)
    /// 5. Apply combo scaling
    /// 6. Roll crit (attacker LCK)
    ///
    /// Returns DamageResult with all roll outcomes.
    ///
    /// Command Tests:
    /// - One Intention: Resolves a complete damage roll for one attack
    /// - No State: Pure orchestration, no mutable state
    /// - Dependencies Injected: TypeChart, Random, TunableConstants passed as parameters
    /// - Testable with Mock Bricks: Each brick can be mocked to verify orchestration
    /// </summary>
    public static class ResolveDamageRoll
    {
        public static DamageResult Execute(
            ResolveDamageRollParams p,
            TypeChart typeChart,
            Random rng,
            TunableConstants constants,
            // Optional brick function injections for testing
            Func<float, float, float, float> calculateBaseDamage = null,
            Func<float, float, float, Random, bool> rollDodge = null,
            Func<float, ElementalType, ElementalType, DualType?, TypeChart, TunableConstants, ApplyTypeMatchup.Result> applyTypeMatchup = null,
            Func<float, float, float> calculateSameTypeBonus = null,
            Func<float, float, float> applyComboScaling = null,
            Func<float, float, float, Random, bool> rollCrit = null)
        {
            // Initialize injected brick functions to defaults if not provided
            calculateBaseDamage ??= CalculateBaseDamage.Execute;
            rollDodge ??= RollDodge.Execute;
            applyTypeMatchup ??= ApplyTypeMatchup.Execute;
            calculateSameTypeBonus ??= CalculateSameTypeBonus.Execute;
            applyComboScaling ??= ApplyComboScaling.Execute;
            rollCrit ??= RollCrit.Execute;

            var result = new DamageResult();

            // Brick 1: Roll dodge
            if (rollDodge(p.DefenderLck, constants.BaseDodge, constants.LckDodgeScale, rng))
            {
                result.WasDodged = true;
                result.FinalDamage = 0;
                return result;
            }

            // Brick 2: Calculate base damage
            float baseDamage = calculateBaseDamage(p.AttackerStr, p.DefenderDef, p.ActionPower);
            result.BaseDamage = baseDamage;

            // Brick 3: Apply type matchup
            // Extract primary type from attacker's dual type (form type for Rangers, single type for enemies)
            var matchup = applyTypeMatchup(
                baseDamage,
                p.AttackerDualType.FormType,
                p.DefenderType,
                p.DefenderDualType,
                typeChart,
                constants);

            result.Matchup = matchup.Matchup;
            result.TypeMultiplier = matchup.Multiplier;
            float damage = matchup.Damage;

            // Brick 4: Apply same-type bonus
            if (p.HasSameTypeBonus)
            {
                damage = calculateSameTypeBonus(damage, constants.SameTypeBonus);
                result.HadSameTypeBonus = true;
            }

            // Brick 5: Apply combo scaling
            damage = applyComboScaling(damage, p.ComboMultiplier);
            result.ComboMultiplier = p.ComboMultiplier;

            // Brick 6: Roll crit
            if (rollCrit(p.AttackerLck, constants.BaseCrit, constants.LckCritScale, rng))
            {
                damage *= constants.CritMultiplier;
                result.WasCritical = true;
            }

            // Final damage: minimum 1, rounded
            result.FinalDamage = (int)Math.Max(1, Math.Round(damage));

            return result;
        }
    }
}
