using System.Collections.Generic;
using TokuTactics.Systems.LoadoutSelection;

namespace TokuTactics.Bricks.Loadout
{
    /// <summary>
    /// Validates a loadout submission against constraints.
    /// Checks: not already locked, not empty, within budget, all form IDs registered.
    /// </summary>
    public static class ValidateLoadoutSubmission
    {
        public static LoadoutResult Execute(
            List<string> selectedFormIds,
            bool isLoadoutLocked,
            int budget,
            HashSet<string> registeredFormIds,
            string baseFormId)
        {
            if (isLoadoutLocked)
                return LoadoutResult.AlreadyLocked;

            if (selectedFormIds == null || selectedFormIds.Count == 0)
                return LoadoutResult.Empty;

            if (selectedFormIds.Count > budget)
                return LoadoutResult.OverBudget;

            foreach (var formId in selectedFormIds)
            {
                if (formId == baseFormId) continue;
                if (!registeredFormIds.Contains(formId))
                    return LoadoutResult.InvalidForm;
            }

            return LoadoutResult.Accepted;
        }
    }
}
