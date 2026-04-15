using TokuTactics.Core.ActionEconomy;
using TokuTactics.Core.Assist;
using TokuTactics.Core.Stats;
using TokuTactics.Entities.Rangers;

namespace TokuTactics.Bricks.Assist
{
    /// <summary>
    /// Maps a Ranger's current state to an AssistCandidateState for the assist system.
    /// Pure transformation — no mutation, no side effects.
    /// </summary>
    public static class MapRangerToAssistState
    {
        public static AssistCandidateState Execute(Ranger ranger, ActionBudget budget)
        {
            var state = new AssistCandidateState
            {
                IsMorphed = ranger.MorphState == MorphState.Morphed,
                CurrentFormId = ranger.CurrentForm?.Data.Id,
                BaseFormId = ranger.BaseForm.Data.Id,
                IsInBaseForm = ranger.CurrentForm == ranger.BaseForm,
                WeaponBasePower = ranger.CurrentForm?.Data.WeaponA?.BasePower ?? 0,
                Str = ranger.Stats.Get(StatType.STR),
                Cha = ranger.Stats.Get(StatType.CHA),
                AssisterDualType = ranger.DualType
            };

            if (budget != null)
            {
                state.HasUsedBondRefresh = budget.HasUsedBondRefresh;
                state.HasReceivedBondRefresh = budget.HasReceivedBondRefresh;
            }

            return state;
        }
    }
}
