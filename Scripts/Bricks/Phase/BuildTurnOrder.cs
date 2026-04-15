using System.Collections.Generic;
using System.Linq;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Bricks.Phase
{
    /// <summary>
    /// Builds a sorted list of TurnEntries from participants.
    /// Filters to units that can act, sorts by Speed descending.
    /// Pure — no state mutation.
    /// </summary>
    public static class BuildTurnOrder
    {
        public static List<TurnEntry> Execute(IEnumerable<ITurnParticipant> participants)
        {
            var sorted = participants
                .Where(p => p.CanAct)
                .OrderByDescending(p => p.Speed)
                .ToList();

            var entries = new List<TurnEntry>();
            for (int i = 0; i < sorted.Count; i++)
            {
                entries.Add(new TurnEntry
                {
                    Participant = sorted[i],
                    OrderIndex = i,
                    HasActed = false
                });
            }
            return entries;
        }
    }
}
