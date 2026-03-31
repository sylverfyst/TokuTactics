using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.Cooldown;

namespace TokuTactics.Entities.Zords
{
    /// <summary>
    /// The combined Megazord. Stats are additive from component zords.
    /// Type is dual — derived from the two highest-level component zords.
    /// Combination is timed with a cooldown. Zords can be hot-swapped during combination.
    /// The ultimate attack ends the combination immediately.
    /// 
    /// Uses the same damage system as ground combat.
    /// </summary>
    public class Megazord : IStatProvider
    {
        // === Component Zords ===

        /// <summary>Currently slotted zords. Can be swapped during combination.</summary>
        private readonly List<ZordInstance> _componentZords = new();

        /// <summary>All available zords that can be swapped in.</summary>
        private readonly List<ZordInstance> _availableZords = new();

        /// <summary>Read-only access to current components.</summary>
        public IReadOnlyList<ZordInstance> ComponentZords => _componentZords;

        // === Combination State ===

        /// <summary>Whether the Megazord is currently combined.</summary>
        public bool IsCombined { get; private set; }

        /// <summary>Remaining turns of combination.</summary>
        public int CombinationTimer { get; private set; }

        /// <summary>Tunable: base combination duration in turns.</summary>
        public int BaseCombinationDuration { get; set; } = 5;

        /// <summary>Cooldown tracker for combination.</summary>
        public CooldownTimer CombinationCooldown { get; }

        /// <summary>Tunable: base cooldown duration after combination ends.</summary>
        public int BaseCombinationCooldown { get; set; } = 8;

        // === Hidden Multipliers ===

        /// <summary>
        /// Lookup table for hidden zord combination multipliers on the ultimate attack.
        /// Key = sorted set of zord IDs. Value = damage multiplier.
        /// Community discovery only — never surfaced to the player.
        /// </summary>
        private readonly Dictionary<string, float> _hiddenMultipliers = new();

        public Megazord(int combinationCooldownDuration = 8)
        {
            CombinationCooldown = new CooldownTimer(combinationCooldownDuration);
        }

        // === Setup ===

        /// <summary>
        /// Register all available zords for this team.
        /// </summary>
        public void SetAvailableZords(IEnumerable<ZordInstance> zords)
        {
            _availableZords.Clear();
            _availableZords.AddRange(zords);
        }

        /// <summary>
        /// Register a hidden multiplier for a specific zord combination.
        /// </summary>
        public void RegisterHiddenMultiplier(IEnumerable<string> zordIds, float multiplier)
        {
            string key = MakeMultiplierKey(zordIds);
            _hiddenMultipliers[key] = multiplier;
        }

        // === Combination ===

        /// <summary>
        /// Combine all component zords into the Megazord.
        /// All zords must be slotted before calling this.
        /// Returns false if combination is on cooldown or no zords are slotted.
        /// </summary>
        public bool Combine(IEnumerable<ZordInstance> initialZords)
        {
            if (IsCombined || CombinationCooldown.IsOnCooldown)
                return false;

            _componentZords.Clear();
            _componentZords.AddRange(initialZords);

            if (_componentZords.Count == 0)
                return false;

            IsCombined = true;
            CombinationTimer = BaseCombinationDuration;
            return true;
        }

        /// <summary>
        /// Hot-swap a zord during combination. Free action.
        /// Recalculates stats and potentially changes the Megazord's type.
        /// Returns the removed zord, or null if swap failed.
        /// </summary>
        public ZordInstance SwapZord(int slotIndex, ZordInstance newZord)
        {
            if (!IsCombined) return null;
            if (slotIndex < 0 || slotIndex >= _componentZords.Count) return null;
            if (_componentZords.Contains(newZord)) return null;

            var removed = _componentZords[slotIndex];
            _componentZords[slotIndex] = newZord;

            return removed;
        }

        /// <summary>
        /// End combination (timer expired or after ultimate attack).
        /// Starts the combination cooldown.
        /// </summary>
        public void Decombine()
        {
            IsCombined = false;
            CombinationTimer = 0;
            CombinationCooldown.Activate();
        }

        /// <summary>
        /// Execute the ultimate attack — immediately ends combination.
        /// Returns the hidden multiplier for the current zord configuration.
        /// </summary>
        public float ExecuteUltimate()
        {
            if (!IsCombined) return 0f;

            float multiplier = GetCurrentHiddenMultiplier();
            Decombine();
            return multiplier;
        }

        // === Stats (Additive from component zords) ===

        /// <summary>
        /// Get the Megazord's combined stats — sum of all component zord stats.
        /// </summary>
        public StatBlock GetStats()
        {
            var combined = new StatBlock();
            foreach (var zord in _componentZords)
            {
                combined = combined.Add(zord.GetStats());
            }
            return combined;
        }

        // === Type (From two highest-level zords) ===

        /// <summary>
        /// Get the Megazord's dual type derived from the two highest-level component zords.
        /// Ties broken by recruitment order (earlier zord wins).
        /// </summary>
        public DualType GetDualType()
        {
            if (_componentZords.Count == 0)
                return DualType.Single(ElementalType.Blaze); // fallback

            var topTwo = _componentZords
                .OrderByDescending(z => z.Level)
                .ThenBy(z => z.Data.RecruitmentOrder) // tiebreaker: earlier recruited wins
                .Take(2)
                .ToList();

            if (topTwo.Count == 1)
                return DualType.Single(topTwo[0].Data.Type);

            return new DualType(topTwo[0].Data.Type, topTwo[1].Data.Type);
        }

        // === Turn Processing ===

        /// <summary>
        /// Tick the combination timer. If it expires, decombine.
        /// Call once per turn while combined.
        /// </summary>
        public bool TickTimer()
        {
            if (!IsCombined) return false;

            CombinationTimer--;
            if (CombinationTimer <= 0)
            {
                Decombine();
                return true; // combination ended
            }
            return false;
        }

        /// <summary>
        /// Tick the combination cooldown (when not combined).
        /// </summary>
        public void TickCooldown()
        {
            if (!IsCombined)
            {
                CombinationCooldown.Tick();
            }
        }

        // === Hidden Multiplier Lookup ===

        private float GetCurrentHiddenMultiplier()
        {
            var zordIds = _componentZords.Select(z => z.Data.Id);
            string key = MakeMultiplierKey(zordIds);

            return _hiddenMultipliers.ContainsKey(key) ? _hiddenMultipliers[key] : 1.0f;
        }

        private static string MakeMultiplierKey(IEnumerable<string> zordIds)
        {
            return string.Join("_", zordIds.OrderBy(id => id));
        }

        // === Queries ===

        /// <summary>
        /// Get zords available for swapping (not currently slotted).
        /// </summary>
        public List<ZordInstance> GetSwappableZords()
        {
            return _availableZords
                .Where(z => !_componentZords.Contains(z))
                .ToList();
        }

        /// <summary>
        /// Preview what the stats and type would be if a specific swap was made.
        /// For the zord swap UI.
        /// </summary>
        public (StatBlock Stats, DualType Type) PreviewSwap(int slotIndex, ZordInstance newZord)
        {
            if (slotIndex < 0 || slotIndex >= _componentZords.Count)
                return (GetStats(), GetDualType());

            // Temporarily swap for calculation
            var original = _componentZords[slotIndex];
            _componentZords[slotIndex] = newZord;

            var stats = GetStats();
            var type = GetDualType();

            // Restore
            _componentZords[slotIndex] = original;

            return (stats, type);
        }
    }
}
