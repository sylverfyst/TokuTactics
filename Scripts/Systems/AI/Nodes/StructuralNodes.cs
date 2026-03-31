using System.Collections.Generic;
using System.Linq;

namespace TokuTactics.Systems.AI.Nodes
{
    /// <summary>
    /// Runs children in order. Succeeds if ALL children succeed.
    /// Fails on the first failure. Used for multi-step plans: "move THEN attack."
    /// </summary>
    public class SequenceNode : IBehaviorNode
    {
        public string NodeId { get; }
        private readonly List<IBehaviorNode> _children;
        private int _currentChild;

        public SequenceNode(string nodeId, params IBehaviorNode[] children)
        {
            NodeId = nodeId;
            _children = new List<IBehaviorNode>(children);
            _currentChild = 0;
        }

        public NodeStatus Process(AIContext context)
        {
            while (_currentChild < _children.Count)
            {
                var status = _children[_currentChild].Process(context);

                if (status == NodeStatus.Failure)
                    return NodeStatus.Failure;

                if (status == NodeStatus.Running)
                    return NodeStatus.Running;

                _currentChild++;
            }
            return NodeStatus.Success;
        }

        public void Reset()
        {
            _currentChild = 0;
            foreach (var child in _children)
                child.Reset();
        }
    }

    /// <summary>
    /// Runs children in order. Succeeds on the FIRST success.
    /// Falls through on failures. Used for priority lists: "try this, else try that."
    /// </summary>
    public class SelectorNode : IBehaviorNode
    {
        public string NodeId { get; }
        private readonly List<IBehaviorNode> _children;

        public SelectorNode(string nodeId, params IBehaviorNode[] children)
        {
            NodeId = nodeId;
            _children = new List<IBehaviorNode>(children);
        }

        public NodeStatus Process(AIContext context)
        {
            foreach (var child in _children)
            {
                var status = child.Process(context);
                if (status != NodeStatus.Failure)
                    return status;
            }
            return NodeStatus.Failure;
        }

        public void Reset()
        {
            foreach (var child in _children)
                child.Reset();
        }
    }

    /// <summary>
    /// Checks a condition and succeeds/fails based on the result.
    /// Used for branching: "if unmorphed enemy nearby, do X."
    /// The condition function reads only from the AIContext (observable state).
    /// </summary>
    public class ConditionNode : IBehaviorNode
    {
        public string NodeId { get; }
        private readonly System.Func<AIContext, bool> _condition;

        public ConditionNode(string nodeId, System.Func<AIContext, bool> condition)
        {
            NodeId = nodeId;
            _condition = condition;
        }

        public NodeStatus Process(AIContext context)
        {
            return _condition(context) ? NodeStatus.Success : NodeStatus.Failure;
        }

        public void Reset() { }
    }

    /// <summary>
    /// Scores all children using utility functions and executes the highest-scoring one.
    /// This is where behavior trees meet utility AI.
    /// 
    /// Used for complex enemies (lieutenants, Dark Rangers) that need to evaluate
    /// multiple options and pick the best one contextually.
    /// </summary>
    public class UtilitySelectorNode : IBehaviorNode
    {
        public string NodeId { get; }
        private readonly List<ScoredOption> _options;

        public UtilitySelectorNode(string nodeId, params ScoredOption[] options)
        {
            NodeId = nodeId;
            _options = new List<ScoredOption>(options);
        }

        public NodeStatus Process(AIContext context)
        {
            // Score all options
            var scored = _options
                .Select(opt => new { Option = opt, Score = opt.Scorer.Score(context) })
                .Where(s => s.Score > 0)
                .OrderByDescending(s => s.Score)
                .ToList();

            if (scored.Count == 0)
                return NodeStatus.Failure;

            // Execute the highest-scoring option
            return scored[0].Option.Action.Process(context);
        }

        public void Reset()
        {
            foreach (var option in _options)
                option.Action.Reset();
        }
    }

    /// <summary>
    /// An option in a UtilitySelectorNode — pairs a scorer with an action.
    /// </summary>
    public class ScoredOption
    {
        public IUtilityScorer Scorer { get; }
        public IBehaviorNode Action { get; }

        public ScoredOption(IUtilityScorer scorer, IBehaviorNode action)
        {
            Scorer = scorer;
            Action = action;
        }
    }

    /// <summary>
    /// Scores a potential action based on observable game state.
    /// Higher score = more desirable action.
    /// 
    /// Composable: different scorers are plugged into different enemy types.
    /// Difficulty tuning = which scorers are active.
    /// </summary>
    public interface IUtilityScorer
    {
        string ScorerId { get; }
        float Score(AIContext context);
    }
}
