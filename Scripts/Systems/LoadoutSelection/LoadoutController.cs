using System.Collections.Generic;
using System.Linq;
using TokuTactics.Bricks.Loadout;
using TokuTactics.Commands.Loadout;
using TokuTactics.Core.Events;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Core.Form;
using TokuTactics.Systems.FormManagement;

namespace TokuTactics.Systems.LoadoutSelection
{
    /// <summary>
    /// Orchestrator: Controls the loadout selection flow and first-morph gate.
    /// Delegates validation to bricks and submission to the ExecuteLoadoutSubmission command.
    /// Owns scouting intelligence state and handles event publishing.
    /// </summary>
    public class LoadoutController
    {
        private readonly FormPool _formPool;
        private readonly EventBus _eventBus;
        private readonly ScoutingIntelligence _scouting = new();

        public string TriggeringRangerId { get; private set; }
        public bool IsLoadoutComplete => _formPool.IsLoadoutLocked;
        public ScoutingIntelligence Scouting => _scouting;

        public LoadoutController(FormPool formPool, EventBus eventBus)
        {
            _formPool = formPool;
            _eventBus = eventBus;
        }

        // === Morph Gate ===

        public MorphRequestResult RequestMorph(Ranger ranger)
        {
            var validation = ValidateMorphRequest.Execute(ranger, _formPool.IsLoadoutLocked);

            if (validation == MorphRequestResult.NeedsLoadout)
            {
                TriggeringRangerId = ranger.Id;
                return MorphRequestResult.NeedsLoadout;
            }

            if (validation != MorphRequestResult.MorphComplete)
                return validation;

            // Perform morph — brick already validated, orchestrator owns the mutation
            ranger.Morph();

            _eventBus.Publish(new PlayMorphAnimationEvent
            {
                RangerId = ranger.Id,
                FormId = ranger.CurrentForm?.Data.Id,
                IsFirstTimeForThisForm = true
            });

            return MorphRequestResult.MorphComplete;
        }

        // === Loadout Submission ===

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

        public LoadoutResult SubmitLoadout(List<string> selectedFormIds)
        {
            // Build registered IDs for validation
            var poolStatus = _formPool.GetPoolStatus();
            var registeredIds = new HashSet<string>();
            foreach (var entry in poolStatus)
                registeredIds.Add(entry.FormData.Id);

            var result = ExecuteLoadoutSubmission.Execute(
                selectedFormIds, _formPool.IsLoadoutLocked, _formPool.Budget,
                registeredIds, _formPool.BaseFormId);

            if (result != LoadoutResult.Accepted)
                return result;

            // Equip, lock, publish — orchestrator owns all mutations
            foreach (var formId in selectedFormIds)
                _formPool.EquipForm(formId);

            _formPool.LockLoadout();

            _eventBus.Publish(new LoadoutLockedEvent
            {
                EquippedFormIds = selectedFormIds.ToArray()
            });
            _eventBus.Dispatch();

            return LoadoutResult.Accepted;
        }

        // === Scouting Intelligence ===

        public void RevealEnemyType(string enemyId, ElementalType type)
        {
            _scouting.RevealType(enemyId, type);
        }

        public void ObserveEnemy(string enemyId)
        {
            _scouting.Observe(enemyId);
        }
    }

    // === Scouting Intelligence ===

    public class ScoutingIntelligence
    {
        private readonly Dictionary<string, ElementalType> _revealedTypes = new();
        private readonly HashSet<string> _observedEnemyIds = new();

        public void RevealType(string enemyId, ElementalType type)
        {
            _revealedTypes[enemyId] = type;
            _observedEnemyIds.Add(enemyId);
        }

        public void Observe(string enemyId)
        {
            _observedEnemyIds.Add(enemyId);
        }

        public bool IsTypeRevealed(string enemyId) => _revealedTypes.ContainsKey(enemyId);

        public ElementalType? GetRevealedType(string enemyId) =>
            _revealedTypes.ContainsKey(enemyId) ? _revealedTypes[enemyId] : null;

        public Dictionary<string, ElementalType> GetRevealedTypes() => new(_revealedTypes);
        public HashSet<string> GetObservedEnemyIds() => new(_observedEnemyIds);

        public void Clear()
        {
            _revealedTypes.Clear();
            _observedEnemyIds.Clear();
        }
    }

    // === Loadout Screen Data ===

    public class LoadoutScreenData
    {
        public List<LoadoutFormOption> AvailableForms { get; set; }
        public int Budget { get; set; }
        public Dictionary<string, ElementalType> RevealedEnemyTypes { get; set; }
        public HashSet<string> ObservedEnemyIds { get; set; }
    }

    public class LoadoutFormOption
    {
        public FormData FormData { get; set; }
        public string TypeName { get; set; }
    }
}
