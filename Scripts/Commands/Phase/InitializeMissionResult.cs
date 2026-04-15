using System.Collections.Generic;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Result of mission initialization. Contains the validated/defaulted defeat target set.
    /// </summary>
    public class InitializeMissionResult
    {
        /// <summary>The resolved defeat target IDs (defaulted to all enemy IDs if none specified).</summary>
        public HashSet<string> DefeatTargetIds { get; set; }
    }
}
