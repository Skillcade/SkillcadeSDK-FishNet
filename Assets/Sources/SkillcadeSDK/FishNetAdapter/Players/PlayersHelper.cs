using System.Collections.Generic;
using System.Linq;
using SkillcadeSDK.Common.Players;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Вспомогательный класс для упрощенной работы с коллекцией игроков.
    /// Содержит часто используемые операции для работы с несколькими игроками.
    /// </summary>
    public static class PlayersHelper
    {
        // ==================== READY PLAYERS ====================

        /// <summary>
        /// Получает количество готовых игроков.
        /// </summary>
        /// <example>
        /// int readyCount = PlayersHelper.GetReadyPlayersCount(playersController);
        /// Debug.Log($"Готовых игроков: {readyCount}");
        /// </example>
        public static int GetReadyPlayersCount(IPlayersController playersController)
        {
            int count = 0;
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                if (playerData.IsReady())
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Получает всех готовых игроков.
        /// </summary>
        /// <returns>Список готовых игроков</returns>
        public static List<IPlayerData> GetReadyPlayers(IPlayersController playersController)
        {
            var readyPlayers = new List<IPlayerData>();
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                if (playerData.IsReady())
                    readyPlayers.Add(playerData);
            }
            return readyPlayers;
        }

        /// <summary>
        /// Проверяет, все ли игроки готовы.
        /// </summary>
        /// <param name="minPlayers">Минимальное количество игроков для старта</param>
        /// <returns>true если всех игроков >= minPlayers и все готовы</returns>
        /// <example>
        /// // Проверяем, можно ли начинать игру
        /// if (PlayersHelper.AreAllPlayersReady(playersController, minPlayers: 2))
        /// {
        ///     StartGame();
        /// }
        /// </example>
        public static bool AreAllPlayersReady(IPlayersController playersController, int minPlayers = 1)
        {
            int totalPlayers = 0;
            int readyPlayers = 0;

            foreach (var playerData in playersController.GetAllPlayersData())
            {
                totalPlayers++;
                if (playerData.IsReady())
                    readyPlayers++;
            }

            return totalPlayers >= minPlayers && readyPlayers == totalPlayers;
        }

        /// <summary>
        /// Сбрасывает статус готовности для всех игроков.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        /// <example>
        /// // При возврате в лобби
        /// PlayersHelper.ResetAllPlayersReady(playersController);
        /// </example>
        public static void ResetAllPlayersReady(IPlayersController playersController)
        {
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                playerData.SetReadyOnServer(false);
            }
        }

        // ==================== IN-GAME PLAYERS ====================

        /// <summary>
        /// Получает всех игроков, находящихся в игре.
        /// </summary>
        public static List<IPlayerData> GetInGamePlayers(IPlayersController playersController)
        {
            var inGamePlayers = new List<IPlayerData>();
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                if (playerData.IsInGame())
                    inGamePlayers.Add(playerData);
            }
            return inGamePlayers;
        }

        /// <summary>
        /// Получает количество игроков в игре.
        /// </summary>
        public static int GetInGamePlayersCount(IPlayersController playersController)
        {
            int count = 0;
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                if (playerData.IsInGame())
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Переводит всех готовых игроков в статус "в игре".
        /// ⚠️ Только для серверного кода!
        /// </summary>
        /// <example>
        /// // После завершения обратного отсчета
        /// PlayersHelper.SetReadyPlayersInGame(playersController);
        /// playerSpawner.SpawnAllInGamePlayers();
        /// </example>
        public static void SetReadyPlayersInGame(IPlayersController playersController)
        {
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                if (playerData.IsReady())
                {
                    playerData.SetInGameOnServer(true);
                }
            }
        }

        /// <summary>
        /// Сбрасывает статус "в игре" для всех игроков.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        public static void ResetAllPlayersInGame(IPlayersController playersController)
        {
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                playerData.SetInGameOnServer(false);
            }
        }

        // ==================== MATCH OPERATIONS ====================

        /// <summary>
        /// Устанавливает Match ID для всех игроков.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        /// <example>
        /// string matchId = System.Guid.NewGuid().ToString();
        /// PlayersHelper.SetMatchIdForAllPlayers(playersController, matchId);
        /// </example>
        public static void SetMatchIdForAllPlayers(IPlayersController playersController, string matchId)
        {
            foreach (var playerData in playersController.GetAllPlayersData())
            {
                playerData.SetMatchIdOnServer(matchId);
            }
        }

        // ==================== PLAYER LOOKUP ====================

        /// <summary>
        /// Находит игрока по User ID.
        /// </summary>
        /// <param name="userId">ID пользователя для поиска</param>
        /// <param name="playerData">Найденные данные игрока</param>
        /// <returns>true если игрок найден</returns>
        public static bool TryFindPlayerByUserId(IPlayersController playersController, string userId, out IPlayerData playerData)
        {
            foreach (var player in playersController.GetAllPlayersData())
            {
                if (player.TryGetUserId(out string playerId) && playerId == userId)
                {
                    playerData = player;
                    return true;
                }
            }

            playerData = null;
            return false;
        }

        // ==================== STATISTICS ====================

        /// <summary>
        /// Получает статистику игроков (готовых / всего).
        /// </summary>
        /// <returns>Кортеж (readyCount, totalCount)</returns>
        /// <example>
        /// var (ready, total) = PlayersHelper.GetPlayersStats(playersController);
        /// statusText.text = $"Готово: {ready}/{total}";
        /// </example>
        public static (int readyCount, int totalCount) GetPlayersStats(IPlayersController playersController)
        {
            int total = 0;
            int ready = 0;

            foreach (var playerData in playersController.GetAllPlayersData())
            {
                total++;
                if (playerData.IsReady())
                    ready++;
            }

            return (ready, total);
        }

        /// <summary>
        /// Получает детальную статистику игроков.
        /// </summary>
        public static PlayerStats GetDetailedStats(IPlayersController playersController)
        {
            var stats = new PlayerStats();

            foreach (var playerData in playersController.GetAllPlayersData())
            {
                stats.TotalPlayers++;

                if (playerData.IsReady())
                    stats.ReadyPlayers++;

                if (playerData.IsInGame())
                    stats.InGamePlayers++;
            }

            return stats;
        }
    }

    /// <summary>
    /// Структура для хранения статистики игроков.
    /// </summary>
    public struct PlayerStats
    {
        public int TotalPlayers;
        public int ReadyPlayers;
        public int InGamePlayers;

        public override string ToString()
        {
            return $"Players: {TotalPlayers} | Ready: {ReadyPlayers} | InGame: {InGamePlayers}";
        }
    }
}
