using System.Collections.Generic;
using System.Linq;
using TokuTactics.Bricks.Form;
using TokuTactics.Commands.Form;
using TokuTactics.Core.Cooldown;
using TokuTactics.Core.Form;
using TokuTactics.Entities.Forms;

namespace TokuTactics.Systems.FormManagement
{
    /// <summary>
    /// Orchestrator: Manages the shared form economy for a team of Rangers.
    ///
    /// Owns state (equipped forms, occupancy, cooldowns). Delegates validation
    /// to bricks and turn processing to the ProcessFormPoolTurn command.
    /// </summary>
    public class FormPool
    {
        // === State ===

        private readonly HashSet<string> _equippedFormIds = new();
        private readonly Dictionary<string, string> _occupiedBy = new();
        private readonly Dictionary<string, CooldownTimer> _cooldowns = new();
        private readonly Dictionary<string, List<FormInstance>> _formInstances = new();
        private readonly Dictionary<string, FormData> _formDefs = new();

        /// <summary>The base form ID — exempt from exclusivity and cooldowns.</summary>
        public string BaseFormId { get; }

        // === Budget ===

        public int Budget { get; private set; }
        public int EquippedCount => _equippedFormIds.Count;
        public bool HasBudgetRemaining => EquippedCount < Budget;

        // === Loadout Lock ===

        public bool IsLoadoutLocked { get; private set; }

        // === Configuration ===

        public float BaseRegenPerTurn { get; set; } = 5.0f;

        public FormPool(string baseFormId, int startingBudget)
        {
            BaseFormId = baseFormId;
            Budget = startingBudget;
        }

        // === Setup ===

        public void RegisterForm(FormData formData)
        {
            _formDefs[formData.Id] = formData;
            _cooldowns[formData.Id] = new CooldownTimer(formData.CooldownDuration);
            _formInstances[formData.Id] = new List<FormInstance>();
        }

        public void RegisterFormInstance(FormInstance instance)
        {
            if (_formInstances.ContainsKey(instance.Data.Id))
                _formInstances[instance.Data.Id].Add(instance);
        }

        public void ExpandBudget(int amount)
        {
            Budget += amount;
        }

        // === Loadout (Pre-Mission) ===

        public bool EquipForm(string formId)
        {
            if (!ValidateFormEquip.Execute(
                formId, BaseFormId, IsLoadoutLocked,
                new HashSet<string>(_formDefs.Keys),
                _equippedFormIds.Count, Budget))
                return false;

            if (formId != BaseFormId)
                _equippedFormIds.Add(formId);

            return true;
        }

        public void LockLoadout()
        {
            IsLoadoutLocked = true;
        }

        public void ClearLoadout()
        {
            _equippedFormIds.Clear();
            _occupiedBy.Clear();
            IsLoadoutLocked = false;
            foreach (var cooldown in _cooldowns.Values)
                cooldown.Reset();
        }

        // === Form Availability ===

        public FormAvailability CheckAvailability(string formId, string rangerId)
        {
            return CheckFormAvailability.Execute(
                formId, rangerId, BaseFormId, _equippedFormIds, _cooldowns, _occupiedBy);
        }

        public List<FormData> GetAvailableForms(string rangerId)
        {
            var available = new List<FormData>();
            foreach (var formId in _equippedFormIds)
            {
                if (CheckAvailability(formId, rangerId) == FormAvailability.Available)
                    available.Add(_formDefs[formId]);
            }
            return available;
        }

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

        public bool OccupyForm(string formId, string rangerId)
        {
            if (formId == BaseFormId) return true;
            if (CheckAvailability(formId, rangerId) != FormAvailability.Available)
                return false;

            _occupiedBy[formId] = rangerId;
            return true;
        }

        public int VacateForm(string formId, string rangerId, int magModifier = 0)
        {
            if (formId == BaseFormId) return 0;

            if (_occupiedBy.ContainsKey(formId) && _occupiedBy[formId] == rangerId)
                _occupiedBy.Remove(formId);

            if (_cooldowns.ContainsKey(formId))
            {
                _cooldowns[formId].Activate(magModifier);
                return _cooldowns[formId].RemainingTurns;
            }

            return 0;
        }

        public void PutBaseFormOnCooldown(int duration)
        {
            _cooldowns[BaseFormId] = new CooldownTimer(duration);
            _cooldowns[BaseFormId].Activate();
        }

        public bool IsBaseFormAvailable()
        {
            if (!_cooldowns.ContainsKey(BaseFormId)) return true;
            return _cooldowns[BaseFormId].IsAvailable;
        }

        // === Turn Processing ===

        public void ProcessTurn()
        {
            ProcessFormPoolTurn.Execute(_cooldowns, _formInstances, BaseRegenPerTurn);
        }

        // === Permadeath ===

        public void PermanentlyRemoveForm(string formId)
        {
            if (formId == BaseFormId) return;
            _formDefs.Remove(formId);
            _equippedFormIds.Remove(formId);
            _occupiedBy.Remove(formId);
            _cooldowns.Remove(formId);
            _formInstances.Remove(formId);
        }
    }

}
