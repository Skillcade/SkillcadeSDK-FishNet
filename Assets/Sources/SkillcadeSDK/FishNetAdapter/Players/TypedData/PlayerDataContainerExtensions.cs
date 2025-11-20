using SkillcadeSDK.Common.Players;
using System;

namespace SkillcadeSDK.FishNetAdapter.Players.TypedData
{
    /// <summary>
    /// Extension методы для работы с типизированными контейнерами данных.
    /// Позволяет удобно работать с кастомными структурами данных игрока.
    /// </summary>
    public static class PlayerDataContainerExtensions
    {
        // ==================== CLIENT METHODS ====================

        /// <summary>
        /// Устанавливает типизированный контейнер данных с клиента.
        /// Используйте для отправки данных от локального игрока на сервер.
        /// </summary>
        /// <typeparam name="T">Тип контейнера данных</typeparam>
        /// <param name="container">Контейнер с данными для отправки</param>
        /// <example>
        /// // Создаем и отправляем данные инвентаря
        /// var inventory = new PlayerInventoryData
        /// {
        ///     Gold = 100,
        ///     Gems = 50,
        ///     Items = new List&lt;string&gt; { "Sword", "Shield" }
        /// };
        /// localPlayerData.SetContainerFromClient(inventory);
        /// </example>
        public static void SetContainerFromClient<T>(this IPlayerData playerData, T container)
            where T : IPlayerDataContainer
        {
            playerData.SetDataOnLocalClient(container.DataKey, container);
        }

        // ==================== SERVER METHODS ====================

        /// <summary>
        /// Устанавливает типизированный контейнер данных на сервере.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        /// <typeparam name="T">Тип контейнера данных</typeparam>
        /// <param name="container">Контейнер с данными</param>
        /// <example>
        /// // На сервере устанавливаем начальный инвентарь
        /// var inventory = new PlayerInventoryData
        /// {
        ///     Gold = 0,
        ///     Gems = 0,
        ///     Items = new List&lt;string&gt;()
        /// };
        /// playerData.SetContainerOnServer(inventory);
        /// </example>
        public static void SetContainerOnServer<T>(this IPlayerData playerData, T container)
            where T : IPlayerDataContainer
        {
            playerData.SetDataOnServer(container.DataKey, container);
        }

        // ==================== READ METHODS ====================

        /// <summary>
        /// Получает типизированный контейнер данных.
        /// </summary>
        /// <typeparam name="T">Тип контейнера данных</typeparam>
        /// <param name="container">Полученный контейнер</param>
        /// <returns>true если контейнер найден и тип совпадает</returns>
        /// <example>
        /// // Проверяем и получаем данные инвентаря
        /// if (playerData.TryGetContainer(out PlayerInventoryData inventory))
        /// {
        ///     Debug.Log($"Золото: {inventory.Gold}, Гемов: {inventory.Gems}");
        ///     foreach (var item in inventory.Items)
        ///         Debug.Log($"Предмет: {item}");
        /// }
        /// </example>
        public static bool TryGetContainer<T>(this IPlayerData playerData, out T container)
            where T : IPlayerDataContainer
        {
            // Создаем временный экземпляр для получения ключа
            var tempInstance = Activator.CreateInstance<T>();
            return playerData.TryGetData(tempInstance.DataKey, out container);
        }

        /// <summary>
        /// Получает типизированный контейнер данных или возвращает null.
        /// </summary>
        /// <typeparam name="T">Тип контейнера данных</typeparam>
        /// <returns>Контейнер данных или null если не найден</returns>
        /// <example>
        /// var inventory = playerData.GetContainer&lt;PlayerInventoryData&gt;();
        /// if (inventory != null)
        /// {
        ///     inventory.Gold += 10;
        ///     playerData.SetContainerFromClient(inventory);
        /// }
        /// </example>
        public static T GetContainer<T>(this IPlayerData playerData)
            where T : IPlayerDataContainer
        {
            return playerData.TryGetContainer(out T container) ? container : default;
        }

        /// <summary>
        /// Проверяет наличие контейнера данных у игрока.
        /// </summary>
        /// <typeparam name="T">Тип контейнера данных</typeparam>
        /// <returns>true если контейнер существует</returns>
        public static bool HasContainer<T>(this IPlayerData playerData)
            where T : IPlayerDataContainer
        {
            return playerData.TryGetContainer<T>(out _);
        }

        // ==================== MODIFY METHODS ====================

        /// <summary>
        /// Модифицирует контейнер данных с клиента.
        /// Получает текущий контейнер, применяет изменения и отправляет обратно.
        /// </summary>
        /// <typeparam name="T">Тип контейнера данных</typeparam>
        /// <param name="modifier">Функция модификации контейнера</param>
        /// <example>
        /// // Добавляем золото игроку
        /// localPlayerData.ModifyContainerFromClient&lt;PlayerInventoryData&gt;(inventory =>
        /// {
        ///     inventory.Gold += 10;
        ///     inventory.Items.Add("Potion");
        /// });
        /// </example>
        public static void ModifyContainerFromClient<T>(this IPlayerData playerData, Action<T> modifier)
            where T : IPlayerDataContainer, new()
        {
            // Получаем текущий контейнер или создаем новый
            T container = playerData.TryGetContainer(out T existing)
                ? existing
                : new T();

            // Применяем изменения
            modifier(container);

            // Отправляем обновленный контейнер
            playerData.SetContainerFromClient(container);
        }

        /// <summary>
        /// Модифицирует контейнер данных на сервере.
        /// ⚠️ Только для серверного кода!
        /// </summary>
        /// <typeparam name="T">Тип контейнера данных</typeparam>
        /// <param name="modifier">Функция модификации контейнера</param>
        /// <example>
        /// // На сервере даем награду игроку
        /// playerData.ModifyContainerOnServer&lt;PlayerInventoryData&gt;(inventory =>
        /// {
        ///     inventory.Gold += 100;
        ///     inventory.Gems += 10;
        /// });
        /// </example>
        public static void ModifyContainerOnServer<T>(this IPlayerData playerData, Action<T> modifier)
            where T : IPlayerDataContainer, new()
        {
            // Получаем текущий контейнер или создаем новый
            T container = playerData.TryGetContainer(out T existing)
                ? existing
                : new T();

            // Применяем изменения
            modifier(container);

            // Сохраняем обновленный контейнер
            playerData.SetContainerOnServer(container);
        }
    }
}
