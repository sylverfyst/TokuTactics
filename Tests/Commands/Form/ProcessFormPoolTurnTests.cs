using System;
using System.Collections.Generic;
using TokuTactics.Commands.Form;
using TokuTactics.Core.Cooldown;
using TokuTactics.Data.Content;
using TokuTactics.Entities.Forms;

namespace TokuTactics.Tests.Commands.Form
{
    public static class ProcessFormPoolTurnTests
    {
        public static void Run()
        {
            Test_TicksAllCooldowns();
            Test_RegensFormsOnCooldown();
            Test_UsesInjectedBrick();
            Console.WriteLine("ProcessFormPoolTurnTests: All passed");
        }

        private static void Test_TicksAllCooldowns()
        {
            var cd1 = new CooldownTimer(3); cd1.Activate();
            var cd2 = new CooldownTimer(2); cd2.Activate();
            var cooldowns = new Dictionary<string, CooldownTimer>
            {
                { "form_blaze", cd1 },
                { "form_torrent", cd2 }
            };
            var instances = new Dictionary<string, List<FormInstance>>();

            ProcessFormPoolTurn.Execute(cooldowns, instances, 5f);

            Assert(cd1.RemainingTurns == 2, $"form_blaze should be at 2, got {cd1.RemainingTurns}");
            Assert(cd2.RemainingTurns == 1, $"form_torrent should be at 1, got {cd2.RemainingTurns}");
        }

        private static void Test_RegensFormsOnCooldown()
        {
            var cd = new CooldownTimer(3); cd.Activate();
            var cooldowns = new Dictionary<string, CooldownTimer> { { "form_blaze", cd } };
            var form = new FormInstance(FormCatalog.BlazeForm());
            form.Health.TakeDamage(20f);
            float healthBefore = form.Health.Current;
            var instances = new Dictionary<string, List<FormInstance>>
            {
                { "form_blaze", new List<FormInstance> { form } }
            };

            ProcessFormPoolTurn.Execute(cooldowns, instances, 5f);

            Assert(form.Health.Current > healthBefore, "Should regen form on cooldown");
        }

        private static void Test_UsesInjectedBrick()
        {
            var cd = new CooldownTimer(3); cd.Activate();
            var cooldowns = new Dictionary<string, CooldownTimer> { { "form_blaze", cd } };
            var instances = new Dictionary<string, List<FormInstance>>();
            int callCount = 0;

            ProcessFormPoolTurn.Execute(cooldowns, instances, 5f,
                tickCooldown: (c, i, r) => callCount++);

            Assert(callCount == 1, $"Should call brick once per cooldown, got {callCount}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
