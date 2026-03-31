using TokuTactics.Core.Cooldown;

namespace TokuTactics.Tests.Core.Cooldown
{
    public class CooldownTimerTests
    {
        public void Constructor_StartsAvailable()
        {
            var cd = new CooldownTimer(3);

            Assert(cd.IsAvailable, "Should start available");
            Assert(!cd.IsOnCooldown, "Should not be on cooldown");
            Assert(cd.RemainingTurns == 0, "Should have 0 remaining");
            Assert(cd.BaseDuration == 3, "Base duration should be set");
        }

        public void Activate_StartsFullCooldown()
        {
            var cd = new CooldownTimer(3);

            cd.Activate();

            Assert(cd.IsOnCooldown, "Should be on cooldown");
            Assert(!cd.IsAvailable, "Should not be available");
            Assert(cd.RemainingTurns == 3, "Should have full duration remaining");
        }

        public void Activate_WithMagModifier_ReducesDuration()
        {
            var cd = new CooldownTimer(5);

            cd.Activate(durationModifier: 2);

            Assert(cd.RemainingTurns == 3, "Should be reduced by MAG modifier");
        }

        public void Activate_MagModifier_MinimumOneTurn()
        {
            var cd = new CooldownTimer(3);

            cd.Activate(durationModifier: 10); // More than base duration

            Assert(cd.RemainingTurns == 1, "Should not go below 1 turn");
        }

        public void Tick_ReducesByOne()
        {
            var cd = new CooldownTimer(3);
            cd.Activate();

            cd.Tick();

            Assert(cd.RemainingTurns == 2, "Should have 2 remaining after tick");
            Assert(cd.IsOnCooldown, "Should still be on cooldown");
        }

        public void Tick_ToZero_BecomesAvailable()
        {
            var cd = new CooldownTimer(2);
            cd.Activate();

            cd.Tick();
            cd.Tick();

            Assert(cd.RemainingTurns == 0, "Should be 0");
            Assert(cd.IsAvailable, "Should be available");
            Assert(!cd.IsOnCooldown, "Should not be on cooldown");
        }

        public void Tick_WhenAvailable_DoesNothing()
        {
            var cd = new CooldownTimer(3);

            cd.Tick(); // Not activated

            Assert(cd.RemainingTurns == 0, "Should stay at 0");
            Assert(cd.IsAvailable, "Should remain available");
        }

        public void Reset_ForcesAvailable()
        {
            var cd = new CooldownTimer(5);
            cd.Activate();

            cd.Reset();

            Assert(cd.IsAvailable, "Should be available after reset");
            Assert(cd.RemainingTurns == 0, "Should have 0 remaining");
        }

        public void Activate_AfterExpiry_StartsNewCooldown()
        {
            var cd = new CooldownTimer(2);
            cd.Activate();
            cd.Tick();
            cd.Tick();
            Assert(cd.IsAvailable, "Should be available after full tick-down");

            cd.Activate();

            Assert(cd.IsOnCooldown, "Should be on cooldown again");
            Assert(cd.RemainingTurns == 2, "Should have full duration");
        }

        public void FullLifecycle_ActivateTickReactivate()
        {
            var cd = new CooldownTimer(3);

            // Activate
            cd.Activate();
            Assert(cd.RemainingTurns == 3, "Starts at 3");

            // Tick down
            cd.Tick();
            Assert(cd.RemainingTurns == 2, "2 remaining");
            cd.Tick();
            Assert(cd.RemainingTurns == 1, "1 remaining");
            cd.Tick();
            Assert(cd.RemainingTurns == 0, "0 remaining");
            Assert(cd.IsAvailable, "Available after full ticks");

            // Reactivate with MAG modifier
            cd.Activate(durationModifier: 1);
            Assert(cd.RemainingTurns == 2, "Reduced by MAG");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new CooldownTimerTests();
            tests.Constructor_StartsAvailable();
            tests.Activate_StartsFullCooldown();
            tests.Activate_WithMagModifier_ReducesDuration();
            tests.Activate_MagModifier_MinimumOneTurn();
            tests.Tick_ReducesByOne();
            tests.Tick_ToZero_BecomesAvailable();
            tests.Tick_WhenAvailable_DoesNothing();
            tests.Reset_ForcesAvailable();
            tests.Activate_AfterExpiry_StartsNewCooldown();
            tests.FullLifecycle_ActivateTickReactivate();
            System.Console.WriteLine("CooldownTimerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
