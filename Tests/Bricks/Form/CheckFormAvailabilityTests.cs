using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Form;
using TokuTactics.Core.Cooldown;
using TokuTactics.Systems.FormManagement;

namespace TokuTactics.Tests.Bricks.Form
{
    public static class CheckFormAvailabilityTests
    {
        public static void Run()
        {
            Test_BaseForm_AlwaysAvailable();
            Test_EquippedNoCooldown_Available();
            Test_NotEquipped_ReturnsNotEquipped();
            Test_OnCooldown_ReturnsOnCooldown();
            Test_OccupiedByOther_ReturnsOccupied();
            Test_OccupiedBySelf_Available();
            Console.WriteLine("CheckFormAvailabilityTests: All passed");
        }

        private static void Test_BaseForm_AlwaysAvailable()
        {
            var result = CheckFormAvailability.Execute(
                "form_base", "r1", "form_base",
                new HashSet<string>(), new Dictionary<string, CooldownTimer>(),
                new Dictionary<string, string>());
            Assert(result == FormAvailability.Available, "Base form should always be available");
        }

        private static void Test_EquippedNoCooldown_Available()
        {
            var equipped = new HashSet<string> { "form_blaze" };
            var cooldowns = new Dictionary<string, CooldownTimer> { { "form_blaze", new CooldownTimer(3) } };
            var occupied = new Dictionary<string, string>();

            var result = CheckFormAvailability.Execute(
                "form_blaze", "r1", "form_base", equipped, cooldowns, occupied);
            Assert(result == FormAvailability.Available, "Equipped, off cooldown should be available");
        }

        private static void Test_NotEquipped_ReturnsNotEquipped()
        {
            var result = CheckFormAvailability.Execute(
                "form_blaze", "r1", "form_base",
                new HashSet<string>(), new Dictionary<string, CooldownTimer>(),
                new Dictionary<string, string>());
            Assert(result == FormAvailability.NotEquipped, "Unequipped form should return NotEquipped");
        }

        private static void Test_OnCooldown_ReturnsOnCooldown()
        {
            var equipped = new HashSet<string> { "form_blaze" };
            var cd = new CooldownTimer(3);
            cd.Activate();
            var cooldowns = new Dictionary<string, CooldownTimer> { { "form_blaze", cd } };

            var result = CheckFormAvailability.Execute(
                "form_blaze", "r1", "form_base", equipped, cooldowns,
                new Dictionary<string, string>());
            Assert(result == FormAvailability.OnCooldown, "Form on cooldown should return OnCooldown");
        }

        private static void Test_OccupiedByOther_ReturnsOccupied()
        {
            var equipped = new HashSet<string> { "form_blaze" };
            var cooldowns = new Dictionary<string, CooldownTimer> { { "form_blaze", new CooldownTimer(3) } };
            var occupied = new Dictionary<string, string> { { "form_blaze", "r2" } };

            var result = CheckFormAvailability.Execute(
                "form_blaze", "r1", "form_base", equipped, cooldowns, occupied);
            Assert(result == FormAvailability.OccupiedByOther, "Form occupied by other should return OccupiedByOther");
        }

        private static void Test_OccupiedBySelf_Available()
        {
            var equipped = new HashSet<string> { "form_blaze" };
            var cooldowns = new Dictionary<string, CooldownTimer> { { "form_blaze", new CooldownTimer(3) } };
            var occupied = new Dictionary<string, string> { { "form_blaze", "r1" } };

            var result = CheckFormAvailability.Execute(
                "form_blaze", "r1", "form_base", equipped, cooldowns, occupied);
            Assert(result == FormAvailability.Available, "Form occupied by self should be available");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
