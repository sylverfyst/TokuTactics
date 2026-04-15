using System.Collections.Generic;
using TokuTactics.Systems.AssistResolution;

namespace TokuTactics.Bricks.Assist
{
    /// <summary>
    /// Checks if a unit is eligible to provide an assist.
    /// Must be a known ranger (in rangerStates), must be morphed, and must not be the attacker.
    /// </summary>
    public static class CheckAssistEligibility
    {
        public static bool Execute(
            string unitId,
            string attackerId,
            IReadOnlyDictionary<string, AssistCandidateState> rangerStates)
        {
            if (unitId == attackerId) return false;
            if (!rangerStates.TryGetValue(unitId, out var state)) return false;
            if (!state.IsMorphed) return false;

            return true;
        }
    }
}
