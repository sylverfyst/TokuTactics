using TokuTactics.Systems.FormManagement;
using System;
using System.Collections.Generic;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;
using TokuTactics.Core.Form;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Command: Processes all round-start effects and checks win/loss.
    /// Composes ProcessRoundStatusEffects and ResolveWinLoss commands.
    /// Handles: FormPool tick, status effects, combo resets, win/loss check.
    /// </summary>
    public static class ExecuteRoundStart
    {
        public static RoundStartResult Execute(
            int currentRound,
            IReadOnlyList<Ranger> rangers,
            IReadOnlyList<Enemy> enemies,
            IReadOnlyCollection<string> defeatTargetIds,
            FormPool formPool,
            Func<IReadOnlyList<Ranger>, IReadOnlyList<Enemy>, StatusEffectRoundResult> processStatusEffects = null,
            Func<IReadOnlyList<Ranger>, IReadOnlyList<Enemy>, IReadOnlyCollection<string>, WinLossResult> resolveWinLoss = null)
        {
            processStatusEffects ??= (r, e) => ProcessRoundStatusEffects.Execute(r, e);
            resolveWinLoss ??= (r, e, t) => ResolveWinLoss.Execute(r, e, t);

            int newRound = currentRound + 1;

            // Tick form cooldowns and passive health regen
            formPool.ProcessTurn();

            // Process status effects on all units
            var effectResult = processStatusEffects(rangers, enemies);

            // Reset combo chains for all alive Rangers
            for (int i = 0; i < rangers.Count; i++)
            {
                if (rangers[i].IsAlive)
                    rangers[i].ComboScaler.ResetChain();
            }

            // Check if status effects caused a win or loss
            var winLoss = resolveWinLoss(rangers, enemies, defeatTargetIds);

            return new RoundStartResult
            {
                NewRoundNumber = newRound,
                MissionEnded = winLoss.Ended,
                EndState = winLoss.EndState,
                FallenRangerId = winLoss.FallenRangerId,
                DemorphEvents = effectResult.DemorphEvents,
                AggressionEvents = effectResult.AggressionEvents
            };
        }
    }
}
