using TokuTactics.Core.Types;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Declarative result of checking a target's death state after combat.
    /// Does NOT perform mutations — the orchestrator uses this to call Demorph, publish events, etc.
    /// </summary>
    public class TargetDeathResult
    {
        /// <summary>Whether the target died (enemy killed or unmorphed ranger killed).</summary>
        public bool TargetDied { get; set; }

        /// <summary>Whether a ranger's form was destroyed (triggers demorph).</summary>
        public bool FormDied { get; set; }

        /// <summary>ID of the form that was destroyed. Null if no form death.</summary>
        public string LostFormId { get; set; }

        /// <summary>Whether an unmorphed ranger died (mission loss).</summary>
        public bool MissionLost { get; set; }

        /// <summary>ID of the dead enemy's data definition (for event publishing). Null if target was a ranger.</summary>
        public string EnemyTypeId { get; set; }

        /// <summary>Type of the dead enemy (for event publishing). Null if target was a ranger.</summary>
        public ElementalType? EnemyType { get; set; }

        /// <summary>ID of the target that died (enemy or ranger). Null if no death.</summary>
        public string TargetId { get; set; }

        public static TargetDeathResult NoDeath()
        {
            return new TargetDeathResult();
        }
    }
}
