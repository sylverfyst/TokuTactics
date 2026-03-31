using System.Collections.Generic;

namespace TokuTactics.Systems.AI
{
    /// <summary>
    /// Result of processing a behavior tree node.
    /// </summary>
    public enum NodeStatus
    {
        Success,
        Failure,
        Running
    }

    /// <summary>
    /// Base interface for all behavior tree nodes.
    /// The entire AI system is built from composing these.
    /// </summary>
    public interface IBehaviorNode
    {
        string NodeId { get; }
        NodeStatus Process(AIContext context);
        void Reset();
    }

    /// <summary>
    /// Context passed through the behavior tree.
    /// Provides read-only access to observable game state
    /// (the AI never reads hidden systems like proclivities).
    /// </summary>
    public class AIContext
    {
        /// <summary>The entity making decisions.</summary>
        public object Self { get; set; }

        /// <summary>Observable game state query interface.</summary>
        public IGameStateQuery GameState { get; set; }

        /// <summary>Chosen action output — set by leaf nodes.</summary>
        public AIDecision Decision { get; set; }
    }

    /// <summary>
    /// Read-only query interface for game state.
    /// The AI sees ONLY what's observable: positions, health, types, forms.
    /// Never proclivities, never hidden multipliers.
    /// </summary>
    public interface IGameStateQuery
    {
        /// <summary>Get all enemies visible to this entity.</summary>
        List<EntityInfo> GetVisibleEnemies(object self);

        /// <summary>Get all allies of this entity.</summary>
        List<EntityInfo> GetAllies(object self);

        /// <summary>Check if a position is adjacent to a specific entity.</summary>
        bool IsAdjacent(object entityA, object entityB);

        /// <summary>Get the terrain type at a position.</summary>
        string GetTerrainAt(int x, int y);

        /// <summary>Get the distance between two entities.</summary>
        int GetDistance(object entityA, object entityB);

        /// <summary>Check if an entity is unmorphed (and therefore vulnerable).</summary>
        bool IsUnmorphed(object entity);

        /// <summary>Get the current form type of an entity.</summary>
        string GetCurrentFormId(object entity);

        /// <summary>Get the health percentage of an entity.</summary>
        float GetHealthPercentage(object entity);
    }

    /// <summary>
    /// Observable info about an entity on the battlefield.
    /// </summary>
    public class EntityInfo
    {
        public object Entity { get; set; }
        public string Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public float HealthPercentage { get; set; }
        public string CurrentFormId { get; set; }
        public bool IsUnmorphed { get; set; }
        public string TypeId { get; set; }
    }

    /// <summary>
    /// The output of a behavior tree — what the AI decided to do.
    /// </summary>
    public class AIDecision
    {
        public AIActionType ActionType { get; set; }
        public object Target { get; set; }
        public (int X, int Y)? MoveTarget { get; set; }
        public string FormSwitchId { get; set; }
        public float Priority { get; set; }
    }

    public enum AIActionType
    {
        Move,
        BasicAttack,
        WeaponAttack,
        FormSwitch,
        UseAbility,
        Wait
    }
}
