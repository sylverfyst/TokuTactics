namespace TokuTactics.Entities.Rangers
{
    /// <summary>
    /// The three states a Ranger can be in during combat.
    /// Determines which actions are available and which systems are active.
    /// </summary>
    public enum MorphState
    {
        /// <summary>
        /// Pre-morph state. Basic attack + personal ability only.
        /// No forms, no weapons, no switching. Vulnerable.
        /// </summary>
        Unmorphed,

        /// <summary>
        /// Active combat state. Form switching, weapon attacks, full action economy.
        /// Always enters via base form first.
        /// </summary>
        Morphed,

        /// <summary>
        /// Forced unmorphed after form death. Base form on cooldown.
        /// Can use personal abilities. Can re-morph when base form cooldown expires.
        /// </summary>
        Demorphed
    }
}
