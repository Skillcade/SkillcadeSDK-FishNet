#if UNITY_SERVER || UNITY_EDITOR
namespace SkillcadeSDK.FishNetAdapter.Players
{
    public enum ServerDisconnectReason
    {
        AuthRejected,
        DuplicatePlayer,
        LobbyFull,
        ReconnectRejected,
        Anticheat,
        AdminKick,
    }
}
#endif
