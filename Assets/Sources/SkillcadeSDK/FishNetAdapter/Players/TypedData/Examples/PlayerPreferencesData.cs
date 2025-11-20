using System;

namespace SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples
{
    /// <summary>
    /// Пример типизированного контейнера - настройки игрока.
    /// </summary>
    [Serializable]
    public class PlayerPreferencesData : PlayerDataContainer
    {
        public override string DataKey => "PlayerPreferences";

        // ==================== VISUAL SETTINGS ====================

        /// <summary>
        /// ID выбранного скина персонажа
        /// </summary>
        public string SelectedSkinId;

        /// <summary>
        /// Цвет персонажа (RGB в формате hex: "#FF0000")
        /// </summary>
        public string PlayerColor = "#FFFFFF";

        /// <summary>
        /// ID выбранной эмоции/эмодзи
        /// </summary>
        public string SelectedEmoteId;

        // ==================== GAME SETTINGS ====================

        /// <summary>
        /// Чувствительность мыши (0.1 - 10.0)
        /// </summary>
        public float MouseSensitivity = 1.0f;

        /// <summary>
        /// Включен ли звук
        /// </summary>
        public bool SoundEnabled = true;

        /// <summary>
        /// Громкость звука (0.0 - 1.0)
        /// </summary>
        public float SoundVolume = 0.8f;

        /// <summary>
        /// Включена ли музыка
        /// </summary>
        public bool MusicEnabled = true;

        /// <summary>
        /// Громкость музыки (0.0 - 1.0)
        /// </summary>
        public float MusicVolume = 0.6f;

        // ==================== SOCIAL ====================

        /// <summary>
        /// Отображаемое имя игрока
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Разрешены ли приглашения в друзья
        /// </summary>
        public bool AllowFriendRequests = true;

        /// <summary>
        /// Показывать ли статус онлайн
        /// </summary>
        public bool ShowOnlineStatus = true;

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Проверяет корректность цвета
        /// </summary>
        public bool IsColorValid()
        {
            return PlayerColor != null &&
                   PlayerColor.StartsWith("#") &&
                   PlayerColor.Length == 7;
        }

        /// <summary>
        /// Устанавливает безопасные значения по умолчанию
        /// </summary>
        public void ResetToDefaults()
        {
            SelectedSkinId = null;
            PlayerColor = "#FFFFFF";
            SelectedEmoteId = null;
            MouseSensitivity = 1.0f;
            SoundEnabled = true;
            SoundVolume = 0.8f;
            MusicEnabled = true;
            MusicVolume = 0.6f;
            AllowFriendRequests = true;
            ShowOnlineStatus = true;
        }

        /// <summary>
        /// Создает настройки по умолчанию
        /// </summary>
        public static PlayerPreferencesData CreateDefault()
        {
            var prefs = new PlayerPreferencesData();
            prefs.ResetToDefaults();
            return prefs;
        }
    }
}
