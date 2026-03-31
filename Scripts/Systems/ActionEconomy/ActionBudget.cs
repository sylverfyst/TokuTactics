namespace TokuTactics.Systems.ActionEconomy
{
    /// <summary>
    /// Tracks what a unit can do on their current turn.
    /// 
    /// Normal turn: move once, act once.
    /// Form switch: resets both movement and action (free).
    /// Morph: consumes the action (turn ends).
    /// Tier 4 bond refresh: grants one additional action reset per round.
    /// 
    /// This is the system that makes form switching feel like "tagging in a new fighter."
    /// </summary>
    public class ActionBudget
    {
        /// <summary>Whether the unit can still move this action cycle.</summary>
        public bool CanMove { get; private set; }

        /// <summary>Whether the unit can still act (attack/ability) this action cycle.</summary>
        public bool CanAct { get; private set; }

        /// <summary>Whether the unit can form switch (has at least one form off cooldown).</summary>
        public bool CanFormSwitch { get; set; }

        /// <summary>Whether this unit has used their tier 4 bond refresh this round.</summary>
        public bool HasUsedBondRefresh { get; private set; }

        /// <summary>Whether this unit has received a tier 4 bond refresh this round.</summary>
        public bool HasReceivedBondRefresh { get; private set; }

        /// <summary>Whether the unit's turn is completely finished.</summary>
        public bool IsTurnComplete => !CanMove && !CanAct && !CanFormSwitch;

        /// <summary>
        /// Initialize at the start of a unit's turn.
        /// </summary>
        public void StartTurn()
        {
            CanMove = true;
            CanAct = true;
            CanFormSwitch = true; // Caller sets to false if no forms available
            HasUsedBondRefresh = false;
            HasReceivedBondRefresh = false;
        }

        /// <summary>
        /// Consume movement.
        /// </summary>
        public bool ConsumeMove()
        {
            if (!CanMove) return false;
            CanMove = false;
            return true;
        }

        /// <summary>
        /// Consume the action (basic attack, weapon attack, personal ability).
        /// </summary>
        public bool ConsumeAction()
        {
            if (!CanAct) return false;
            CanAct = false;
            return true;
        }

        /// <summary>
        /// Consume the action for morphing. Ends the turn entirely.
        /// </summary>
        public bool ConsumeMorphAction()
        {
            if (!CanAct) return false;
            CanMove = false;
            CanAct = false;
            CanFormSwitch = false;
            return true;
        }

        /// <summary>
        /// Reset action economy after a form switch.
        /// This is what makes chaining possible — each switch grants a new move + action.
        /// </summary>
        public void ResetFromFormSwitch()
        {
            CanMove = true;
            CanAct = true;
            // CanFormSwitch stays as-is — caller updates based on available forms
        }

        /// <summary>
        /// Apply a tier 4 bond refresh. Grants one additional action.
        /// Can only be received once per round per unit.
        /// </summary>
        public bool ApplyBondRefresh()
        {
            if (HasReceivedBondRefresh) return false;

            HasReceivedBondRefresh = true;
            CanAct = true;
            CanMove = true;
            return true;
        }

        /// <summary>
        /// Mark that this unit has given a tier 4 bond refresh this round.
        /// Can only give once per round.
        /// </summary>
        public bool GiveBondRefresh()
        {
            if (HasUsedBondRefresh) return false;
            HasUsedBondRefresh = true;
            return true;
        }

        /// <summary>
        /// Force end the turn (e.g., after morphing, or player choice to wait).
        /// </summary>
        public void EndTurn()
        {
            CanMove = false;
            CanAct = false;
            CanFormSwitch = false;
        }
    }
}
