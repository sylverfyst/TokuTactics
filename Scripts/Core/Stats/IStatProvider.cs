namespace TokuTactics.Core.Stats
{
    /// <summary>
    /// Interface for anything that provides stats: forms, weapons, modifiers, buffs.
    /// The composable stat system works by collecting all IStatProviders
    /// and combining their StatBlocks.
    /// </summary>
    public interface IStatProvider
    {
        StatBlock GetStats();
    }
}
