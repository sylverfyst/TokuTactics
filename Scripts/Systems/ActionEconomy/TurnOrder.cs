using System.Collections.Generic;
using System.Linq;
using TokuTactics.Bricks.Phase;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Systems.ActionEconomy
{
    /// <summary>
    /// Orchestrator: Manages turn ordering within a phase.
    /// </summary>
    public class TurnOrder
    {
        private readonly List<TurnEntry> _entries = new();
        private int _currentIndex = -1;

        public IReadOnlyList<TurnEntry> Entries => _entries;

        public TurnEntry Current => _currentIndex >= 0 && _currentIndex < _entries.Count
            ? _entries[_currentIndex]
            : null;

        public bool IsPhaseComplete => _entries.Count == 0 ||
            _currentIndex >= _entries.Count ||
            _entries.All(e => e.HasActed);

        public void Build(IEnumerable<ITurnParticipant> participants)
        {
            _entries.Clear();
            _currentIndex = -1;
            _entries.AddRange(BuildTurnOrder.Execute(participants));
        }

        public TurnEntry Advance()
        {
            _currentIndex++;
            if (_currentIndex >= _entries.Count)
                return null;
            return _entries[_currentIndex];
        }

        public void Reset()
        {
            _currentIndex = -1;
            foreach (var entry in _entries)
                entry.HasActed = false;
        }
    }
}
