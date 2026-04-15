namespace TokuTactics.Core.ActionEconomy
{
    /// <summary>
    /// The two main phases of a turn. Player acts first, then enemies.
    /// </summary>
    public enum TurnPhase
    {
        Player,
        Enemy
    }

    /// <summary>
    /// A single entry in the turn order.
    /// </summary>
    public class TurnEntry
    {
        public ITurnParticipant Participant { get; set; }
        public int OrderIndex { get; set; }
        public bool HasActed { get; set; }
    }

    /// <summary>
    /// Interface for anything that participates in turn order.
    /// </summary>
    public interface ITurnParticipant
    {
        string ParticipantId { get; }
        float Speed { get; }
        bool CanAct { get; }
    }
}
