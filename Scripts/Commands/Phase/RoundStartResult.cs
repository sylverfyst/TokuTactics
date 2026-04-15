using System.Collections.Generic;
using TokuTactics.Systems.PhaseManagement;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Result of starting a new round. Contains the new round number,
    /// whether the mission ended, and event data from status effect processing.
    /// </summary>
    public class RoundStartResult
    {
        /// <summary>The new round number after increment.</summary>
        public int NewRoundNumber { get; set; }

        /// <summary>Whether the mission ended during round start (status effect death).</summary>
        public bool MissionEnded { get; set; }

        /// <summary>The end state if mission ended. Only meaningful if MissionEnded is true.</summary>
        public MissionState EndState { get; set; }

        /// <summary>ID of the fallen ranger if defeat.</summary>
        public string FallenRangerId { get; set; }

        /// <summary>Demorph events from status effect processing.</summary>
        public List<DemorphEventData> DemorphEvents { get; set; } = new();

        /// <summary>Aggression events from status effect processing.</summary>
        public List<AggressionEventData> AggressionEvents { get; set; } = new();
    }
}
