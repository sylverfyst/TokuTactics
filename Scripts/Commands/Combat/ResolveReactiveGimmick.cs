using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Combat;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Grid;
using TokuTactics.Entities.Enemies;
using GimmickResolutionNS = TokuTactics.Systems.GimmickResolution;

namespace TokuTactics.Commands.Combat
{
    /// <summary>
    /// Command: Checks if a reactive gimmick should fire after a hit, and resolves it.
    /// Composes ValidateReactiveGimmick brick and delegates to GimmickResolver (injected).
    /// Returns null if no gimmick fires.
    /// </summary>
    public static class ResolveReactiveGimmick
    {
        public static GimmickResolutionNS.GimmickResolution Execute(
            ICombatTarget target,
            bool wasDodged,
            BattleGrid grid,
            GimmickResolutionNS.GimmickResolver gimmickResolver,
            HashSet<string> rangerIds,
            Func<ICombatTarget, bool, bool> validateGimmick = null)
        {
            validateGimmick ??= ValidateReactiveGimmick.Execute;

            if (!validateGimmick(target, wasDodged))
                return null;

            var enemy = (Enemy)target;

            var ownerPos = grid.GetUnitPosition(enemy.Id);
            if (!ownerPos.HasValue) return null;

            var context = enemy.BuildGimmickContext();
            context.WasJustHit = true;

            if (!enemy.ShouldGimmickActivate(context)) return null;

            var output = enemy.GetGimmickOutput(context);
            if (output == null || !output.HasEffect) return null;

            var resolution = gimmickResolver.Resolve(
                ownerPos.Value, output,
                enemy.Data.Gimmick.Behavior.Range,
                rangerIds);

            if (resolution.HasEffects)
                enemy.OnGimmickActivated();

            return resolution;
        }
    }
}
