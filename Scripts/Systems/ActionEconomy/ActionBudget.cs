namespace TokuTactics.Systems.ActionEconomy
{
    /// <summary>
    /// Type: Pure data shape for a unit's per-turn action economy.
    /// All operations on this type are performed by bricks.
    ///
    /// Normal turn: move once, act once.
    /// Form switch: resets both movement and action (free).
    /// Morph: consumes the action (turn ends).
    /// Tier 4 bond refresh: grants one additional action reset per round.
    /// </summary>
    public class ActionBudget
    {
        /// <summary>Whether the unit can still move this action cycle.</summary>
        public bool CanMove { get; set; }

        /// <summary>Whether the unit can still act (attack/ability) this action cycle.</summary>
        public bool CanAct { get; set; }

        /// <summary>Whether the unit can form switch (has at least one form off cooldown).</summary>
        public bool CanFormSwitch { get; set; }

        /// <summary>Whether this unit has used their tier 4 bond refresh this round.</summary>
        public bool HasUsedBondRefresh { get; set; }

        /// <summary>Whether this unit has received a tier 4 bond refresh this round.</summary>
        public bool HasReceivedBondRefresh { get; set; }

        /// <summary>Whether the unit's turn is completely finished.</summary>
        public bool IsTurnComplete => !CanMove && !CanAct && !CanFormSwitch;
    }
}
