# 🔄 Альтернативные подходы к синхронизации данных игрока

## 📊 Обзор подходов

Я проанализировал разные способы синхронизации данных и создал несколько решений. Вот что доступно:

| Подход | Сложность | Типобезопасность | Гибкость | Для кого |
|--------|-----------|------------------|----------|----------|
| **1. Extension методы** | ⭐ Простой | ✅ Частичная | 🟡 Средняя | Junior |
| **2. Typed Containers** | ⭐⭐ Средний | ✅ Полная | ✅ Высокая | **Рекомендуется** |
| **3. Partial Classes** | ⭐⭐ Средний | ✅ Полная | 🟡 Средняя | Middle |
| **4. Source Generators** | ⭐⭐⭐ Сложный | ✅ Полная | ✅ Высокая | Senior |
| **5. Custom SyncTypes** | ⭐⭐⭐ Сложный | ✅ Полная | ✅ Высокая | Senior |

---

## Подход 1: Extension методы (уже реализован)

### ✅ Плюсы
- Очень просто использовать
- Не нужно менять существующий код
- Хорошо для стандартных полей

### ❌ Минусы
- Нужно вручную добавлять методы для каждого поля
- Нет группировки связанных данных
- Много повторяющегося кода

### Пример
```csharp
// PlayerDataExtensions.cs
public static void SetReadyFromClient(this IPlayerData playerData, bool isReady)
{
    playerData.SetDataOnLocalClient(PlayerDataConst.IsReady, isReady);
}

public static bool IsReady(this IPlayerData playerData)
{
    return playerData.TryGetData(PlayerDataConst.IsReady, out bool ready) && ready;
}

// Использование
playerData.SetReadyFromClient(true);
bool ready = playerData.IsReady();
```

**Когда использовать:** Для простых флагов и примитивных значений (IsReady, MatchId, etc.)

---

## Подход 2: Typed Containers (уже реализован) ⭐ РЕКОМЕНДУЕТСЯ

### ✅ Плюсы
- **Полная типобезопасность** - компилятор проверяет все
- **Группировка данных** - связанные данные в одном месте
- **Helper методы** - логика прямо в контейнере
- **Легко расширять** - добавил поле = готово
- **IntelliSense работает** - автодополнение для всех полей

### ❌ Минусы
- Нужно создавать классы контейнеров
- Весь контейнер синхронизируется целиком (но это обычно ОК)

### Пример
```csharp
// 1. Создаем контейнер
[Serializable]
public class PlayerInventoryData : PlayerDataContainer
{
    public override string DataKey => "PlayerInventory";

    public int Gold;
    public int Gems;
    public List<string> Items = new();

    // Helper методы
    public bool CanAfford(int price) => Gold >= price;
    public void AddGold(int amount) => Gold += amount;
}

// 2. Используем
var inventory = new PlayerInventoryData
{
    Gold = 100,
    Gems = 50,
    Items = new List<string> { "Sword", "Shield" }
};
playerData.SetContainerOnServer(inventory);

// 3. Читаем
if (playerData.TryGetContainer(out PlayerInventoryData inv))
{
    Debug.Log($"Золото: {inv.Gold}"); // IntelliSense работает!
    if (inv.CanAfford(50))
        inv.AddGold(-50);
}

// 4. Модифицируем
playerData.ModifyContainerOnServer<PlayerInventoryData>(inv =>
{
    inv.Gold += 10;
    inv.Items.Add("Potion");
});
```

**Когда использовать:** Для любых сложных данных - инвентарь, статистика, настройки, прогресс.

**📚 Подробное руководство:** [TYPED_DATA_GUIDE.md](./TYPED_DATA_GUIDE.md)

---

## Подход 3: Partial Classes

Расширяем `FishNetPlayerData` через partial классы для добавления строго типизированных свойств.

### ✅ Плюсы
- Работает как обычные свойства класса
- Полная типобезопасность
- Можно использовать в выражениях LINQ

### ❌ Минусы
- Нужно менять код для каждого нового поля
- Разбросан по разным файлам
- Сложнее поддерживать

### Реализация
```csharp
// FishNetPlayerData.Inventory.cs (partial class)
namespace SkillcadeSDK.FishNetAdapter.Players
{
    public partial class FishNetPlayerData
    {
        // Типизированное свойство для Gold
        public int Gold
        {
            get => TryGetData("Gold", out int value) ? value : 0;
            set => IsServerInitialized
                ? SetDataOnServer("Gold", value)
                : SetDataOnLocalClient("Gold", value);
        }

        // Типизированное свойство для списка Items
        public List<string> Items
        {
            get => TryGetData("Items", out List<string> value)
                ? value
                : new List<string>();
            set => IsServerInitialized
                ? SetDataOnServer("Items", value)
                : SetDataOnLocalClient("Items", value);
        }
    }
}

// Использование - как обычные свойства!
playerData.Gold = 100;
playerData.Gold += 50;

if (playerData.Gold >= 100)
{
    playerData.Items.Add("Sword");
}
```

**Когда использовать:** Когда нужен доступ к данным как к свойствам класса.

---

## Подход 4: Source Generators (продвинутый)

Автоматическая генерация кода на основе атрибутов.

### ✅ Плюсы
- Минимум ручного кода
- Автогенерация extension методов
- Полная типобезопасность
- Нет runtime overhead

### ❌ Минусы
- Требует настройки Source Generators
- Сложнее отлаживать
- Requires .NET Standard 2.0+

### Концепция
```csharp
// 1. Определяем схему данных
[PlayerData]
public partial class PlayerDataSchema
{
    [SyncedField("IsReady")]
    public bool IsReady { get; set; }

    [SyncedField("Gold")]
    public int Gold { get; set; }

    [SyncedField("Items")]
    public List<string> Items { get; set; }
}

// 2. Source Generator автоматически создает extension методы:
// playerData.SetIsReady(true)
// playerData.GetIsReady()
// playerData.SetGold(100)
// playerData.GetGold()
// И т.д.

// 3. Использование
playerData.SetGold(100);
int gold = playerData.GetGold();
```

**Когда использовать:** В больших проектах, где много различных типов данных.

**Примечание:** Требует создания Source Generator проекта.

---

## Подход 5: Custom SyncTypes (FishNet native)

Использование специализированных `SyncVar<T>` для каждого поля вместо `SyncDictionary`.

### ✅ Плюсы
- Максимальная производительность
- Точный контроль синхронизации
- События изменения для каждого поля

### ❌ Минусы
- Требует изменения базового класса FishNetPlayerData
- Менее гибкий (нужно знать все поля заранее)
- Больше boilerplate кода

### Реализация
```csharp
public class StronglyTypedPlayerData : NetworkBehaviour, IPlayerData
{
    // Каждое поле - отдельный SyncVar
    private readonly SyncVar<int> _gold = new(
        new SyncTypeSettings(WritePermission.ServerOnly));

    private readonly SyncVar<int> _gems = new(
        new SyncTypeSettings(WritePermission.ServerOnly));

    private readonly SyncList<string> _items = new(
        new SyncTypeSettings(WritePermission.ServerOnly));

    // Типизированные свойства
    public int Gold
    {
        get => _gold.Value;
        set => _gold.Value = value;
    }

    public int Gems
    {
        get => _gems.Value;
        set => _gems.Value = value;
    }

    public SyncList<string> Items => _items;

    // События для каждого поля
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _gold.OnChange += OnGoldChanged;
        _gems.OnChange += OnGemsChanged;
        _items.OnChange += OnItemsChanged;
    }

    private void OnGoldChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"Золото изменилось: {prev} -> {next}");
    }
}

// Использование - как обычные свойства
playerData.Gold = 100;
playerData.Gems = 50;
playerData.Items.Add("Sword");
```

**Когда использовать:** Когда нужна максимальная производительность и известны все поля заранее.

---

## 📊 Сравнение производительности

| Подход | Размер данных | Частота обновлений | Сетевой трафик |
|--------|---------------|-------------------|----------------|
| Extension методы | Минимальный | ⚡ Быстро | Только измененные поля |
| Typed Containers | Средний | 🟡 Средне | Весь контейнер |
| Partial Classes | Минимальный | ⚡ Быстро | Только измененные поля |
| Source Generators | Минимальный | ⚡ Быстро | Только измененные поля |
| Custom SyncTypes | Минимальный | ⚡⚡ Очень быстро | Только измененные поля |

---

## 🎯 Рекомендации по выбору

### Для начинающих (Junior)
**Используйте: Extension методы + Typed Containers**

```csharp
// Простые флаги - Extension методы
playerData.SetReadyFromClient(true);
playerData.SetInGameOnServer(true);

// Сложные данные - Typed Containers
var inventory = new PlayerInventoryData { Gold = 100 };
playerData.SetContainerOnServer(inventory);
```

### Для средних разработчиков (Middle)
**Используйте: Typed Containers + Partial Classes**

```csharp
// Группы данных - Typed Containers
playerData.SetContainerOnServer(new PlayerInventoryData { ... });

// Частые обновления - Partial Classes
playerData.Gold += 10; // Более эффективно для частых изменений
```

### Для опытных (Senior)
**Выбирайте на основе требований:**
- **Много типов данных?** → Source Generators
- **Высокая нагрузка?** → Custom SyncTypes
- **Гибкость важнее?** → Typed Containers

---

## 💡 Комбинированный подход (BEST PRACTICE)

Можно комбинировать разные подходы для разных типов данных!

```csharp
public class PlayerDataUsage : MonoBehaviour
{
    private IPlayerData _playerData;

    void Example()
    {
        // 1. Простые флаги - Extension методы
        _playerData.SetReadyFromClient(true);
        _playerData.SetInGameOnServer(true);

        // 2. Сложные структуры - Typed Containers
        var inventory = new PlayerInventoryData
        {
            Gold = 100,
            Items = new List<string> { "Sword" }
        };
        _playerData.SetContainerOnServer(inventory);

        // 3. Статистика - Typed Container с модификацией
        _playerData.ModifyContainerOnServer<PlayerStatsData>(stats =>
        {
            stats.TakeDamage(10);
            if (stats.AddExperience(50))
                Debug.Log("Level up!");
        });

        // 4. Частые обновления - Partial Properties (если реализовано)
        // _playerData.CurrentHealth -= 10;
    }
}
```

---

## 🚀 Что уже реализовано в этом проекте

### ✅ Готово к использованию

1. **Extension методы** (`PlayerDataExtensions.cs`)
   - SetReadyFromClient/OnServer
   - IsReady, IsInGame
   - Get/Set MatchId, UserId
   - И другие...

2. **PlayersHelper** (`PlayersHelper.cs`)
   - GetReadyPlayersCount
   - AreAllPlayersReady
   - SetReadyPlayersInGame
   - И другие...

3. **Typed Containers** (новое!)
   - `IPlayerDataContainer` - базовый интерфейс
   - `PlayerDataContainerExtensions` - методы работы с контейнерами
   - Примеры: `PlayerInventoryData`, `PlayerStatsData`, `PlayerPreferencesData`

### 📚 Документация

- [PLAYER_SYNC_GUIDE.md](./PLAYER_SYNC_GUIDE.md) - Руководство по extension методам
- [TYPED_DATA_GUIDE.md](./TYPED_DATA_GUIDE.md) - Руководство по typed containers
- [IMPROVEMENTS_SUMMARY.md](./IMPROVEMENTS_SUMMARY.md) - Анализ улучшений

---

## 🎓 Что выбрать для вашего проекта?

### Маленький проект (1-2 разработчика)
```
✅ Extension методы + Typed Containers
```

### Средний проект (3-5 разработчиков)
```
✅ Typed Containers + PlayersHelper
Опционально: Partial Classes для часто изменяемых данных
```

### Большой проект (5+ разработчиков)
```
✅ Typed Containers как основа
✅ Source Generators для автоматизации
✅ Custom SyncTypes для критичных данных
```

---

## ⚡ Быстрый старт

### Вариант 1: Простой (Extension методы)
```csharp
// Уже работает из коробки!
playerData.SetReadyFromClient(true);
bool ready = playerData.IsReady();
```

### Вариант 2: Продвинутый (Typed Containers)
```csharp
// 1. Создайте контейнер
[Serializable]
public class MyData : PlayerDataContainer
{
    public override string DataKey => "MyData";
    public int Value;
}

// 2. Используйте
var data = new MyData { Value = 100 };
playerData.SetContainerOnServer(data);

// 3. Читайте
if (playerData.TryGetContainer(out MyData myData))
    Debug.Log(myData.Value);
```

---

## 📞 Нужна помощь?

- **Extension методы:** См. [PLAYER_SYNC_GUIDE.md](./PLAYER_SYNC_GUIDE.md)
- **Typed Containers:** См. [TYPED_DATA_GUIDE.md](./TYPED_DATA_GUIDE.md)
- **Примеры кода:** Папка `Players/TypedData/Examples/`

**Рекомендация:** Начните с **Typed Containers** - это самый универсальный и удобный подход! 🚀
