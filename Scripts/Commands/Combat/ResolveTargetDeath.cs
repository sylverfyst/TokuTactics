using System;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Command: Determines what death-related outcomes occurred for a combat target.
    /// Returns a declarative result — does NOT mutate state (no Demorph calls, no event publishing).
    /// The orchestrator reads the result and performs mutations + publishes events.
    /// </summary>
    public static class ResolveTargetDeath
    {
        public static TargetDeathResult Execute(ICombatTarget target)
        {
            if (target is Enemy enemy)
                return CheckEnemyDeath(enemy);

            if (target is Ranger ranger)
                return CheckRangerDeath(ranger);

            return TargetDeathResult.NoDeath();
        }

        private static TargetDeathResult CheckEnemyDeath(Enemy enemy)
        {
            if (enemy.IsAlive)
                return TargetDeathResult.NoDeath();

            return new TargetDeathResult
            {
                TargetDied = true,
                TargetId = enemy.Id,
                EnemyTypeId = enemy.Data.Id,
                EnemyType = enemy.Data.Type ?? ElementalType.Normal
            };
        }

        private static TargetDeathResult CheckRangerDeath(Ranger ranger)
        {
            // Check form death (morphed, form health dead)
            if (ranger.MorphState == MorphState.Morphed
                && ranger.CurrentForm != null
                && !ranger.CurrentForm.Health.IsAlive)
            {
                return new TargetDeathResult
                {
                    FormDied = true,
                    LostFormId = ranger.CurrentForm.Data.Id,
                    TargetId = ranger.Id
                };
            }

            // Check unmorphed death (mission loss)
            if (ranger.MorphState != MorphState.Morphed
                && !ranger.UnmorphedHealth.IsAlive)
            {
                return new TargetDeathResult
                {
                    TargetDied = true,
                    MissionLost = true,
                    TargetId = ranger.Id
                };
            }

            return TargetDeathResult.NoDeath();
        }
    }
}
