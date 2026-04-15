using System;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Command: Processes a single assist attack against a target.
    /// Composes ResolveDamageRoll (existing command) and ApplyDamageToEnemy/ApplyDamageToRanger bricks.
    /// Awards bond experience via BondTracker (injected).
    /// Returns declarative result — orchestrator publishes events.
    /// </summary>
    public static class ProcessAssist
    {
        public static ProcessAssistResult Execute(
            AssistEffect assist,
            ICombatTarget target,
            TypeChart typeChart,
            Random rng,
            TunableConstants constants,
            BondTracker bondTracker,
            Func<Enemy, int, EnemyDamageEvent> applyDamageToEnemy = null,
            Action<Ranger, int> applyDamageToRanger = null)
        {
            applyDamageToEnemy ??= ApplyDamageToEnemy.Execute;
            applyDamageToRanger ??= ApplyDamageToRanger.Execute;
            var assistCombatResult = new AssistCombatResult
            {
                AssisterId = assist.AssisterId,
                BondTier = assist.BondTier,
                IsPairAttack = assist.IsPairAttack
            };

            var processResult = new ProcessAssistResult
            {
                AssistCombatResult = assistCombatResult
            };

            // Calculate assist damage
            var damageParams = new ResolveDamageRollParams
            {
                AttackerStr = assist.AssisterStr,
                AttackerLck = 0, // Assists don't crit independently
                DefenderDef = target.Stats.Get(Core.Stats.StatType.DEF),
                DefenderLck = target.Stats.Get(Core.Stats.StatType.LCK),
                ActionPower = assist.AssisterWeaponPower,
                AttackType = assist.AssisterDualType.FormType,
                DefenderType = target.Type,
                DefenderDualType = (target is Ranger rangerTarget) ? rangerTarget.DualType : null,
                ComboMultiplier = assist.DamageMultiplier,
                HasSameTypeBonus = assist.AssisterDualType.IsSameType
            };

            var damageResult = ResolveDamageRoll.Execute(damageParams, typeChart, rng, constants);
            assistCombatResult.Damage = damageResult;

            // Apply assist damage
            if (!damageResult.WasDodged)
            {
                if (target is Enemy enemy)
                {
                    var evt = applyDamageToEnemy(enemy, damageResult.FinalDamage);
                    if (evt.BecameAggressive)
                    {
                        processResult.AggressionTriggered = true;
                        processResult.AggressionEnemyId = enemy.Id;
                        processResult.AggressionHealthPercentage = enemy.Health.Percentage;
                    }
                }
                else if (target is Ranger ranger)
                {
                    applyDamageToRanger(ranger, damageResult.FinalDamage);
                }
            }

            // Award bond experience
            var tierChange = bondTracker.AddAssistExperience(
                assist.AttackerId, assist.AssisterId, assist.ChaMultiplier);

            if (tierChange != null)
            {
                assistCombatResult.BondTierChange = tierChange;
                processResult.BondTierChange = tierChange;
            }

            // Tier 2 form disruption
            if (assist.ForceToBaseForm)
            {
                assistCombatResult.FormDisrupted = true;
                assistCombatResult.VacatedFormId = assist.VacatedFormId;
            }

            // Tier 4 refresh opportunity
            if (assist.CanRefreshPartner)
            {
                assistCombatResult.RefreshAvailable = true;
            }

            return processResult;
        }
    }
}
