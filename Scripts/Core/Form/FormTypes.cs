using TokuTactics.Entities.Forms;

namespace TokuTactics.Core.Form
{
    public enum FormAvailability
    {
        Available,
        NotEquipped,
        OnCooldown,
        OccupiedByOther
    }

    /// <summary>
    /// Status entry for a form in the pool. Used by the UI to display the morph/switch menu.
    /// </summary>
    public class FormPoolEntry
    {
        public FormData FormData { get; set; }
        public bool IsEquipped { get; set; }
        public bool IsOnCooldown { get; set; }
        public int CooldownRemaining { get; set; }
        public string OccupiedByRangerId { get; set; }
    }
}
