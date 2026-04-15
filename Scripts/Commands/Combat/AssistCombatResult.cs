using TokuTactics.Core.Combat;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Result of a single assist within a combat action.
    /// Lives in the Commands layer so commands can produce it without
    /// referencing the orchestrator namespace.
    /// </summary>
    public class AssistCombatResult
    {
        public string AssisterId { get; set; }
        public int BondTier { get; set; }
        public bool IsPairAttack { get; set; }
        public DamageResult Damage { get; set; }

        /// <summary>Whether a bond tier change occurred from this assist.</summary>
        public BondTierChange BondTierChange { get; set; }

        /// <summary>Whether tier 2 disrupted the assister's form.</summary>
        public bool FormDisrupted { get; set; }

        /// <summary>The form that was vacated by tier 2 disruption.</summary>
        public string VacatedFormId { get; set; }

        /// <summary>Whether tier 4 refresh is available from this assist.</summary>
        public bool RefreshAvailable { get; set; }
    }
}
