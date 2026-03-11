namespace SkillcadeSDK.StateMachine
{
    /// <summary>
    /// Data passed to FinishedState when entering.
    /// </summary>
    public class FinishedStateData
    {
        public readonly int WinnerClientId;
        public readonly FinishReason FinishReason;

        public FinishedStateData(int winnerClientId, FinishReason finishReason)
        {
            WinnerClientId = winnerClientId;
            FinishReason = finishReason;
        }
    }
}
