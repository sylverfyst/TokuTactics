using TokuTactics.Commands.Gimmick;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Declarative result of reactive gimmick resolution.
    /// The orchestrator reads this and calls enemy.OnGimmickActivated() if needed.
    /// </summary>
    public class ReactiveGimmickResult
    {
        /// <summary>The gimmick resolution (damage, status, displacement effects). Null if no gimmick fired.</summary>
        public GimmickResolution Resolution { get; set; }

        /// <summary>Whether the gimmick was activated and the enemy should be notified.</summary>
        public bool GimmickActivated { get; set; }

        /// <summary>ID of the enemy whose gimmick fired.</summary>
        public string EnemyId { get; set; }
    }
}
