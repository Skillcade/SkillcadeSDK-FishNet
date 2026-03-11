using SkillcadeSDK.StateMachine;

namespace SkillcadeSDK.Events
{
    /// <summary>
    /// Published when entering WaitForPlayers state.
    /// </summary>
    public readonly struct WaitForPlayersEnterEvent : IGameEvent
    {
    }

    /// <summary>
    /// Published when all players are ready and we can proceed to countdown.
    /// </summary>
    public readonly struct AllPlayersReadyEvent : IGameEvent
    {
    }

    /// <summary>
    /// Published on each countdown tick.
    /// </summary>
    public readonly struct CountdownTickEvent : IGameEvent
    {
        public readonly int RemainingSeconds;

        public CountdownTickEvent(int remainingSeconds)
        {
            RemainingSeconds = remainingSeconds;
        }
    }

    /// <summary>
    /// Published when the game starts (Running state begins).
    /// </summary>
    public readonly struct RunningStartEvent : IGameEvent
    {
    }

    /// <summary>
    /// Published when a player finishes the game.
    /// </summary>
    public readonly struct PlayerFinishedEvent : IGameEvent
    {
        public readonly int WinnerId;

        public PlayerFinishedEvent(int winnerId)
        {
            WinnerId = winnerId;
        }
    }

    /// <summary>
    /// Published when the game finishes with a winner.
    /// </summary>
    public readonly struct GameFinishedEvent : IGameEvent
    {
        public readonly int WinnerId;
        public readonly FinishReason FinishReason;

        public GameFinishedEvent(int winnerId, FinishReason finishReason)
        {
            WinnerId = winnerId;
            FinishReason = finishReason;
        }
    }
}
