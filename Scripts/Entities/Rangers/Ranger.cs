using System;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Core.Health;
using TokuTactics.Core.Combat;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Entities.Forms;
using TokuTactics.Systems.ActionEconomy;

namespace TokuTactics.Entities.Rangers
{
    /// <summary>
    /// A Ranger — one of the five core team members or the 6th Ranger.
    /// This is the central entity that ties together:
    /// - Innate type (permanent, visible)
    /// - Hidden proclivity (hidden, per-save randomized)
    /// - Morph state (unmorphed / morphed / demorphed)
    /// - Form instances (per-Ranger leveling, health pools)
    /// - Personal ability (unmorphed only)
    /// - Status effects
    /// - Combo chain tracking
    /// 
    /// The Ranger is the PERSON. Forms are the CLASSES they wear.
    /// </summary>
    public class Ranger : ICombatActor, ITurnParticipant
    {
        // === Identity ===

        public string Id { get; }
        public string Name { get; }

        /// <summary>The Ranger's innate elemental type. Permanent, visible to the player.</summary>
        public ElementalType IntrinsicType { get; }

        /// <summary>Hidden stat proclivity. Never shown to the player directly.</summary>
        public Proclivity Proclivity { get; }

        /// <summary>Personal ability, usable only while unmorphed.</summary>
        public IPersonalAbility PersonalAbility { get; }

        // === State ===

        /// <summary>Current morph state.</summary>
        public MorphState MorphState { get; private set; } = MorphState.Unmorphed;

        /// <summary>The currently active form (null when unmorphed/demorphed).</summary>
        public FormInstance CurrentForm { get; private set; }

        /// <summary>Fixed unmorphed stats. No levels, never change.</summary>
        public StatBlock UnmorphedStats { get; }

        /// <summary>Unmorphed health pool. If this hits 0, mission is lost.</summary>
        public HealthPool UnmorphedHealth { get; }

        /// <summary>Active status effects on this Ranger.</summary>
        public StatusEffectTracker StatusEffects { get; } = new();

        /// <summary>Combo chain tracker for form switch damage scaling.</summary>
        public ComboScaler ComboScaler { get; } = new();

        // === Form Management ===

        /// <summary>
        /// All form instances this Ranger has experience with.
        /// Key is FormData.Id. Per-Ranger, per-form leveling.
        /// </summary>
        private readonly Dictionary<string, FormInstance> _formInstances = new();

        /// <summary>
        /// The base form instance. Always available, always the entry point for morphing.
        /// Base form IS a form — it levels like any other.
        /// </summary>
        public FormInstance BaseForm { get; }

        /// <summary>Read-only access to all form instances.</summary>
        public IReadOnlyDictionary<string, FormInstance> FormInstances => _formInstances;

        /// <summary>Whether this Ranger is the 6th Ranger (separate systems).</summary>
        public bool IsSixthRanger { get; }

        // === Constructor ===

        public Ranger(
            string id,
            string name,
            ElementalType intrinsicType,
            Proclivity proclivity,
            IPersonalAbility personalAbility,
            StatBlock unmorphedStats,
            float unmorphedMaxHealth,
            FormData baseFormData,
            bool isSixthRanger = false)
        {
            Id = id;
            Name = name;
            IntrinsicType = intrinsicType;
            Proclivity = proclivity;
            PersonalAbility = personalAbility;
            UnmorphedStats = unmorphedStats;
            UnmorphedHealth = new HealthPool(unmorphedMaxHealth);
            IsSixthRanger = isSixthRanger;

            BaseForm = new FormInstance(baseFormData);
            _formInstances[baseFormData.Id] = BaseForm;
        }

        // === ICombatActor Implementation ===

        /// <summary>
        /// The Ranger's current effective type.
        /// Unmorphed: single type (intrinsic only).
        /// Morphed: dual type (intrinsic + current form type).
        /// </summary>
        public DualType DualType => MorphState == MorphState.Morphed && CurrentForm != null
            ? new DualType(IntrinsicType, CurrentForm.Data.Type)
            : DualType.Single(IntrinsicType);

        ElementalType ICombatTarget.Type => IntrinsicType;

        /// <summary>
        /// Current effective stats based on morph state.
        /// Unmorphed: fixed unmorphed stats. Morphed: current form stats.
        /// </summary>
        public StatBlock Stats => MorphState == MorphState.Morphed && CurrentForm != null
            ? CurrentForm.GetStats()
            : UnmorphedStats;

        public bool IsAlive => UnmorphedHealth.IsAlive;

        public float ComboScaleMultiplier => ComboScaler.DamageMultiplier;

        // === ITurnParticipant Implementation ===

        string ITurnParticipant.ParticipantId => Id;
        float ITurnParticipant.Speed => Stats.Get(StatType.SPD);
        bool ITurnParticipant.CanAct => IsAlive;

        // === Morph Actions ===

        /// <summary>
        /// Morph into base form from the unmorphed state. Costs the Ranger's action.
        /// Only valid from Unmorphed state. Use Remorph() for Demorphed state.
        /// 
        /// IMPORTANT: The caller (combat system) is responsible for triggering
        /// the team loadout selection on the first morph of the mission.
        /// </summary>
        public bool Morph()
        {
            if (MorphState != MorphState.Unmorphed)
                return false;

            CurrentForm = BaseForm;
            MorphState = MorphState.Morphed;
            return true;
        }

        /// <summary>
        /// Switch to a different form. Resets action economy (free action).
        /// Returns the form being LEFT (for cooldown activation by the FormPool).
        /// Returns null if the switch is invalid.
        /// </summary>
        public FormInstance SwitchForm(FormInstance newForm)
        {
            if (MorphState != MorphState.Morphed || CurrentForm == null)
                return null;

            if (newForm == CurrentForm)
                return null;

            var previousForm = CurrentForm;
            CurrentForm = newForm;
            ComboScaler.AdvanceChain();

            return previousForm;
        }

        /// <summary>
        /// Force demorph — called when a form's health reaches 0.
        /// Ranger reverts to demorphed state.
        /// 
        /// IMPORTANT: The caller must also:
        /// - Call FormPool.PutBaseFormOnCooldown() to start the remorph timer
        /// - On permadeath, call FormPool.PermanentlyRemoveForm() and Ranger.RemoveFormInstance()
        /// </summary>
        public FormInstance Demorph()
        {
            var lostForm = CurrentForm;
            CurrentForm = null;
            MorphState = MorphState.Demorphed;
            return lostForm;
        }

        /// <summary>
        /// Re-morph after being demorphed. Only valid from Demorphed state.
        /// 
        /// IMPORTANT: The caller (combat system) MUST check FormPool.IsBaseFormAvailable()
        /// before calling this. Remorph does not validate cooldown state — the FormPool
        /// owns that responsibility.
        /// </summary>
        public bool Remorph()
        {
            if (MorphState != MorphState.Demorphed)
                return false;

            CurrentForm = BaseForm;
            MorphState = MorphState.Morphed;
            return true;
        }

        // === Form Instance Management ===

        /// <summary>
        /// Get or create a FormInstance for a given form definition.
        /// Instances are created on first use and persist for the Ranger.
        /// </summary>
        public FormInstance GetOrCreateFormInstance(FormData formData)
        {
            if (!_formInstances.ContainsKey(formData.Id))
            {
                _formInstances[formData.Id] = new FormInstance(formData);
            }
            return _formInstances[formData.Id];
        }

        /// <summary>
        /// Check if this Ranger has leveled a specific form.
        /// </summary>
        public bool HasFormInstance(string formId) => _formInstances.ContainsKey(formId);

        /// <summary>
        /// Remove a form instance permanently (permadeath).
        /// </summary>
        public bool RemoveFormInstance(string formId)
        {
            if (formId == BaseForm.Data.Id) return false; // Can't permanently lose base form
            return _formInstances.Remove(formId);
        }

        // === Health ===

        /// <summary>
        /// Get the currently active health pool based on morph state.
        /// </summary>
        public IHealthPool ActiveHealthPool => MorphState == MorphState.Morphed && CurrentForm != null
            ? CurrentForm.Health
            : UnmorphedHealth;

        /// <summary>
        /// Apply damage to the Ranger's currently active health pool.
        /// Returns a DamageEvent describing what happened (form death, demorph, etc.).
        /// </summary>
        public RangerDamageEvent TakeDamage(float amount)
        {
            var pool = ActiveHealthPool;
            float actualDamage = pool.TakeDamage(amount);

            var damageEvent = new RangerDamageEvent
            {
                DamageDealt = actualDamage,
                RangerId = Id
            };

            // Check for form death
            if (MorphState == MorphState.Morphed && !pool.IsAlive)
            {
                damageEvent.FormDied = true;
                damageEvent.DeadFormId = CurrentForm.Data.Id;
                Demorph();

                // Check if unmorphed Ranger immediately dies
                if (!UnmorphedHealth.IsAlive)
                {
                    damageEvent.RangerDiedUnmorphed = true;
                }
            }
            // Check for unmorphed death
            else if (MorphState != MorphState.Morphed && !pool.IsAlive)
            {
                damageEvent.RangerDiedUnmorphed = true;
            }

            return damageEvent;
        }

        // === Turn Management ===

        /// <summary>
        /// Reset per-turn state. Called at the start of this Ranger's turn.
        /// </summary>
        public void StartTurn()
        {
            ComboScaler.ResetChain();
        }
    }

    /// <summary>
    /// Event describing the result of damage dealt to a Ranger.
    /// Used by the game state system to trigger form death, demorph, mission loss.
    /// </summary>
    public class RangerDamageEvent
    {
        public string RangerId { get; set; }
        public float DamageDealt { get; set; }
        public bool FormDied { get; set; }
        public string DeadFormId { get; set; }
        public bool RangerDiedUnmorphed { get; set; }
    }
}
