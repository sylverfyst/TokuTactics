using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Form;
using TokuTactics.Core.Cooldown;
using TokuTactics.Entities.Forms;

namespace TokuTactics.Commands.Form
{
    /// <summary>
    /// Command: Ticks all cooldowns in the form pool and applies health regeneration
    /// to forms still on cooldown. Composes TickCooldownWithRegen brick.
    /// </summary>
    public static class ProcessFormPoolTurn
    {
        public static void Execute(
            Dictionary<string, CooldownTimer> cooldowns,
            Dictionary<string, List<FormInstance>> formInstances,
            float regenPerTurn,
            Action<CooldownTimer, List<FormInstance>, float> tickCooldown = null)
        {
            tickCooldown ??= TickCooldownWithRegen.Execute;

            foreach (var kvp in cooldowns)
            {
                formInstances.TryGetValue(kvp.Key, out var instances);
                tickCooldown(kvp.Value, instances, regenPerTurn);
            }
        }
    }
}
