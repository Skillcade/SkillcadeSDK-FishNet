using System;

namespace SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples
{
    /// <summary>
    /// Пример типизированного контейнера - статистика игрока.
    /// </summary>
    [Serializable]
    public class PlayerStatsData : PlayerDataContainer
    {
        public override string DataKey => "PlayerStats";

        // ==================== COMBAT STATS ====================

        /// <summary>
        /// Максимальное здоровье
        /// </summary>
        public float MaxHealth = 100f;

        /// <summary>
        /// Текущее здоровье
        /// </summary>
        public float CurrentHealth = 100f;

        /// <summary>
        /// Урон атаки
        /// </summary>
        public float AttackDamage = 10f;

        /// <summary>
        /// Защита
        /// </summary>
        public float Defense = 5f;

        /// <summary>
        /// Скорость передвижения
        /// </summary>
        public float MoveSpeed = 5f;

        // ==================== LEVEL & EXPERIENCE ====================

        /// <summary>
        /// Текущий уровень игрока
        /// </summary>
        public int Level = 1;

        /// <summary>
        /// Текущий опыт
        /// </summary>
        public int Experience = 0;

        /// <summary>
        /// Опыт, необходимый для следующего уровня
        /// </summary>
        public int ExperienceToNextLevel = 100;

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Проверяет, жив ли игрок
        /// </summary>
        public bool IsAlive()
        {
            return CurrentHealth > 0;
        }

        /// <summary>
        /// Получает процент здоровья (0-1)
        /// </summary>
        public float GetHealthPercent()
        {
            return MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
        }

        /// <summary>
        /// Наносит урон игроку
        /// </summary>
        /// <returns>Реальный полученный урон</returns>
        public float TakeDamage(float damage)
        {
            float actualDamage = Math.Max(0, damage - Defense);
            CurrentHealth = Math.Max(0, CurrentHealth - actualDamage);
            return actualDamage;
        }

        /// <summary>
        /// Восстанавливает здоровье
        /// </summary>
        public void Heal(float amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        /// <summary>
        /// Добавляет опыт и проверяет повышение уровня
        /// </summary>
        /// <returns>true если игрок повысил уровень</returns>
        public bool AddExperience(int amount)
        {
            Experience += amount;

            if (Experience >= ExperienceToNextLevel)
            {
                LevelUp();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Повышает уровень игрока
        /// </summary>
        private void LevelUp()
        {
            Level++;
            Experience -= ExperienceToNextLevel;
            ExperienceToNextLevel = (int)(ExperienceToNextLevel * 1.5f);

            // Улучшение статов при повышении уровня
            MaxHealth += 10f;
            CurrentHealth = MaxHealth;
            AttackDamage += 2f;
            Defense += 1f;
        }

        /// <summary>
        /// Создает начальную статистику
        /// </summary>
        public static PlayerStatsData CreateDefault()
        {
            return new PlayerStatsData
            {
                MaxHealth = 100f,
                CurrentHealth = 100f,
                AttackDamage = 10f,
                Defense = 5f,
                MoveSpeed = 5f,
                Level = 1,
                Experience = 0,
                ExperienceToNextLevel = 100
            };
        }
    }
}
