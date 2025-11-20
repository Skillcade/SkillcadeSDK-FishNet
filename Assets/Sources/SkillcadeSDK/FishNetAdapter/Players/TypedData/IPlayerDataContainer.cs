using System;

namespace SkillcadeSDK.FishNetAdapter.Players.TypedData
{
    /// <summary>
    /// Интерфейс для типизированных контейнеров данных игрока.
    /// Реализуйте этот интерфейс для создания своих структур данных.
    /// </summary>
    /// <remarks>
    /// ВАЖНО: Все типы данных должны быть сериализуемыми!
    /// Поддерживаются: примитивы (int, float, bool, string), структуры с [Serializable]
    /// </remarks>
    public interface IPlayerDataContainer
    {
        /// <summary>
        /// Уникальный ключ для хранения данных в PlayerData.
        /// Рекомендуется использовать название типа.
        /// </summary>
        string DataKey { get; }
    }

    /// <summary>
    /// Базовая реализация контейнера данных.
    /// Наследуйте от этого класса для создания своих типов данных.
    /// </summary>
    /// <example>
    /// [Serializable]
    /// public class PlayerInventoryData : PlayerDataContainer
    /// {
    ///     public int Gold;
    ///     public int Gems;
    ///     public List&lt;string&gt; Items;
    ///
    ///     public override string DataKey => "PlayerInventory";
    /// }
    /// </example>
    [Serializable]
    public abstract class PlayerDataContainer : IPlayerDataContainer
    {
        public abstract string DataKey { get; }
    }
}
