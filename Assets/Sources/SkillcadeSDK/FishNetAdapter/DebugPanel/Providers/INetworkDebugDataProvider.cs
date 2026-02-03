#if SKILLCADE_DEBUG

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Providers
{
    public interface INetworkDebugDataProvider
    {
        string SectionName { get; }
        bool IsAvailable { get; }
        string GetFormattedData();
    }
}
#endif
