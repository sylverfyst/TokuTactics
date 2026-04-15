using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Bricks.Bond;

namespace TokuTactics.Systems.ActionEconomy
{
    /// <summary>
    /// Tracks bond levels between all Ranger pairs.
    /// Bonds grow through assists in combat and unlock tiered mechanical effects.
    /// 
    /// Tier 1: Increased assist damage
    /// Tier 2: Pair attack (pulls assister to base form)
    /// Tier 3: Pair attack (no disruption, replaces tier 2)
    /// Tier 4: Tier 3 + action refresh (once per round per character)
    /// </summary>
    public class BondTracker
    {
        private readonly Dictionary<string, BondState> _bonds = new();

        /// <summary>
        /// Tunable: experience required to reach each tier.
        /// Index 0 = tier 1 threshold, index 1 = tier 2, etc.
        /// A bond starts at tier 0 and must earn experience to reach tier 1.
        /// </summary>
        public int[] TierThresholds { get; set; } = { 50, 150, 350, 700 };

        /// <summary>
        /// Get the bond state between two Rangers. Order-independent.
        /// </summary>
        public BondState GetBond(string rangerA, string rangerB)
        {
            string key = MakeKey(rangerA, rangerB);
            if (!_bonds.ContainsKey(key))
            {
                _bonds[key] = new BondState(rangerA, rangerB);
            }
            return _bonds[key];
        }

        /// <summary>
        /// Add bond experience from an assist action.
        /// CHA multiplier from the assisting Ranger scales the gain.
        /// Returns the new bond tier if it changed.
        /// </summary>
        public BondTierChange AddAssistExperience(string actorId, string assisterId, float chaMultiplier)
        {
            var bond = GetBond(actorId, assisterId);
            int previousTier = bond.Tier;

            int baseExp = 10; // tunable
            int scaledExp = CalculateScaledBondExp.Execute(baseExp, chaMultiplier);
            bond.Experience += scaledExp;
            bond.Tier = ResolveBondTier.Execute(bond.Experience, TierThresholds);

            if (bond.Tier > previousTier)
            {
                return new BondTierChange
                {
                    RangerA = actorId,
                    RangerB = assisterId,
                    OldTier = previousTier,
                    NewTier = bond.Tier
                };
            }

            return null;
        }

        /// <summary>
        /// Get all bonds at or above a specific tier.
        /// </summary>
        public List<BondState> GetBondsAtTier(int minTier)
        {
            return _bonds.Values.Where(b => b.Tier >= minTier).ToList();
        }

        /// <summary>
        /// Get all bonds for a specific Ranger.
        /// </summary>
        public List<BondState> GetBondsForRanger(string rangerId)
        {
            return _bonds.Values
                .Where(b => b.RangerA == rangerId || b.RangerB == rangerId)
                .ToList();
        }

        private string MakeKey(string a, string b)
        {
            return string.Compare(a, b, StringComparison.Ordinal) < 0
                ? $"{a}_{b}"
                : $"{b}_{a}";
        }
    }

    /// <summary>
    /// The state of a bond between two specific Rangers.
    /// </summary>
    /// <summary>
    /// Type: Pure data shape for the bond between two Rangers.
    /// Operations on this type are performed by bricks and the BondTracker orchestrator.
    /// </summary>
    public class BondState
    {
        public string RangerA { get; set; }
        public string RangerB { get; set; }
        public int Experience { get; set; }
        public int Tier { get; set; }

        public BondState(string rangerA, string rangerB)
        {
            RangerA = rangerA;
            RangerB = rangerB;
        }

        /// <summary>Check if this bond involves a specific Ranger.</summary>
        public bool Involves(string rangerId) => RangerA == rangerId || RangerB == rangerId;

        /// <summary>Get the other Ranger in this bond pair.</summary>
        public string GetPartner(string rangerId)
        {
            if (RangerA == rangerId) return RangerB;
            if (RangerB == rangerId) return RangerA;
            return null;
        }
    }

    /// <summary>
    /// Event when a bond reaches a new tier.
    /// </summary>
    public class BondTierChange
    {
        public string RangerA { get; set; }
        public string RangerB { get; set; }
        public int OldTier { get; set; }
        public int NewTier { get; set; }
    }
}
