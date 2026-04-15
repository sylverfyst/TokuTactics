using System.Collections.Generic;
using TokuTactics.Core.Cooldown;
using TokuTactics.Entities.Forms;

namespace TokuTactics.Bricks.Form
{
    /// <summary>
    /// Ticks a single cooldown timer. If the form is still on cooldown after ticking,
    /// applies health regeneration to all alive form instances.
    /// A form that just came off cooldown does not get a bonus regen tick.
    /// </summary>
    public static class TickCooldownWithRegen
    {
        public static void Execute(
            CooldownTimer cooldown,
            List<FormInstance> instances,
            float regenPerTurn)
        {
            if (!cooldown.IsOnCooldown) return;

            cooldown.Tick();

            // Only regenerate if still on cooldown after ticking
            if (cooldown.IsOnCooldown && instances != null)
            {
                foreach (var instance in instances)
                {
                    if (instance.Health.IsAlive)
                        instance.Health.Regenerate(regenPerTurn);
                }
            }
        }
    }
}
