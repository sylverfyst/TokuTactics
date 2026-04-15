using TokuTactics.Systems.PhaseManagement;

namespace TokuTactics.Commands.Phase
{
    /// <summary>
    /// Result of a win/loss condition check.
    /// </summary>
    public class WinLossResult
    {
        /// <summary>Whether the mission ended.</summary>
        public bool Ended { get; set; }

        /// <summary>The end state (Victory or Defeat). Only meaningful if Ended is true.</summary>
        public MissionState EndState { get; set; }

        /// <summary>ID of the fallen ranger if defeat. Null otherwise.</summary>
        public string FallenRangerId { get; set; }

        public static WinLossResult NoEnd()
        {
            return new WinLossResult { Ended = false };
        }

        public static WinLossResult Victory()
        {
            return new WinLossResult { Ended = true, EndState = MissionState.Victory };
        }

        public static WinLossResult Defeat(string fallenRangerId)
        {
            return new WinLossResult
            {
                Ended = true,
                EndState = MissionState.Defeat,
                FallenRangerId = fallenRangerId
            };
        }
    }
}
