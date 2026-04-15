using TokuTactics.Systems.CombatResolution;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Result of an attack attempt.
    /// </summary>
    public class AttackResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; }
        public CombatResult CombatResult { get; set; }

        public static AttackResult CreateSuccess(CombatResult combatResult)
        {
            return new AttackResult
            {
                Success = true,
                CombatResult = combatResult
            };
        }

        public static AttackResult CreateFailure(string reason)
        {
            return new AttackResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }
}
