
using SkillcadeSDK.Common.Players;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    public static partial class PlayerDataExtensions
    {
        public static bool IsReady(this IPlayerData playerData)
        {
            return playerData.TryGetData(PlayerDataConst.IsReady, out bool isReady) && isReady;
        }
        
        public static bool IsInGame(this IPlayerData playerData)
        {
            return playerData.TryGetData(PlayerDataConst.InGame, out bool inGame) && inGame;
        }
    }
}