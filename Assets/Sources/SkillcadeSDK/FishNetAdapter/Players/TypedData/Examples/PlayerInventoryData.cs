using System;
using System.Collections.Generic;

namespace SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples
{
    /// <summary>
    /// Пример типизированного контейнера данных - инвентарь игрока.
    /// </summary>
    /// <remarks>
    /// ВАЖНО для junior разработчиков:
    /// 1. Класс должен быть [Serializable]
    /// 2. Все поля должны быть public или иметь [SerializeField]
    /// 3. DataKey должен быть уникальным
    /// </remarks>
    [Serializable]
    public class PlayerInventoryData : PlayerDataContainer
    {
        /// <summary>
        /// Уникальный ключ для хранения данных инвентаря
        /// </summary>
        public override string DataKey => "PlayerInventory";

        // ==================== CURRENCY ====================

        /// <summary>
        /// Количество золота у игрока
        /// </summary>
        public int Gold;

        /// <summary>
        /// Количество гемов у игрока
        /// </summary>
        public int Gems;

        // ==================== ITEMS ====================

        /// <summary>
        /// Список предметов в инвентаре (ID предметов)
        /// </summary>
        public List<string> Items = new();

        /// <summary>
        /// Экипированное оружие (ID оружия)
        /// </summary>
        public string EquippedWeapon;

        /// <summary>
        /// Экипированная броня (ID брони)
        /// </summary>
        public string EquippedArmor;

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Проверяет, есть ли предмет в инвентаре
        /// </summary>
        public bool HasItem(string itemId)
        {
            return Items.Contains(itemId);
        }

        /// <summary>
        /// Добавляет предмет в инвентарь
        /// </summary>
        public void AddItem(string itemId)
        {
            if (!Items.Contains(itemId))
                Items.Add(itemId);
        }

        /// <summary>
        /// Удаляет предмет из инвентаря
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            return Items.Remove(itemId);
        }

        /// <summary>
        /// Проверяет, достаточно ли золота
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return Gold >= amount;
        }

        /// <summary>
        /// Создает начальный инвентарь для нового игрока
        /// </summary>
        public static PlayerInventoryData CreateDefault()
        {
            return new PlayerInventoryData
            {
                Gold = 0,
                Gems = 0,
                Items = new List<string>(),
                EquippedWeapon = null,
                EquippedArmor = null
            };
        }
    }
}
