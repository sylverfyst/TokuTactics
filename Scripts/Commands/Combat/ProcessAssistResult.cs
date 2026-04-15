using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Declarative result of processing a single assist attack.
    /// Contains the combat result and event data for the orchestrator to publish.
    /// </summary>
    public class ProcessAssistResult
    {
        /// <summary>The assist combat result (damage, bond tier, flags).</summary>
        public AssistCombatResult AssistCombatResult { get; set; }

        /// <summary>Bond tier change if one occurred. Null otherwise.</summary>
        public BondTierChange BondTierChange { get; set; }

        /// <summary>Whether aggression was triggered on the target.</summary>
        public bool AggressionTriggered { get; set; }

        /// <summary>Enemy ID if aggression triggered. Null otherwise.</summary>
        public string AggressionEnemyId { get; set; }

        /// <summary>Enemy health percentage if aggression triggered.</summary>
        public float AggressionHealthPercentage { get; set; }
    }
}
