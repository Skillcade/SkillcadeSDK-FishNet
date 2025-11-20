using SkillcadeSDK.Common.Players;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Типобезопасные extension методы для упрощенной работы с данными игрока.
    /// Эти методы предоставляют удобный API с IntelliSense подсказками.
    /// </summary>
    public static class PlayerDataExtensions
    {
        // ==================== READY STATUS ====================

        /// <summary>
        /// Устанавливает статус готовности игрока с клиента.
        /// Используйте этот метод когда игрок нажимает кнопку "Готов".
        /// </summary>
        /// <example>
        /// // Пример: Когда игрок нажимает кнопку "Ready"
        /// localPlayerData.SetReadyFromClient(true);
        /// </example>
        public static void SetReadyFromClient(this IPlayerData playerData, bool isReady)
        {
            playerData.SetDataOnLocalClient(PlayerDataConst.IsReady, isReady);
        }

        /// <summary>
        /// Устанавливает статус готовности игрока на сервере.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        public static void SetReadyOnServer(this IPlayerData playerData, bool isReady)
        {
            playerData.SetDataOnServer(PlayerDataConst.IsReady, isReady);
        }

        /// <summary>
        /// Проверяет, готов ли игрок к началу игры.
        /// </summary>
        /// <returns>true если игрок готов, false в противном случае</returns>
        /// <example>
        /// if (playerData.IsReady())
        /// {
        ///     Debug.Log("Игрок готов!");
        /// }
        /// </example>
        public static bool IsReady(this IPlayerData playerData)
        {
            return playerData.TryGetData(PlayerDataConst.IsReady, out bool isReady) && isReady;
        }

        // ==================== IN-GAME STATUS ====================

        /// <summary>
        /// Устанавливает флаг "в игре" на сервере.
        /// Этот флаг определяет, должен ли игрок быть заспавнен в игровом мире.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        public static void SetInGameOnServer(this IPlayerData playerData, bool inGame)
        {
            playerData.SetDataOnServer(PlayerDataConst.InGame, inGame);
        }

        /// <summary>
        /// Проверяет, находится ли игрок в игре.
        /// </summary>
        /// <returns>true если игрок в игре, false в противном случае</returns>
        public static bool IsInGame(this IPlayerData playerData)
        {
            return playerData.TryGetData(PlayerDataConst.InGame, out bool inGame) && inGame;
        }

        // ==================== MATCH ID ====================

        /// <summary>
        /// Устанавливает ID матча для игрока на сервере.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        public static void SetMatchIdOnServer(this IPlayerData playerData, string matchId)
        {
            playerData.SetDataOnServer(PlayerDataConst.MatchId, matchId);
        }

        /// <summary>
        /// Получает ID матча игрока.
        /// </summary>
        /// <param name="matchId">ID матча, если найден</param>
        /// <returns>true если ID найден, false в противном случае</returns>
        public static bool TryGetMatchId(this IPlayerData playerData, out string matchId)
        {
            return playerData.TryGetData(PlayerDataConst.MatchId, out matchId);
        }

        /// <summary>
        /// Получает ID матча игрока или null если не установлен.
        /// </summary>
        public static string GetMatchId(this IPlayerData playerData)
        {
            playerData.TryGetData(PlayerDataConst.MatchId, out string matchId);
            return matchId;
        }

        // ==================== USER ID ====================

        /// <summary>
        /// Устанавливает User ID игрока с клиента.
        /// Обычно вызывается после успешной авторизации.
        /// </summary>
        public static void SetUserIdFromClient(this IPlayerData playerData, string userId)
        {
            playerData.SetDataOnLocalClient(PlayerDataConst.UserId, userId);
        }

        /// <summary>
        /// Устанавливает User ID игрока на сервере.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        public static void SetUserIdOnServer(this IPlayerData playerData, string userId)
        {
            playerData.SetDataOnServer(PlayerDataConst.UserId, userId);
        }

        /// <summary>
        /// Получает User ID игрока.
        /// </summary>
        /// <param name="userId">User ID, если найден</param>
        /// <returns>true если ID найден, false в противном случае</returns>
        public static bool TryGetUserId(this IPlayerData playerData, out string userId)
        {
            return playerData.TryGetData(PlayerDataConst.UserId, out userId);
        }

        /// <summary>
        /// Получает User ID игрока или null если не установлен.
        /// </summary>
        public static string GetUserId(this IPlayerData playerData)
        {
            playerData.TryGetData(PlayerDataConst.UserId, out string userId);
            return userId;
        }
    }
}
