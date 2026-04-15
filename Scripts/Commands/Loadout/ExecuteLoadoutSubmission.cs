using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Loadout;
using TokuTactics.Systems.FormManagement;
using TokuTactics.Systems.LoadoutSelection;

namespace TokuTactics.Commands.Loadout
{
    /// <summary>
    /// Command: Validates a loadout submission and equips the selected forms.
    /// Composes ValidateLoadoutSubmission brick and FormPool.EquipForm.
    /// Does NOT lock the loadout or publish events — the orchestrator handles that.
    /// </summary>
    public static class ExecuteLoadoutSubmission
    {
        public static LoadoutResult Execute(
            List<string> selectedFormIds,
            FormPool formPool,
            Func<List<string>, bool, int, HashSet<string>, string, LoadoutResult> validateSubmission = null)
        {
            validateSubmission ??= ValidateLoadoutSubmission.Execute;

            // Build registered IDs from pool status
            var poolStatus = formPool.GetPoolStatus();
            var registeredIds = new HashSet<string>();
            foreach (var entry in poolStatus)
                registeredIds.Add(entry.FormData.Id);

            // Validate
            var validation = validateSubmission(
                selectedFormIds,
                formPool.IsLoadoutLocked,
                formPool.Budget,
                registeredIds,
                formPool.BaseFormId);

            if (validation != LoadoutResult.Accepted)
                return validation;

            // Equip selected forms
            foreach (var formId in selectedFormIds)
                formPool.EquipForm(formId);

            return LoadoutResult.Accepted;
        }
    }
}
