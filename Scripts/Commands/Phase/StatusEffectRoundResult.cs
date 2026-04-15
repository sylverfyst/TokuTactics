using System.Collections.Generic;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Result of processing all status effects for a round start.
    /// Contains declarative event data for the orchestrator to publish.
    /// </summary>
    public class StatusEffectRoundResult
    {
        /// <summary>Rangers whose form died from DoT, triggering demorph.</summary>
        public List<DemorphEventData> DemorphEvents { get; set; } = new();

        /// <summary>Enemies that became aggressive from DoT damage.</summary>
        public List<AggressionEventData> AggressionEvents { get; set; } = new();
    }

    public class DemorphEventData
    {
        public string RangerId { get; set; }
        public string LostFormId { get; set; }
    }

    public class AggressionEventData
    {
        public string EnemyId { get; set; }
        public float HealthPercentage { get; set; }
    }
}
