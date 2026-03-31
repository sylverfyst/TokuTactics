using System.Collections.Generic;
using System.Linq;

namespace TokuTactics.Core.StatusEffect
{
    /// <summary>
    /// Manages all active status effects on a single entity (Ranger, enemy, etc.).
    /// 
    /// Process() returns a list of EffectOutputs for the combat system to apply.
    /// The tracker never directly modifies game state — it's purely declarative.
    /// </summary>
    public class StatusEffectTracker
    {
        private readonly List<StatusEffectInstance> _activeEffects = new();

        public IReadOnlyList<StatusEffectInstance> ActiveEffects => _activeEffects;

        /// <summary>
        /// Apply a new status effect to this entity.
        /// </summary>
        public void Apply(StatusEffectInstance effect)
        {
            _activeEffects.Add(effect);
        }

        /// <summary>
        /// Process all active effects that respond to the given context.
        /// Returns all EffectOutputs produced. The combat system consumes these
        /// and applies damage, healing, stat modifiers, etc.
        /// </summary>
        public List<EffectOutput> Process(EffectContext context)
        {
            var outputs = new List<EffectOutput>();

            foreach (var effect in _activeEffects)
            {
                var output = effect.Process(context);
                if (output != null && output.HasEffect)
                {
                    outputs.Add(output);
                }
            }

            return outputs;
        }

        /// <summary>
        /// Tick all durations and remove expired effects.
        /// Returns removal outputs for effects that expired (to undo stat modifiers, etc.).
        /// Call once per turn for this entity.
        /// </summary>
        public List<EffectOutput> TickAndClean(EffectContext context)
        {
            var removalOutputs = new List<EffectOutput>();

            foreach (var effect in _activeEffects)
            {
                effect.Tick();
            }

            var expired = _activeEffects.Where(e => e.IsExpired).ToList();
            foreach (var effect in expired)
            {
                var removalOutput = effect.GetRemovalOutput(context);
                if (removalOutput != null && removalOutput.HasEffect)
                {
                    removalOutputs.Add(removalOutput);
                }
                _activeEffects.Remove(effect);
            }

            return removalOutputs;
        }

        /// <summary>
        /// Remove a specific effect by ID. Returns the removal output.
        /// </summary>
        public EffectOutput Remove(string effectId, EffectContext context)
        {
            var effect = _activeEffects.FirstOrDefault(e => e.Id == effectId);
            if (effect == null) return null;

            var removalOutput = effect.GetRemovalOutput(context);
            _activeEffects.Remove(effect);
            return removalOutput;
        }

        /// <summary>
        /// Remove all effects. Returns all removal outputs.
        /// Used on demorph or form switch.
        /// </summary>
        public List<EffectOutput> ClearAll(EffectContext context)
        {
            var removalOutputs = new List<EffectOutput>();

            foreach (var effect in _activeEffects)
            {
                var output = effect.GetRemovalOutput(context);
                if (output != null && output.HasEffect)
                {
                    removalOutputs.Add(output);
                }
            }

            _activeEffects.Clear();
            return removalOutputs;
        }

        /// <summary>
        /// Check if a specific effect type is active.
        /// </summary>
        public bool HasEffect(string effectId) =>
            _activeEffects.Any(e => e.Id == effectId);
    }
}
