namespace TokuTactics.Commands.Loadout
{
    /// <summary>
    /// Result of attempting to morph a Ranger.
    /// Lives in the Commands layer so bricks and commands can produce it
    /// without referencing the orchestrator namespace.
    /// </summary>
    public enum MorphRequestResult
    {
        /// <summary>Loadout not yet selected. Open the loadout screen.</summary>
        NeedsLoadout,

        /// <summary>Morph completed successfully.</summary>
        MorphComplete,

        /// <summary>Cannot morph — invalid state (already morphed, dead, etc.).</summary>
        Invalid
    }

    /// <summary>
    /// Result of submitting a loadout.
    /// </summary>
    public enum LoadoutResult
    {
        /// <summary>Loadout accepted and locked.</summary>
        Accepted,

        /// <summary>Too many forms selected — exceeds budget.</summary>
        OverBudget,

        /// <summary>One or more form IDs are not registered in the pool.</summary>
        InvalidForm,

        /// <summary>Loadout already locked — cannot change.</summary>
        AlreadyLocked,

        /// <summary>No forms were selected.</summary>
        Empty
    }
}
