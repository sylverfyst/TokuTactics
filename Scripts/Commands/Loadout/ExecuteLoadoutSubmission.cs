using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Loadout;

namespace TokuTactics.Commands.Loadout
{
    /// <summary>
    /// Command: Validates a loadout submission against constraints.
    /// Composes ValidateLoadoutSubmission brick. Pure validation — does NOT equip
    /// forms or lock the loadout. The orchestrator performs those mutations.
    /// </summary>
    public static class ExecuteLoadoutSubmission
    {
        public static LoadoutResult Execute(
            List<string> selectedFormIds,
            bool isLoadoutLocked,
            int budget,
            HashSet<string> registeredFormIds,
            string baseFormId,
            Func<List<string>, bool, int, HashSet<string>, string, LoadoutResult> validateSubmission = null)
        {
            validateSubmission ??= ValidateLoadoutSubmission.Execute;

            return validateSubmission(
                selectedFormIds,
                isLoadoutLocked,
                budget,
                registeredFormIds,
                baseFormId);
        }
    }
}
