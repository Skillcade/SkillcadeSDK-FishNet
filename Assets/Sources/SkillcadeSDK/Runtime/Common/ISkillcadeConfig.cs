namespace SkillcadeSDK.Common
{
    /// <summary>
    /// Interface for game configuration settings.
    /// Game-specific implementations provide timing and behavior configuration.
    /// </summary>
    public interface ISkillcadeConfig
    {
        /// <summary>
        /// Duration of the countdown before the game starts, in seconds.
        /// </summary>
        float StartGameCountdownSeconds { get; }

        /// <summary>
        /// Duration to wait after the game finishes before transitioning, in seconds.
        /// </summary>
        float WaitAfterFinishSeconds { get; }
        bool UseReplaysV1 { get; }
    }
}
