using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Cooldown;
using TokuTactics.Entities.Forms;

namespace TokuTactics.Systems.FormManagement
{
    /// <summary>
    /// Manages the shared form economy for a team of Rangers.
    /// 
    /// Core rules:
    /// - Only one Ranger can occupy a non-base form at a time
    /// - When a Ranger leaves a form, it goes on cooldown for the entire team
    /// - Base form is exempt — any Ranger can be in base form simultaneously
    /// - Form budget limits how many forms can be equipped per mission
    /// - Cooldown duration is modified by the leaving Ranger's MAG stat
    /// 
    /// The core five share one FormPool. The 6th Ranger has their own separate FormPool.
    /// </summary>
    public class FormPool
    {
        // === State ===

        /// <summary>Forms currently equipped for this mission (within budget).</summary>
        private readonly HashSet<string> _equippedFormIds = new();

        /// <summary>Which Ranger currently occupies each form. Key = FormData.Id.</summary>
        private readonly Dictionary<string, string> _occupiedBy = new();

        /// <summary>Cooldown timers per form. Key = FormData.Id.</summary>
        private readonly Dictionary<string, CooldownTimer> _cooldowns = new();

        /// <summary>Reference to form instances for health regeneration during cooldown.</summary>
        private readonly Dictionary<string, List<FormInstance>> _formInstances = new();

        /// <summary>All available form definitions in this pool.</summary>
        private readonly Dictionary<string, FormData> _formDefs = new();

        /// <summary>The base form ID — exempt from exclusivity and cooldowns.</summary>
        public string BaseFormId { get; }

        // === Budget ===

        /// <summary>Current form budget — how many non-base forms can be equipped per mission.</summary>
        public int Budget { get; private set; }

        /// <summary>How many forms are currently equipped.</summary>
        public int EquippedCount => _equippedFormIds.Count;

        /// <summary>Whether the budget allows equipping more forms.</summary>
        public bool HasBudgetRemaining => EquippedCount < Budget;

        // === Loadout Lock ===

        /// <summary>
        /// Whether the loadout has been locked for this mission.
        /// Set to true on first morph. Once locked, no forms can be equipped or unequipped.
        /// Loadout is locked once per map — cannot be retriggered even if all Rangers demorph.
        /// </summary>
        public bool IsLoadoutLocked { get; private set; }

        // === Configuration ===

        /// <summary>Tunable: base health regeneration per cooldown turn.</summary>
        public float BaseRegenPerTurn { get; set; } = 5.0f;

        public FormPool(string baseFormId, int startingBudget)
        {
            BaseFormId = baseFormId;
            Budget = startingBudget;
        }

        // === Setup ===

        /// <summary>
        /// Register a form definition in this pool.
        /// Call during initialization for all forms this pool manages.
        /// </summary>
        public void RegisterForm(FormData formData)
        {
            _formDefs[formData.Id] = formData;
            _cooldowns[formData.Id] = new CooldownTimer(formData.CooldownDuration);
            _formInstances[formData.Id] = new List<FormInstance>();
        }

        /// <summary>
        /// Register a Ranger's form instance for cooldown health regen tracking.
        /// </summary>
        public void RegisterFormInstance(FormInstance instance)
        {
            if (_formInstances.ContainsKey(instance.Data.Id))
            {
                _formInstances[instance.Data.Id].Add(instance);
            }
        }

        /// <summary>Expand the form budget (story progression).</summary>
        public void ExpandBudget(int amount)
        {
            Budget += amount;
        }

        // === Loadout (Pre-Mission) ===

        /// <summary>
        /// Equip a form for this mission. Must be within budget and loadout not yet locked.
        /// </summary>
        public bool EquipForm(string formId)
        {
            if (formId == BaseFormId) return true; // Base is always equipped
            if (IsLoadoutLocked) return false;
            if (!_formDefs.ContainsKey(formId)) return false;
            if (_equippedFormIds.Count >= Budget) return false;

            _equippedFormIds.Add(formId);
            return true;
        }

        /// <summary>
        /// Lock the loadout. Called on first morph of the mission.
        /// Once locked, no forms can be equipped or unequipped until ClearLoadout().
        /// Loadout is locked once per map — cannot be retriggered.
        /// </summary>
        public void LockLoadout()
        {
            IsLoadoutLocked = true;
        }

        /// <summary>
        /// Clear all equipped forms and unlock the loadout (between missions).
        /// </summary>
        public void ClearLoadout()
        {
            _equippedFormIds.Clear();
            _occupiedBy.Clear();
            IsLoadoutLocked = false;
            foreach (var cooldown in _cooldowns.Values)
                cooldown.Reset();
        }

        // === Form Availability ===

        /// <summary>
        /// Check if a form can be switched into by a specific Ranger.
        /// Must be: equipped, not occupied by another Ranger, not on cooldown.
        /// Base form is always available.
        /// </summary>
        public FormAvailability CheckAvailability(string formId, string rangerId)
        {
            if (formId == BaseFormId)
                return FormAvailability.Available;

            if (!_equippedFormIds.Contains(formId))
                return FormAvailability.NotEquipped;

            if (_cooldowns.ContainsKey(formId) && _cooldowns[formId].IsOnCooldown)
                return FormAvailability.OnCooldown;

            if (_occupiedBy.ContainsKey(formId) && _occupiedBy[formId] != rangerId)
                return FormAvailability.OccupiedByOther;

            return FormAvailability.Available;
        }

        /// <summary>
        /// Get all forms currently available for a specific Ranger to switch into.
        /// </summary>
        public List<FormData> GetAvailableForms(string rangerId)
        {
            var available = new List<FormData>();

            foreach (var formId in _equippedFormIds)
            {
                if (CheckAvailability(formId, rangerId) == FormAvailability.Available)
                {
                    available.Add(_formDefs[formId]);
                }
            }

            return available;
        }

        /// <summary>
        /// Get full status of all forms in the pool (for the morph/switch menu UI).
        /// </summary>
        public List<FormPoolEntry> GetPoolStatus()
        {
            var entries = new List<FormPoolEntry>();

            foreach (var kvp in _formDefs)
            {
                if (kvp.Key == BaseFormId) continue;

                entries.Add(new FormPoolEntry
                {
                    FormData = kvp.Value,
                    IsEquipped = _equippedFormIds.Contains(kvp.Key),
                    IsOnCooldown = _cooldowns.ContainsKey(kvp.Key) && _cooldowns[kvp.Key].IsOnCooldown,
                    CooldownRemaining = _cooldowns.ContainsKey(kvp.Key) ? _cooldowns[kvp.Key].RemainingTurns : 0,
                    OccupiedByRangerId = _occupiedBy.ContainsKey(kvp.Key) ? _occupiedBy[kvp.Key] : null
                });
            }

            return entries;
        }

        // === Form Switching ===

        /// <summary>
        /// A Ranger occupies a form. Called when switching into a form.
        /// </summary>
        public bool OccupyForm(string formId, string rangerId)
        {
            if (formId == BaseFormId) return true;
            if (CheckAvailability(formId, rangerId) != FormAvailability.Available)
                return false;

            _occupiedBy[formId] = rangerId;
            return true;
        }

        /// <summary>
        /// A Ranger vacates a form. Starts the cooldown.
        /// MAG modifier reduces cooldown duration.
        /// Returns the cooldown duration that was set.
        /// </summary>
        public int VacateForm(string formId, string rangerId, int magModifier = 0)
        {
            if (formId == BaseFormId) return 0; // Base form doesn't go on cooldown normally

            if (_occupiedBy.ContainsKey(formId) && _occupiedBy[formId] == rangerId)
            {
                _occupiedBy.Remove(formId);
            }

            if (_cooldowns.ContainsKey(formId))
            {
                _cooldowns[formId].Activate(magModifier);
                return _cooldowns[formId].RemainingTurns;
            }

            return 0;
        }

        /// <summary>
        /// Put base form on cooldown (after form death demorph).
        /// Always creates/replaces the timer with the specified duration.
        /// </summary>
        public void PutBaseFormOnCooldown(int duration)
        {
            _cooldowns[BaseFormId] = new CooldownTimer(duration);
            _cooldowns[BaseFormId].Activate();
        }

        /// <summary>
        /// Check if base form is available (relevant after demorph).
        /// </summary>
        public bool IsBaseFormAvailable()
        {
            if (!_cooldowns.ContainsKey(BaseFormId)) return true;
            return _cooldowns[BaseFormId].IsAvailable;
        }

        // === Turn Processing ===

        /// <summary>
        /// Tick all cooldowns and apply health regeneration to forms on cooldown.
        /// Call once per full turn (after both player and enemy phases).
        /// Regeneration only applies to forms that are STILL on cooldown after ticking,
        /// not forms that just became available this turn.
        /// </summary>
        public void ProcessTurn()
        {
            foreach (var kvp in _cooldowns)
            {
                bool wasOnCooldown = kvp.Value.IsOnCooldown;
                if (wasOnCooldown)
                {
                    kvp.Value.Tick();

                    // Only regenerate if the form is still on cooldown after ticking.
                    // A form that just came off cooldown doesn't get a bonus regen tick.
                    if (kvp.Value.IsOnCooldown && _formInstances.ContainsKey(kvp.Key))
                    {
                        foreach (var instance in _formInstances[kvp.Key])
                        {
                            if (instance.Health.IsAlive)
                            {
                                instance.Health.Regenerate(BaseRegenPerTurn);
                            }
                        }
                    }
                }
            }
        }

        // === Permadeath ===

        /// <summary>
        /// Permanently remove a form from the pool.
        /// Called when a form dies on permadeath difficulty.
        /// </summary>
        public void PermanentlyRemoveForm(string formId)
        {
            if (formId == BaseFormId) return; // Base form cannot be permanently lost

            _formDefs.Remove(formId);
            _equippedFormIds.Remove(formId);
            _occupiedBy.Remove(formId);
            _cooldowns.Remove(formId);
            _formInstances.Remove(formId);
        }
    }

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
