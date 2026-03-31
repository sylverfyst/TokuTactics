using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.Events;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.FormManagement;

namespace TokuTactics.Systems.LoadoutSelection
{
    /// <summary>
    /// Result of attempting to morph a Ranger.
    /// The game layer checks this to decide whether to open the loadout screen
    /// or proceed directly to the morph animation.
    /// </summary>
    public enum MorphRequestResult
    {
        /// <summary>Loadout not yet selected. Open the loadout screen.</summary>
        NeedsLoadout,

        /// <summary>Morph completed successfully.</summary>
        MorphComplete,

        /// <summary>Cannot morph — invalid state (already morphed, dead, etc.).</summary>
        Invalid
    }

    /// <summary>
    /// Result of submitting a loadout.
    /// </summary>
    public enum LoadoutResult
    {
        /// <summary>Loadout accepted and locked.</summary>
        Accepted,

        /// <summary>Too many forms selected — exceeds budget.</summary>
        OverBudget,

        /// <summary>One or more form IDs are not registered in the pool.</summary>
        InvalidForm,

        /// <summary>Loadout already locked — cannot change.</summary>
        AlreadyLocked,

        /// <summary>No forms were selected.</summary>
        Empty
    }

    /// <summary>
    /// Controls the loadout selection flow and first-morph gate.
    /// 
    /// When a Ranger attempts to morph for the first time in a mission,
    /// the controller intercepts and requires loadout selection. The game layer
    /// opens the loadout UI, the player picks forms within budget, and the
    /// controller validates and locks the loadout via FormPool.
    /// 
    /// After the loadout is locked, subsequent morph requests proceed directly.
    /// The loadout is locked once per mission — it cannot be retriggered even
    /// if all Rangers demorph and remorph.
    /// 
    /// The controller also tracks scouting intelligence — which enemy types
    /// have been revealed during the unmorphed phase. The loadout UI reads
    /// this to show partial enemy information.
    /// 
    /// For the vertical slice, the 6th Ranger's separate loadout is not implemented.
    /// </summary>
    public class LoadoutController
    {
        private readonly FormPool _formPool;
        private readonly EventBus _eventBus;
        private readonly ScoutingIntelligence _scouting = new();

        /// <summary>ID of the Ranger whose morph request triggered the loadout screen.</summary>
        public string TriggeringRangerId { get; private set; }

        /// <summary>Whether the loadout has been submitted and locked.</summary>
        public bool IsLoadoutComplete => _formPool.IsLoadoutLocked;

        /// <summary>Current scouting intelligence for the loadout UI.</summary>
        public ScoutingIntelligence Scouting => _scouting;

        public LoadoutController(FormPool formPool, EventBus eventBus)
        {
            _formPool = formPool;
            _eventBus = eventBus;
        }

        // === Morph Gate ===

        /// <summary>
        /// Attempt to morph a Ranger. If the loadout hasn't been selected yet,
        /// returns NeedsLoadout — the game layer should open the loadout UI.
        /// If the loadout is already locked, performs the morph and returns MorphComplete.
        /// </summary>
        public MorphRequestResult RequestMorph(Ranger ranger)
        {
            if (ranger.MorphState != MorphState.Unmorphed)
                return MorphRequestResult.Invalid;

            if (!ranger.IsAlive)
                return MorphRequestResult.Invalid;

            if (!_formPool.IsLoadoutLocked)
            {
                TriggeringRangerId = ranger.Id;
                return MorphRequestResult.NeedsLoadout;
            }

            // Loadout already locked — morph directly
            if (!ranger.Morph())
                return MorphRequestResult.Invalid;

            _eventBus.Publish(new PlayMorphAnimationEvent
            {
                RangerId = ranger.Id,
                FormId = ranger.CurrentForm?.Data.Id,
                IsFirstTimeForThisForm = true
            });

            return MorphRequestResult.MorphComplete;
        }

        // === Loadout Submission ===

        /// <summary>
        /// Get the data needed to display the loadout screen.
        /// Shows all registered non-base forms, the budget, and scouting intel.
        /// </summary>
        public LoadoutScreenData GetLoadoutScreenData()
        {
            var allForms = _formPool.GetPoolStatus()
                .Where(e => e.FormData.Id != _formPool.BaseFormId)
                .Select(e => new LoadoutFormOption
                {
                    FormData = e.FormData,
                    TypeName = e.FormData.Type.ToString()
                })
                .ToList();

            return new LoadoutScreenData
            {
                AvailableForms = allForms,
                Budget = _formPool.Budget,
                RevealedEnemyTypes = _scouting.GetRevealedTypes(),
                ObservedEnemyIds = _scouting.GetObservedEnemyIds()
            };
        }

        /// <summary>
        /// Submit the player's form selections. Validates against budget
        /// and registered forms, then equips and locks the loadout.
        /// 
        /// After a successful submission, call RequestMorph again on the
        /// triggering Ranger to complete their morph.
        /// </summary>
        public LoadoutResult SubmitLoadout(List<string> selectedFormIds)
        {
            if (_formPool.IsLoadoutLocked)
                return LoadoutResult.AlreadyLocked;

            if (selectedFormIds == null || selectedFormIds.Count == 0)
                return LoadoutResult.Empty;

            if (selectedFormIds.Count > _formPool.Budget)
                return LoadoutResult.OverBudget;

            // Validate all form IDs exist in the pool
            var poolStatus = _formPool.GetPoolStatus();
            var registeredIds = new HashSet<string>(
                poolStatus.Select(e => e.FormData.Id));

            foreach (var formId in selectedFormIds)
            {
                if (formId == _formPool.BaseFormId) continue; // Skip base form
                if (!registeredIds.Contains(formId))
                    return LoadoutResult.InvalidForm;
            }

            // Equip selected forms
            foreach (var formId in selectedFormIds)
            {
                _formPool.EquipForm(formId);
            }

            // Lock the loadout — no changes until next mission
            _formPool.LockLoadout();

            _eventBus.Publish(new LoadoutLockedEvent
            {
                EquippedFormIds = selectedFormIds.ToArray()
            });
            _eventBus.Dispatch();

            return LoadoutResult.Accepted;
        }

        // === Scouting Intelligence ===

        /// <summary>
        /// Record that an enemy's type was revealed during scouting.
        /// Called when: enemy uses a type-based attack, personal ability
        /// reveals strength/weakness, or any interaction that surfaces type info.
        /// </summary>
        public void RevealEnemyType(string enemyId, ElementalType type)
        {
            _scouting.RevealType(enemyId, type);
        }

        /// <summary>
        /// Record that an enemy was observed (interacted with but type not necessarily known).
        /// Observed enemies appear on the loadout screen as "type unknown" entries.
        /// </summary>
        public void ObserveEnemy(string enemyId)
        {
            _scouting.Observe(enemyId);
        }
    }

    // === Scouting Intelligence ===

    /// <summary>
    /// Tracks what the player has learned about enemies during the unmorphed phase.
    /// The loadout screen reads this to show partial enemy information.
    /// 
    /// Two levels of knowledge:
    /// - Observed: enemy was interacted with, but type is unknown
    /// - Revealed: enemy's elemental type is known
    /// 
    /// Intelligence persists even if the enemy is killed — information gathered is kept.
    /// </summary>
    public class ScoutingIntelligence
    {
        private readonly Dictionary<string, ElementalType> _revealedTypes = new();
        private readonly HashSet<string> _observedEnemyIds = new();

        /// <summary>Record that an enemy's type was revealed.</summary>
        public void RevealType(string enemyId, ElementalType type)
        {
            _revealedTypes[enemyId] = type;
            _observedEnemyIds.Add(enemyId);
        }

        /// <summary>Record that an enemy was observed (type may or may not be known).</summary>
        public void Observe(string enemyId)
        {
            _observedEnemyIds.Add(enemyId);
        }

        /// <summary>Check if an enemy's type has been revealed.</summary>
        public bool IsTypeRevealed(string enemyId) => _revealedTypes.ContainsKey(enemyId);

        /// <summary>Get the revealed type of an enemy, or null if unknown.</summary>
        public ElementalType? GetRevealedType(string enemyId) =>
            _revealedTypes.ContainsKey(enemyId) ? _revealedTypes[enemyId] : null;

        /// <summary>Get all revealed enemy types for the loadout screen.</summary>
        public Dictionary<string, ElementalType> GetRevealedTypes() => new(_revealedTypes);

        /// <summary>Get all observed enemy IDs.</summary>
        public HashSet<string> GetObservedEnemyIds() => new(_observedEnemyIds);

        /// <summary>Reset for a new mission.</summary>
        public void Clear()
        {
            _revealedTypes.Clear();
            _observedEnemyIds.Clear();
        }
    }

    // === Loadout Screen Data ===

    /// <summary>
    /// Snapshot of everything the loadout UI needs to display.
    /// Built by LoadoutController.GetLoadoutScreenData().
    /// </summary>
    public class LoadoutScreenData
    {
        /// <summary>All non-base forms available for selection.</summary>
        public List<LoadoutFormOption> AvailableForms { get; set; }

        /// <summary>Maximum number of forms the player can select.</summary>
        public int Budget { get; set; }

        /// <summary>Enemy types revealed during scouting. Key = enemy instance ID.</summary>
        public Dictionary<string, ElementalType> RevealedEnemyTypes { get; set; }

        /// <summary>All enemy IDs that were observed (interacted with).</summary>
        public HashSet<string> ObservedEnemyIds { get; set; }
    }

    /// <summary>
    /// A single form option on the loadout screen.
    /// </summary>
    public class LoadoutFormOption
    {
        public FormData FormData { get; set; }
        public string TypeName { get; set; }
    }
}
