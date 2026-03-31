using System.Collections.Generic;
using System.Linq;

namespace TokuTactics.Systems.ActionEconomy
{
    /// <summary>
    /// The two main phases of a turn. Player acts first, then enemies.
    /// Within each phase, units act in SPD order (highest first).
    /// </summary>
    public enum TurnPhase
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Manages turn ordering within a phase.
    /// Units are sorted by SPD. Higher SPD goes first, which is not always
    /// advantageous (less information about ally positions for assists).
    /// </summary>
    public class TurnOrder
    {
        private readonly List<TurnEntry> _entries = new();
        private int _currentIndex = -1;

        /// <summary>All entries in this phase's turn order.</summary>
        public IReadOnlyList<TurnEntry> Entries => _entries;

        /// <summary>The currently acting unit.</summary>
        public TurnEntry Current => _currentIndex >= 0 && _currentIndex < _entries.Count
            ? _entries[_currentIndex]
            : null;

        /// <summary>Whether all units in this phase have acted.</summary>
        public bool IsPhaseComplete => _entries.Count == 0 ||
            _currentIndex >= _entries.Count ||
            _entries.All(e => e.HasActed);

        /// <summary>
        /// Build the turn order from a set of units sorted by SPD descending.
        /// </summary>
        public void Build(IEnumerable<ITurnParticipant> participants)
        {
            _entries.Clear();
            _currentIndex = -1;

            var sorted = participants
                .Where(p => p.CanAct)
                .OrderByDescending(p => p.Speed)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                _entries.Add(new TurnEntry
                {
                    Participant = sorted[i],
                    OrderIndex = i,
                    HasActed = false
                });
            }
        }

        /// <summary>
        /// Advance to the next unit. Returns the next entry or null if phase is complete.
        /// </summary>
        public TurnEntry Advance()
        {
            _currentIndex++;
            if (_currentIndex >= _entries.Count)
                return null;

            return _entries[_currentIndex];
        }

        /// <summary>
        /// Reset for a new phase.
        /// </summary>
        public void Reset()
        {
            _currentIndex = -1;
            foreach (var entry in _entries)
                entry.HasActed = false;
        }
    }

    /// <summary>
    /// A single entry in the turn order.
    /// </summary>
    public class TurnEntry
    {
        public ITurnParticipant Participant { get; set; }
        public int OrderIndex { get; set; }
        public bool HasActed { get; set; }
    }

    /// <summary>
    /// Interface for anything that participates in turn order:
    /// Rangers, enemies, potentially the Megazord.
    /// </summary>
    public interface ITurnParticipant
    {
        string ParticipantId { get; }
        float Speed { get; }
        bool CanAct { get; }
    }
}
