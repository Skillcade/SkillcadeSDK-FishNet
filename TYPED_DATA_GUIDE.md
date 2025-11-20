# 🎯 Руководство по типизированным контейнерам данных

## 🚀 Что это такое?

**Типизированные контейнеры** - это способ создавать свои собственные структуры данных для игроков с полной типобезопасностью и удобным API.

### До (старый способ)
```csharp
// Нужно помнить строковые ключи и типы
playerData.SetDataOnServer("Gold", 100);
playerData.SetDataOnServer("Gems", 50);
playerData.SetDataOnServer("Items", new List<string> { "Sword" });

// Легко ошибиться с типом
if (playerData.TryGetData("Gold", out int gold)) { }
if (playerData.TryGetData("Gems", out int gems)) { }
```

### После (новый способ)
```csharp
// Создаем типизированный контейнер
var inventory = new PlayerInventoryData
{
    Gold = 100,
    Gems = 50,
    Items = new List<string> { "Sword" }
};

// Одним вызовом сохраняем все данные
playerData.SetContainerOnServer(inventory);

// Получаем типизированные данные
if (playerData.TryGetContainer(out PlayerInventoryData inv))
{
    Debug.Log($"Золото: {inv.Gold}"); // IntelliSense работает!
}
```

---

## 📦 Создание своего контейнера данных

### Шаг 1: Создайте класс контейнера

```csharp
using System;
using SkillcadeSDK.FishNetAdapter.Players.TypedData;

[Serializable] // ОБЯЗАТЕЛЬНО!
public class PlayerInventoryData : PlayerDataContainer
{
    // ОБЯЗАТЕЛЬНО: Уникальный ключ
    public override string DataKey => "PlayerInventory";

    // Ваши данные (должны быть public или [SerializeField])
    public int Gold;
    public int Gems;
    public List<string> Items = new();
}
```

### Шаг 2: Используйте контейнер

```csharp
// Создание и сохранение
var inventory = new PlayerInventoryData
{
    Gold = 100,
    Gems = 50
};
playerData.SetContainerOnServer(inventory);

// Чтение
if (playerData.TryGetContainer(out PlayerInventoryData inv))
{
    Debug.Log($"Золото: {inv.Gold}");
}
```

**Готово!** Теперь данные автоматически синхронизируются между сервером и клиентами!

---

## 🎮 Примеры использования

### Пример 1: Инициализация данных при подключении игрока

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players.TypedData;
using SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples;
using VContainer;

public class PlayerDataInitializer : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;

    private void Start()
    {
        _playersController.OnPlayerAdded += OnPlayerJoined;
    }

    private void OnPlayerJoined(int playerId, IPlayerData playerData)
    {
        if (!IsServer) return;

        // Инициализируем инвентарь
        playerData.SetContainerOnServer(PlayerInventoryData.CreateDefault());

        // Инициализируем статистику
        playerData.SetContainerOnServer(PlayerStatsData.CreateDefault());

        // Инициализируем настройки
        playerData.SetContainerOnServer(PlayerPreferencesData.CreateDefault());

        Debug.Log($"Игрок {playerId} инициализирован!");
    }
}
```

### Пример 2: Магазин (покупка предметов)

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players.TypedData;
using SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    // CLIENT: Игрок пытается купить предмет
    public void BuyItem(IPlayerData playerData, string itemId, int price)
    {
        // Проверяем, достаточно ли золота
        if (playerData.TryGetContainer(out PlayerInventoryData inventory))
        {
            if (inventory.HasEnoughGold(price))
            {
                // Отправляем запрос на сервер
                PurchaseItemServerRpc(itemId, price);
            }
            else
            {
                Debug.Log("Недостаточно золота!");
            }
        }
    }

    // SERVER: Обработка покупки
    [ServerRpc(RequireOwnership = true)]
    private void PurchaseItemServerRpc(string itemId, int price)
    {
        var playerData = GetPlayerData(); // Ваш метод получения данных

        // Модифицируем инвентарь атомарно
        playerData.ModifyContainerOnServer<PlayerInventoryData>(inventory =>
        {
            // Проверяем еще раз на сервере (защита от читов)
            if (inventory.HasEnoughGold(price))
            {
                inventory.Gold -= price;
                inventory.AddItem(itemId);
                Debug.Log($"Куплен предмет {itemId} за {price} золота");
            }
        });
    }
}
```

### Пример 3: Боевая система (получение урона)

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players.TypedData;
using SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    // SERVER: Игрок получает урон
    public void ApplyDamage(IPlayerData playerData, float damage)
    {
        if (!IsServer) return;

        // Модифицируем статистику
        playerData.ModifyContainerOnServer<PlayerStatsData>(stats =>
        {
            float actualDamage = stats.TakeDamage(damage);

            Debug.Log($"Игрок получил {actualDamage} урона. " +
                      $"Здоровье: {stats.CurrentHealth}/{stats.MaxHealth}");

            if (!stats.IsAlive())
            {
                OnPlayerDied(playerData);
            }
        });
    }

    // SERVER: Игрок восстанавливает здоровье
    public void HealPlayer(IPlayerData playerData, float amount)
    {
        if (!IsServer) return;

        playerData.ModifyContainerOnServer<PlayerStatsData>(stats =>
        {
            stats.Heal(amount);
            Debug.Log($"Игрок восстановил {amount} HP. " +
                      $"Здоровье: {stats.CurrentHealth}/{stats.MaxHealth}");
        });
    }

    // SERVER: Игрок получает опыт
    public void GiveExperience(IPlayerData playerData, int xp)
    {
        if (!IsServer) return;

        playerData.ModifyContainerOnServer<PlayerStatsData>(stats =>
        {
            bool leveledUp = stats.AddExperience(xp);

            if (leveledUp)
            {
                Debug.Log($"LEVEL UP! Новый уровень: {stats.Level}");
                OnPlayerLevelUp(playerData, stats.Level);
            }
        });
    }

    private void OnPlayerDied(IPlayerData playerData) { /* ... */ }
    private void OnPlayerLevelUp(IPlayerData playerData, int newLevel) { /* ... */ }
}
```

### Пример 4: UI отображение данных

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players.TypedData;
using SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class PlayerUI : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;

    [SerializeField] private Text _goldText;
    [SerializeField] private Text _gemsText;
    [SerializeField] private Text _levelText;
    [SerializeField] private Slider _healthBar;
    [SerializeField] private Slider _xpBar;

    private IPlayerData _localPlayerData;

    private void Start()
    {
        // Получаем данные локального игрока
        int localId = _playersController.LocalPlayerId;
        if (_playersController.TryGetPlayerData(localId, out _localPlayerData))
        {
            // Подписываемся на изменения
            _localPlayerData.OnChanged += OnPlayerDataChanged;

            // Обновляем UI
            UpdateUI();
        }
    }

    private void OnPlayerDataChanged(IPlayerData playerData)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Обновляем инвентарь
        if (_localPlayerData.TryGetContainer(out PlayerInventoryData inventory))
        {
            _goldText.text = $"Золото: {inventory.Gold}";
            _gemsText.text = $"Гемы: {inventory.Gems}";
        }

        // Обновляем статистику
        if (_localPlayerData.TryGetContainer(out PlayerStatsData stats))
        {
            _levelText.text = $"Уровень: {stats.Level}";
            _healthBar.value = stats.GetHealthPercent();

            float xpPercent = (float)stats.Experience / stats.ExperienceToNextLevel;
            _xpBar.value = xpPercent;
        }
    }

    private void OnDestroy()
    {
        if (_localPlayerData != null)
            _localPlayerData.OnChanged -= OnPlayerDataChanged;
    }
}
```

### Пример 5: Настройки игрока

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players.TypedData;
using SkillcadeSDK.FishNetAdapter.Players.TypedData.Examples;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Slider _soundVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sensitivitySlider;
    [SerializeField] private Toggle _friendRequestsToggle;

    private IPlayerData _localPlayerData;

    private void Start()
    {
        // Загружаем настройки
        LoadSettings();

        // Подписываемся на изменения UI
        _soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        _friendRequestsToggle.onValueChanged.AddListener(OnFriendRequestsToggled);
    }

    private void LoadSettings()
    {
        if (_localPlayerData.TryGetContainer(out PlayerPreferencesData prefs))
        {
            _soundVolumeSlider.value = prefs.SoundVolume;
            _musicVolumeSlider.value = prefs.MusicVolume;
            _sensitivitySlider.value = prefs.MouseSensitivity;
            _friendRequestsToggle.isOn = prefs.AllowFriendRequests;
        }
    }

    private void OnSoundVolumeChanged(float value)
    {
        // CLIENT: Обновляем настройки
        _localPlayerData.ModifyContainerFromClient<PlayerPreferencesData>(prefs =>
        {
            prefs.SoundVolume = value;
        });

        // Применяем локально
        AudioListener.volume = value;
    }

    private void OnMusicVolumeChanged(float value)
    {
        _localPlayerData.ModifyContainerFromClient<PlayerPreferencesData>(prefs =>
        {
            prefs.MusicVolume = value;
        });
    }

    private void OnSensitivityChanged(float value)
    {
        _localPlayerData.ModifyContainerFromClient<PlayerPreferencesData>(prefs =>
        {
            prefs.MouseSensitivity = value;
        });
    }

    private void OnFriendRequestsToggled(bool value)
    {
        _localPlayerData.ModifyContainerFromClient<PlayerPreferencesData>(prefs =>
        {
            prefs.AllowFriendRequests = value;
        });
    }
}
```

---

## 📚 Справочник методов

### Запись данных

#### SetContainerFromClient<T>(container)
Устанавливает контейнер с клиента (для локального игрока).
```csharp
var inventory = new PlayerInventoryData { Gold = 100 };
localPlayerData.SetContainerFromClient(inventory);
```

#### SetContainerOnServer<T>(container)
Устанавливает контейнер на сервере. ⚠️ Только на сервере!
```csharp
var inventory = new PlayerInventoryData { Gold = 100 };
playerData.SetContainerOnServer(inventory);
```

#### ModifyContainerFromClient<T>(modifier)
Модифицирует существующий контейнер на клиенте.
```csharp
localPlayerData.ModifyContainerFromClient<PlayerInventoryData>(inv =>
{
    inv.Gold += 10;
    inv.Items.Add("Potion");
});
```

#### ModifyContainerOnServer<T>(modifier)
Модифицирует существующий контейнер на сервере. ⚠️ Только на сервере!
```csharp
playerData.ModifyContainerOnServer<PlayerStatsData>(stats =>
{
    stats.CurrentHealth -= 10;
});
```

### Чтение данных

#### TryGetContainer<T>(out container)
Пытается получить контейнер. Возвращает true при успехе.
```csharp
if (playerData.TryGetContainer(out PlayerInventoryData inventory))
{
    Debug.Log($"Золото: {inventory.Gold}");
}
```

#### GetContainer<T>()
Возвращает контейнер или null.
```csharp
var inventory = playerData.GetContainer<PlayerInventoryData>();
if (inventory != null)
{
    Debug.Log($"Золото: {inventory.Gold}");
}
```

#### HasContainer<T>()
Проверяет наличие контейнера.
```csharp
if (playerData.HasContainer<PlayerInventoryData>())
{
    Debug.Log("У игрока есть инвентарь");
}
```

---

## ⚠️ Важные правила

### 1. Класс должен быть [Serializable]
```csharp
[Serializable] // ОБЯЗАТЕЛЬНО!
public class MyData : PlayerDataContainer { }
```

### 2. DataKey должен быть уникальным
```csharp
public override string DataKey => "MyUniqueKey"; // Не дублируйте!
```

### 3. Поля должны быть public или [SerializeField]
```csharp
public int Gold; // ✅ Будет синхронизироваться
private int Gems; // ❌ НЕ будет синхронизироваться

[SerializeField] private int Diamonds; // ✅ Будет синхронизироваться
```

### 4. Поддерживаемые типы данных
- ✅ Примитивы: `int`, `float`, `bool`, `string`
- ✅ Структуры Unity: `Vector3`, `Quaternion`, `Color`
- ✅ Коллекции: `List<T>`, `Dictionary<K,V>` (где T, K, V - поддерживаемые типы)
- ✅ Свои классы/структуры с `[Serializable]`
- ❌ `GameObject`, `MonoBehaviour`, делегаты, события

### 5. CLIENT vs SERVER
```csharp
// CLIENT: Только для своего игрока
localPlayerData.SetContainerFromClient(data);
localPlayerData.ModifyContainerFromClient<T>(d => { });

// SERVER: Для любого игрока
playerData.SetContainerOnServer(data);
playerData.ModifyContainerOnServer<T>(d => { });

// CLIENT & SERVER: Чтение доступно везде
playerData.TryGetContainer(out data);
```

---

## 🎓 Шпаргалка для junior разработчиков

### Как создать свой контейнер?
1. Создайте класс, наследующий `PlayerDataContainer`
2. Добавьте атрибут `[Serializable]`
3. Переопределите `DataKey` с уникальным значением
4. Добавьте public поля для ваших данных
5. Готово!

### Когда использовать Modify вместо Set?
- **Set** - когда создаете новые данные с нуля
- **Modify** - когда хотите изменить существующие данные

```csharp
// Set - создаем с нуля
playerData.SetContainerOnServer(new PlayerInventoryData { Gold = 0 });

// Modify - меняем существующее
playerData.ModifyContainerOnServer<PlayerInventoryData>(inv => inv.Gold += 10);
```

### Как добавить helper методы?
Добавляйте их прямо в контейнер!

```csharp
[Serializable]
public class PlayerInventoryData : PlayerDataContainer
{
    public int Gold;

    // Helper метод
    public bool CanAfford(int price)
    {
        return Gold >= price;
    }

    // Helper метод
    public void AddGold(int amount)
    {
        Gold += amount;
    }
}

// Использование
if (inventory.CanAfford(100))
{
    inventory.AddGold(-100);
}
```

---

## 💡 Преимущества типизированных контейнеров

✅ **Типобезопасность** - компилятор проверяет типы
✅ **IntelliSense** - автодополнение работает
✅ **Меньше кода** - один вызов вместо множества
✅ **Группировка данных** - логически связанные данные вместе
✅ **Helper методы** - логика прямо в контейнере
✅ **Легко расширять** - добавили поле = готово
✅ **Читаемость** - понятно что за данные

---

## 🔥 Лучшие практики

### 1. Группируйте связанные данные
```csharp
// ✅ ХОРОШО - все данные инвентаря в одном контейнере
class PlayerInventoryData {
    int Gold, Gems;
    List<string> Items;
}

// ❌ ПЛОХО - разные контейнеры для связанных данных
class GoldData { int Gold; }
class GemsData { int Gems; }
class ItemsData { List<string> Items; }
```

### 2. Добавляйте методы валидации
```csharp
[Serializable]
public class PlayerStatsData : PlayerDataContainer
{
    public float Health;
    public float MaxHealth;

    // Валидация
    public void Validate()
    {
        Health = Mathf.Clamp(Health, 0, MaxHealth);
    }
}
```

### 3. Используйте static методы создания
```csharp
public static PlayerInventoryData CreateDefault()
{
    return new PlayerInventoryData
    {
        Gold = 0,
        Gems = 0,
        Items = new List<string>()
    };
}
```

### 4. Документируйте поля
```csharp
/// <summary>
/// Количество золота у игрока (мягкая валюта)
/// </summary>
public int Gold;
```

---

**Готово! Теперь вы можете создавать свои типизированные контейнеры данных! 🚀**
