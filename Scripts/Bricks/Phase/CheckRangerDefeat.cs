using System.Collections.Generic;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Bricks.Phase
{
    /// <summary>
    /// Returns the ID of the first dead ranger, or null if all are alive.
    /// Used to check the mission loss condition (any unmorphed ranger killed).
    /// </summary>
    public static class CheckRangerDefeat
    {
        public static string Execute(IReadOnlyList<Ranger> rangers)
        {
            for (int i = 0; i < rangers.Count; i++)
            {
                if (!rangers[i].IsAlive)
                    return rangers[i].Id;
            }
            return null;
        }
    }
}
