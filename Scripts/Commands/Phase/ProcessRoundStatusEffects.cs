using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.Health;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Command: Ticks status effects on all units and applies health changes.
    /// Composes GetTargetHealthPool, ApplyEffectOutputToHealth, and CheckFormDeath bricks.
    /// Returns declarative event data — does not publish events.
    /// </summary>
    public static class ProcessRoundStatusEffects
    {
        public static StatusEffectRoundResult Execute(
            IReadOnlyList<Ranger> rangers,
            IReadOnlyList<Enemy> enemies,
            Func<Ranger, IHealthPool> getTargetHealthPool = null,
            Action<IHealthPool, EffectOutput> applyEffectOutput = null,
            Func<Ranger, bool> checkFormDeath = null)
        {
            getTargetHealthPool ??= GetTargetHealthPool.Execute;
            applyEffectOutput ??= ApplyEffectOutputToHealth.Execute;
            checkFormDeath ??= CheckFormDeath.Execute;

            var result = new StatusEffectRoundResult();
            var context = new EffectContext { Phase = "turn_start" };

            // Process rangers
            for (int i = 0; i < rangers.Count; i++)
            {
                var ranger = rangers[i];
                if (!ranger.IsAlive) continue;

                // Phase 1: Fire per-tick effects (DoT, HoT)
                var tickOutputs = ranger.StatusEffects.Process(context);
                var healthPool = getTargetHealthPool(ranger);
                foreach (var output in tickOutputs)
                    applyEffectOutput(healthPool, output);

                // Phase 2: Tick durations and remove expired effects
                ranger.StatusEffects.TickAndClean(context);

                // Check if DoT killed the form — trigger demorph
                if (checkFormDeath(ranger))
                {
                    var lostForm = ranger.Demorph();
                    result.DemorphEvents.Add(new DemorphEventData
                    {
                        RangerId = ranger.Id,
                        LostFormId = lostForm?.Data.Id
                    });
                }
            }

            // Process enemies
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;

                // Phase 1: Fire per-tick effects
                var tickOutputs = enemy.StatusEffects.Process(context);
                foreach (var output in tickOutputs)
                {
                    if (output.Damage > 0)
                    {
                        var damageEvt = enemy.TakeDamage(output.Damage);
                        if (damageEvt.BecameAggressive)
                        {
                            result.AggressionEvents.Add(new AggressionEventData
                            {
                                EnemyId = enemy.Id,
                                HealthPercentage = enemy.Health.Percentage
                            });
                        }
                    }
                    if (output.Healing > 0)
                        enemy.Health.Heal(output.Healing);
                }

                // Phase 2: Tick durations and remove expired
                enemy.StatusEffects.TickAndClean(context);
            }

            return result;
        }
    }
}
