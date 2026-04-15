using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Form;
using TokuTactics.Core.Cooldown;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Forms;

namespace TokuTactics.Tests.Bricks.Form
{
    public static class TickCooldownWithRegenTests
    {
        public static void Run()
        {
            Test_TicksDown();
            Test_RegensWhileOnCooldown();
            Test_NoRegenWhenCooldownExpires();
            Test_NotOnCooldown_DoesNothing();
            Console.WriteLine("TickCooldownWithRegenTests: All passed");
        }

        private static void Test_TicksDown()
        {
            var cd = new CooldownTimer(3);
            cd.Activate();

            TickCooldownWithRegen.Execute(cd, new List<FormInstance>(), 5f);

            Assert(cd.RemainingTurns == 2, $"Expected 2 remaining, got {cd.RemainingTurns}");
        }

        private static void Test_RegensWhileOnCooldown()
        {
            var cd = new CooldownTimer(3);
            cd.Activate(); // 3 turns remaining
            var form = new FormInstance(FormCatalog.BlazeForm());
            form.Health.TakeDamage(20f);
            float healthBefore = form.Health.Current;

            TickCooldownWithRegen.Execute(cd, new List<FormInstance> { form }, 5f);

            Assert(form.Health.Current > healthBefore,
                $"Should regen while on cooldown. Before={healthBefore}, After={form.Health.Current}");
        }

        private static void Test_NoRegenWhenCooldownExpires()
        {
            var cd = new CooldownTimer(1);
            cd.Activate(); // 1 turn remaining — will expire on tick
            var form = new FormInstance(FormCatalog.BlazeForm());
            form.Health.TakeDamage(20f);
            float healthBefore = form.Health.Current;

            TickCooldownWithRegen.Execute(cd, new List<FormInstance> { form }, 5f);

            Assert(form.Health.Current == healthBefore,
                "Should NOT regen when cooldown expires on this tick");
            Assert(!cd.IsOnCooldown, "Cooldown should have expired");
        }

        private static void Test_NotOnCooldown_DoesNothing()
        {
            var cd = new CooldownTimer(3);
            // Not activated — not on cooldown
            var form = new FormInstance(FormCatalog.BlazeForm());
            float healthBefore = form.Health.Current;

            TickCooldownWithRegen.Execute(cd, new List<FormInstance> { form }, 5f);

            Assert(form.Health.Current == healthBefore, "Not on cooldown should not regen");
            Assert(cd.RemainingTurns == 0, "Should not tick if not on cooldown");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
