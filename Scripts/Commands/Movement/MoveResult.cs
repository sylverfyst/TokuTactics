using TokuTactics.Core.Grid;

namespace TokuTactics.Commands.Movement
{
    /// <summary>
    /// Result of a movement attempt.
    /// </summary>
    public class MoveResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; }
        public GridPosition? NewPosition { get; set; }

        public static MoveResult CreateSuccess(GridPosition newPosition)
        {
            return new MoveResult
            {
                Success = true,
                NewPosition = newPosition
            };
        }

        public static MoveResult CreateFailure(string reason)
        {
            return new MoveResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }
}
