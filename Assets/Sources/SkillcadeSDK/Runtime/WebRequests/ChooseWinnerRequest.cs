namespace SkillcadeSDK.WebRequests
{
#if UNITY_SERVER || UNITY_EDITOR
    public class ChooseWinnerRequest
    {
        public string WinnerId { get; set; }
    }
#endif
}