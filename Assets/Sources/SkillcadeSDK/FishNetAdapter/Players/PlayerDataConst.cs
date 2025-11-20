namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Константы для ключей данных игрока.
    /// </summary>
    /// <remarks>
    /// ВАЖНО: Вместо прямого использования этих констант,
    /// рекомендуется использовать extension методы из PlayerDataExtensions.
    ///
    /// Пример:
    /// ✅ playerData.SetReadyFromClient(true);
    /// ❌ playerData.SetDataOnLocalClient(PlayerDataConst.IsReady, true);
    ///
    /// При добавлении новых констант, также добавляйте extension методы!
    /// </remarks>
    public static partial class PlayerDataConst
    {
        /// <summary>
        /// Готовность игрока к началу игры (bool)
        /// </summary>
        public const string IsReady = "IsReady";

        /// <summary>
        /// Находится ли игрок в активной игре (bool)
        /// </summary>
        public const string InGame = "InGame";

        /// <summary>
        /// ID текущего матча (string)
        /// </summary>
        public const string MatchId = "MatchId";

        /// <summary>
        /// ID пользователя в системе (string)
        /// </summary>
        public const string UserId = "UserId";
    }
}