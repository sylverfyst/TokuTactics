using System.Collections.Generic;

namespace TokuTactics.Bricks.Form
{
    /// <summary>
    /// Validates if a form can be equipped for a mission.
    /// Must not be the base form, loadout must not be locked,
    /// form must be registered, and budget must have room.
    /// </summary>
    public static class ValidateFormEquip
    {
        public static bool Execute(
            string formId,
            string baseFormId,
            bool isLoadoutLocked,
            HashSet<string> registeredFormIds,
            int equippedCount,
            int budget)
        {
            if (formId == baseFormId) return true; // Base is always equipped
            if (isLoadoutLocked) return false;
            if (!registeredFormIds.Contains(formId)) return false;
            if (equippedCount >= budget) return false;

            return true;
        }
    }
}
