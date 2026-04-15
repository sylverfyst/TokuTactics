using System.Collections.Generic;
using TokuTactics.Core.Cooldown;
using TokuTactics.Core.Form;

namespace TokuTactics.Bricks.Form
{
    /// <summary>
    /// Checks if a form can be switched into by a specific ranger.
    /// Returns a FormAvailability enum: Available, NotEquipped, OnCooldown, or OccupiedByOther.
    /// Base form is always available.
    /// </summary>
    public static class CheckFormAvailability
    {
        public static FormAvailability Execute(
            string formId,
            string rangerId,
            string baseFormId,
            HashSet<string> equippedFormIds,
            Dictionary<string, CooldownTimer> cooldowns,
            Dictionary<string, string> occupiedBy)
        {
            if (formId == baseFormId)
                return FormAvailability.Available;

            if (!equippedFormIds.Contains(formId))
                return FormAvailability.NotEquipped;

            if (cooldowns.TryGetValue(formId, out var cooldown) && cooldown.IsOnCooldown)
                return FormAvailability.OnCooldown;

            if (occupiedBy.TryGetValue(formId, out var occupant) && occupant != rangerId)
                return FormAvailability.OccupiedByOther;

            return FormAvailability.Available;
        }
    }
}
