namespace TokuTactics.Core.Events
{
    /// <summary>
    /// Base interface for all game events. Every event in the system implements this.
    /// Events are pure data — they describe what happened, not what should happen.
    /// 
    /// The event bus queues events during a phase and dispatches them in order
    /// after the phase completes. No immediate dispatch, no recursive cascades.
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>
        /// Priority tier determines dispatch order within a cycle.
        /// Game state events process before UI events, which process before audio events.
        /// Within the same tier, events dispatch in the order they were queued.
        /// </summary>
        EventPriority Priority { get; }
    }

    /// <summary>
    /// Priority tiers for event dispatch ordering.
    /// Lower numeric value = higher priority = dispatched first.
    /// 
    /// This is NOT about importance — it's about ensuring game state is correct
    /// before presentation layers read it.
    /// 
    /// GameState: Form deaths, demorphs, cooldown changes, stat modifications.
    ///   These must resolve first so all other systems read correct state.
    /// 
    /// Gameplay: AI recalculations, bond updates, aggression threshold changes.
    ///   These depend on correct game state and produce no further state changes.
    /// 
    /// Presentation: UI updates, animations, camera movements.
    ///   These read final state and trigger visual responses.
    /// 
    /// Audio: Sound effects, music changes.
    ///   These fire last and never affect game state.
    /// </summary>
    public enum EventPriority
    {
        GameState = 0,
        Gameplay = 1,
        Presentation = 2,
        Audio = 3
    }
}
