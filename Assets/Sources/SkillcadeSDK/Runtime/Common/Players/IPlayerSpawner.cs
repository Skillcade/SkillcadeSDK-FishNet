namespace SkillcadeSDK.Common.Players
{
    /// <summary>
    /// Interface for spawning and despawning player objects.
    /// Game-specific implementations handle the actual spawning logic.
    /// </summary>
    public interface IPlayerSpawner
    {
        /// <summary>
        /// Ensures all players that are marked as InGame are spawned.
        /// If players are already spawned, does nothing.
        /// </summary>
        void EnsurePlayersSpawned();

        /// <summary>
        /// Ensures all players are despawned.
        /// If players are already despawned, does nothing.
        /// </summary>
        void EnsurePlayersDespawned();
    }
}
