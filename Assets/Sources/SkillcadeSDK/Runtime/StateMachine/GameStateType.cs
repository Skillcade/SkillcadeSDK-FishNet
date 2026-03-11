namespace SkillcadeSDK.StateMachine
{
    /// <summary>
    /// Standard game state types used across all Skillcade games.
    /// </summary>
    public enum GameStateType
    {
        None = 0,
        WaitForPlayers = 1,
        Countdown = 2,
        Running = 3,
        Finished = 4,
    }
}
