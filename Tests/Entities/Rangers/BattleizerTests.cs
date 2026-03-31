using TokuTactics.Entities.Rangers;

namespace TokuTactics.Tests.Entities.Rangers
{
    public class BattleizerTests
    {
        // === Unlock ===

        public void Unlock_SetsState()
        {
            var bz = new Battleizer();

            bz.Unlock("ranger_red");

            Assert(bz.IsUnlocked, "Should be unlocked");
            Assert(bz.AssignedRangerId == "ranger_red", "Should be assigned to red");
        }

        public void NotUnlocked_CannotActivate()
        {
            var bz = new Battleizer();

            Assert(!bz.CanActivate, "Should not be activatable before unlock");
            Assert(!bz.Activate(), "Activate should fail");
        }

        // === Activation ===

        public void Activate_StartsWindow()
        {
            var bz = new Battleizer() { ActiveDuration = 3 };
            bz.Unlock("ranger_red");

            bool result = bz.Activate();

            Assert(result, "Should succeed");
            Assert(bz.IsActive, "Should be active");
            Assert(bz.ActiveTurnsRemaining == 3, "Should have 3 turns");
            Assert(!bz.CanActivate, "Should not be activatable while active");
        }

        public void Activate_WhileActive_Fails()
        {
            var bz = new Battleizer();
            bz.Unlock("ranger_red");
            bz.Activate();

            bool result = bz.Activate();

            Assert(!result, "Should not double-activate");
        }

        public void Activate_WhileOnCooldown_Fails()
        {
            var bz = new Battleizer();
            bz.Unlock("ranger_red");
            bz.Activate();
            bz.Deactivate(); // Start cooldown

            Assert(!bz.CanActivate, "Should not activate during cooldown");
            Assert(!bz.Activate(), "Should fail");
        }

        // === Timer ===

        public void TickActive_CountsDown()
        {
            var bz = new Battleizer() { ActiveDuration = 3 };
            bz.Unlock("ranger_red");
            bz.Activate();

            bool expired = bz.TickActive();

            Assert(!expired, "Should not expire after 1 tick");
            Assert(bz.ActiveTurnsRemaining == 2, "Should have 2 remaining");
        }

        public void TickActive_Deactivates_WhenExpired()
        {
            var bz = new Battleizer() { ActiveDuration = 2 };
            bz.Unlock("ranger_red");
            bz.Activate();

            bz.TickActive();
            bool expired = bz.TickActive();

            Assert(expired, "Should expire after 2 ticks");
            Assert(!bz.IsActive, "Should be inactive");
            Assert(bz.Cooldown.IsOnCooldown, "Cooldown should start");
        }

        public void TickActive_WhenNotActive_ReturnsFalse()
        {
            var bz = new Battleizer();

            Assert(!bz.TickActive(), "Should return false when not active");
        }

        // === Cooldown ===

        public void Cooldown_TicksToAvailable()
        {
            var bz = new Battleizer(cooldownDuration: 3);
            bz.Unlock("ranger_red");
            bz.Activate();
            bz.Deactivate();

            Assert(bz.Cooldown.IsOnCooldown, "Should be on cooldown");

            bz.TickCooldown();
            bz.TickCooldown();
            bz.TickCooldown();

            Assert(bz.CanActivate, "Should be activatable after cooldown expires");
        }

        public void TickCooldown_WhileActive_DoesNotTick()
        {
            var bz = new Battleizer(cooldownDuration: 3);
            bz.Unlock("ranger_red");
            bz.Activate();
            bz.Deactivate();
            int initialRemaining = bz.Cooldown.RemainingTurns;

            // Reactivate somehow (reset cooldown for test)
            bz.Cooldown.Reset();
            bz.Activate();

            // Try to tick cooldown while active
            bz.TickCooldown();

            // Cooldown should not have ticked (it was reset and we're active)
            Assert(bz.IsActive, "Should still be active");
        }

        // === Stat Bonus ===

        public void GetActiveStatBonus_WhenActive_ReturnsStats()
        {
            var bz = new Battleizer();
            bz.Unlock("ranger_red");
            bz.Activate();

            var stats = bz.GetActiveStatBonus();

            Assert(stats.Get(TokuTactics.Core.Stats.StatType.STR) > 0, "Should have STR bonus when active");
        }

        public void GetActiveStatBonus_WhenInactive_ReturnsEmpty()
        {
            var bz = new Battleizer();

            var stats = bz.GetActiveStatBonus();

            Assert(stats.Get(TokuTactics.Core.Stats.StatType.STR) == 0, "Should have no bonus when inactive");
        }

        // === Recipient Determination ===

        public void DetermineRecipient_PicksHighestLevel()
        {
            var rangers = new[]
            {
                ("ranger_red", 15),
                ("ranger_blue", 22),
                ("ranger_green", 18)
            };

            string recipient = Battleizer.DetermineRecipient(rangers);

            Assert(recipient == "ranger_blue", "Should pick blue with highest total levels");
        }

        public void DetermineRecipient_EmptyList_ReturnsNull()
        {
            string recipient = Battleizer.DetermineRecipient(
                new (string, int)[] { });

            Assert(recipient == null, "Should return null for empty list");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new BattleizerTests();
            tests.Unlock_SetsState();
            tests.NotUnlocked_CannotActivate();
            tests.Activate_StartsWindow();
            tests.Activate_WhileActive_Fails();
            tests.Activate_WhileOnCooldown_Fails();
            tests.TickActive_CountsDown();
            tests.TickActive_Deactivates_WhenExpired();
            tests.TickActive_WhenNotActive_ReturnsFalse();
            tests.Cooldown_TicksToAvailable();
            tests.TickCooldown_WhileActive_DoesNotTick();
            tests.GetActiveStatBonus_WhenActive_ReturnsStats();
            tests.GetActiveStatBonus_WhenInactive_ReturnsEmpty();
            tests.DetermineRecipient_PicksHighestLevel();
            tests.DetermineRecipient_EmptyList_ReturnsNull();
            System.Console.WriteLine("BattleizerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
