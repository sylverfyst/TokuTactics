namespace TokuTactics.Core.Phase
{
    /// <summary>Mission state machine tracking the overall lifecycle.</summary>
    public enum MissionState
    {
        NotStarted,
        Active,
        Victory,
        Defeat
    }

    /// <summary>Where we are within a round.</summary>
    public enum PhaseState
    {
        Idle,
        PlayerPhase,
        EnemyPhase
    }
}
