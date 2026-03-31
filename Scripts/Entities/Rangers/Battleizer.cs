using TokuTactics.Core.Stats;
using TokuTactics.Core.Cooldown;

namespace TokuTactics.Entities.Rangers
{
    /// <summary>
    /// The Battleizer — a once-per-fight (typically) ultimate power-up.
    /// 
    /// Rules from the GDD:
    /// - Awarded to the highest-level Ranger at unlock time
    /// - Can only be activated from base form
    /// - Activation is an attack option in base form — instant execution
    /// - Timed power window — massive stats for limited turns
    /// - Very long cooldown — typically once per fight, possibly twice in endgame
    /// - On original difficulty, a single Battleizer hit kills the final boss's heart
    /// - Combo scaling: the Battleizer Ranger always hits at full unscaled damage
    ///   while in Battleizer mode (they haven't been chaining forms)
    /// </summary>
    public class Battleizer
    {
        /// <summary>Whether the Battleizer has been unlocked in this playthrough.</summary>
        public bool IsUnlocked { get; private set; }

        /// <summary>The Ranger ID who received the Battleizer.</summary>
        public string AssignedRangerId { get; private set; }

        /// <summary>Whether the Battleizer is currently active (power window open).</summary>
        public bool IsActive { get; private set; }

        /// <summary>Remaining turns in the active power window.</summary>
        public int ActiveTurnsRemaining { get; private set; }

        /// <summary>Cooldown tracker. Very long cooldown — once per fight typically.</summary>
        public CooldownTimer Cooldown { get; }

        /// <summary>Whether the Battleizer can be activated right now.</summary>
        public bool CanActivate => IsUnlocked && !IsActive && Cooldown.IsAvailable;

        // === Tunable Stats ===

        /// <summary>Tunable: how many turns the Battleizer power window lasts.</summary>
        public int ActiveDuration { get; set; } = 3;

        /// <summary>Tunable: cooldown duration after the power window ends.</summary>
        public int CooldownDuration { get; set; } = 20;

        /// <summary>Tunable: stat bonuses applied during the active window.</summary>
        public StatBlock BattleizerStats { get; set; } = StatBlock.Create(
            str: 30, def: 20, spd: 10, mag: 10, cha: 5, lck: 10);

        /// <summary>Tunable: damage power multiplier during Battleizer attacks.</summary>
        public float DamagePowerMultiplier { get; set; } = 3.0f;

        public Battleizer(int cooldownDuration = 20)
        {
            Cooldown = new CooldownTimer(cooldownDuration);
            CooldownDuration = cooldownDuration;
        }

        /// <summary>
        /// Unlock the Battleizer and assign it to a Ranger.
        /// Called at the story unlock point. The combat system determines
        /// the highest-level Ranger and passes their ID here.
        /// </summary>
        public void Unlock(string rangerId)
        {
            IsUnlocked = true;
            AssignedRangerId = rangerId;
        }

        /// <summary>
        /// Activate the Battleizer. The assigned Ranger must be in base form.
        /// The caller (combat system) is responsible for verifying the Ranger
        /// is in base form before calling this.
        /// Returns false if the Battleizer can't be activated.
        /// </summary>
        public bool Activate()
        {
            if (!CanActivate)
                return false;

            IsActive = true;
            ActiveTurnsRemaining = ActiveDuration;
            return true;
        }

        /// <summary>
        /// Deactivate the Battleizer (window expired).
        /// Starts the cooldown.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            ActiveTurnsRemaining = 0;
            Cooldown.Activate();
        }

        /// <summary>
        /// Tick the active window. If it expires, deactivate.
        /// Call once per turn while active.
        /// Returns true if the window just expired.
        /// </summary>
        public bool TickActive()
        {
            if (!IsActive) return false;

            ActiveTurnsRemaining--;
            if (ActiveTurnsRemaining <= 0)
            {
                Deactivate();
                return true; // window expired
            }
            return false;
        }

        /// <summary>
        /// Tick the cooldown (when not active).
        /// </summary>
        public void TickCooldown()
        {
            if (!IsActive)
            {
                Cooldown.Tick();
            }
        }

        /// <summary>
        /// Get the stat bonus to add during the active window.
        /// Returns an empty StatBlock if not active.
        /// </summary>
        public StatBlock GetActiveStatBonus()
        {
            return IsActive ? BattleizerStats : new StatBlock();
        }

        /// <summary>
        /// Determine which Ranger should receive the Battleizer.
        /// Finds the Ranger with the highest total form levels.
        /// Called by the combat/progression system at unlock time.
        /// </summary>
        public static string DetermineRecipient(
            System.Collections.Generic.IEnumerable<(string RangerId, int TotalFormLevels)> rangerLevels)
        {
            string bestId = null;
            int bestTotal = -1;

            foreach (var (rangerId, totalLevels) in rangerLevels)
            {
                if (totalLevels > bestTotal)
                {
                    bestTotal = totalLevels;
                    bestId = rangerId;
                }
            }

            return bestId;
        }
    }
}
