# 📚 Руководство по синхронизации данных игрока

## 🎯 Основные концепции

### Что такое PlayerData?
`PlayerData` - это данные игрока, которые автоматически синхронизируются между сервером и всеми клиентами. Каждый подключенный игрок имеет свой объект `PlayerData`.

### Где выполняется код?
В многопользовательских играх код выполняется в двух местах:
- **Сервер** - главный компьютер, который управляет игрой
- **Клиент** - компьютер каждого игрока

---

## 📖 Базовые примеры

### 1. Установка готовности игрока (CLIENT)

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

public class LobbyUI : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;

    // Вызывается когда игрок нажимает кнопку "Готов"
    public void OnReadyButtonClicked()
    {
        // Получаем данные локального игрока
        int localPlayerId = _playersController.LocalPlayerId;

        if (_playersController.TryGetPlayerData(localPlayerId, out var playerData))
        {
            // ✅ НОВЫЙ СПОСОБ - просто и понятно
            playerData.SetReadyFromClient(true);

            // ❌ СТАРЫЙ СПОСОБ - сложнее
            // playerData.SetDataOnLocalClient(PlayerDataConst.IsReady, true);
        }
    }
}
```

### 2. Проверка готовности игроков (SERVER)

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

public class GameStarter : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;

    private void Update()
    {
        // Работает только на сервере
        if (!IsServer) return;

        // ✅ НОВЫЙ СПОСОБ - один вызов
        if (PlayersHelper.AreAllPlayersReady(_playersController, minPlayers: 2))
        {
            StartGame();
        }

        /* ❌ СТАРЫЙ СПОСОБ - много кода
        int readyPlayers = 0;
        int totalPlayers = 0;

        foreach (var playerData in _playersController.GetAllPlayersData())
        {
            totalPlayers++;
            if (playerData.TryGetData(PlayerDataConst.IsReady, out bool ready) && ready)
                readyPlayers++;
        }

        if (totalPlayers >= 2 && readyPlayers == totalPlayers)
        {
            StartGame();
        }
        */
    }

    private void StartGame()
    {
        Debug.Log("Все игроки готовы! Начинаем игру...");

        // Переводим готовых игроков в статус "в игре"
        PlayersHelper.SetReadyPlayersInGame(_playersController);
    }
}
```

### 3. Отображение статистики в UI (CLIENT)

```csharp
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class LobbyStatsUI : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;

    [SerializeField] private Text _statsText;

    private void Start()
    {
        // Подписываемся на изменения данных игроков
        _playersController.OnPlayerDataUpdated += OnPlayerDataChanged;
        _playersController.OnPlayerAdded += OnPlayerDataChanged;
        _playersController.OnPlayerRemoved += OnPlayerDataChanged;

        UpdateStatsDisplay();
    }

    private void OnPlayerDataChanged(int playerId, IPlayerData playerData)
    {
        UpdateStatsDisplay();
    }

    private void UpdateStatsDisplay()
    {
        // ✅ НОВЫЙ СПОСОБ - просто и понятно
        var (ready, total) = PlayersHelper.GetPlayersStats(_playersController);
        _statsText.text = $"Готово игроков: {ready}/{total}";

        // Или более детально:
        var stats = PlayersHelper.GetDetailedStats(_playersController);
        Debug.Log(stats); // Players: 4 | Ready: 2 | InGame: 0
    }
}
```

---

## 🎮 Типичные сценарии использования

### Сценарий 1: Лобби с кнопкой "Готов"

```csharp
public class LobbyManager : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;

    // CLIENT: Игрок нажал на кнопку
    public void ToggleReady()
    {
        var localPlayer = GetLocalPlayerData();

        // Проверяем текущий статус
        bool currentlyReady = localPlayer.IsReady();

        // Переключаем
        localPlayer.SetReadyFromClient(!currentlyReady);
    }

    // SERVER: Проверяем можно ли начать игру
    private void CheckCanStartGame()
    {
        if (!IsServer) return;

        // Нужно минимум 2 игрока и все должны быть готовы
        if (PlayersHelper.AreAllPlayersReady(_playersController, minPlayers: 2))
        {
            TransitionToGame();
        }
    }

    private IPlayerData GetLocalPlayerData()
    {
        _playersController.TryGetPlayerData(_playersController.LocalPlayerId, out var data);
        return data;
    }
}
```

### Сценарий 2: Старт матча и спавн игроков

```csharp
public class MatchStarter : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;
    [Inject] private readonly PlayerSpawner _playerSpawner;

    // SERVER: Запуск нового матча
    public void StartNewMatch()
    {
        if (!IsServer) return;

        // 1. Генерируем ID матча
        string matchId = System.Guid.NewGuid().ToString();

        // 2. Устанавливаем ID матча всем игрокам
        PlayersHelper.SetMatchIdForAllPlayers(_playersController, matchId);

        // 3. Переводим готовых игроков в статус "в игре"
        PlayersHelper.SetReadyPlayersInGame(_playersController);

        // 4. Спавним игроков в мире
        _playerSpawner.SpawnAllInGamePlayers();

        Debug.Log($"Матч {matchId} начался! Игроков: {PlayersHelper.GetInGamePlayersCount(_playersController)}");
    }
}
```

### Сценарий 3: Завершение матча и отправка результатов

```csharp
public class MatchFinisher : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;
    [Inject] private readonly IWebRequester _webRequester;

    // SERVER: Завершение матча
    public void FinishMatch(int winnerPlayerId)
    {
        if (!IsServer) return;

        // Получаем данные победителя
        if (!_playersController.TryGetPlayerData(winnerPlayerId, out var winnerData))
        {
            Debug.LogError($"Не найден игрок с ID {winnerPlayerId}");
            return;
        }

        // ✅ НОВЫЙ СПОСОБ - типобезопасно
        string matchId = winnerData.GetMatchId();
        string userId = winnerData.GetUserId();

        if (matchId == null || userId == null)
        {
            Debug.LogError("У победителя нет MatchId или UserId");
            return;
        }

        // Отправляем результат на бэкенд
        _webRequester.SendWinner(matchId, userId);

        // Сбрасываем статусы игроков
        PlayersHelper.ResetAllPlayersInGame(_playersController);
        PlayersHelper.ResetAllPlayersReady(_playersController);
    }
}
```

### Сценарий 4: Список игроков в лобби

```csharp
public class PlayerListUI : MonoBehaviour
{
    [Inject] private readonly IPlayersController _playersController;

    [SerializeField] private PlayerSlotUI _playerSlotPrefab;
    [SerializeField] private Transform _slotsContainer;

    private List<PlayerSlotUI> _slots = new();

    private void Start()
    {
        _playersController.OnPlayerAdded += OnPlayerJoined;
        _playersController.OnPlayerRemoved += OnPlayerLeft;
        _playersController.OnPlayerDataUpdated += OnPlayerUpdated;

        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        // Очищаем старые слоты
        foreach (var slot in _slots)
            Destroy(slot.gameObject);
        _slots.Clear();

        // Создаем слоты для каждого игрока
        foreach (var playerData in _playersController.GetAllPlayersData())
        {
            CreatePlayerSlot(playerData);
        }
    }

    private void CreatePlayerSlot(IPlayerData playerData)
    {
        var slot = Instantiate(_playerSlotPrefab, _slotsContainer);

        // ✅ НОВЫЙ СПОСОБ - удобные методы
        string userId = playerData.GetUserId() ?? "Гость";
        bool isReady = playerData.IsReady();

        slot.SetPlayerName(userId);
        slot.SetReadyStatus(isReady);

        _slots.Add(slot);
    }

    private void OnPlayerJoined(int playerId, IPlayerData playerData)
    {
        RefreshPlayerList();
    }

    private void OnPlayerLeft(int playerId, IPlayerData playerData)
    {
        RefreshPlayerList();
    }

    private void OnPlayerUpdated(int playerId, IPlayerData playerData)
    {
        RefreshPlayerList();
    }
}
```

---

## 🔍 Доступные методы

### PlayerDataExtensions (для одного игрока)

#### Готовность
```csharp
playerData.SetReadyFromClient(bool isReady);    // CLIENT: установить готовность
playerData.SetReadyOnServer(bool isReady);      // SERVER: установить готовность
playerData.IsReady();                           // Проверить готовность
```

#### Статус "В игре"
```csharp
playerData.SetInGameOnServer(bool inGame);      // SERVER: установить статус
playerData.IsInGame();                          // Проверить статус
```

#### Match ID
```csharp
playerData.SetMatchIdOnServer(string matchId);  // SERVER: установить ID матча
playerData.TryGetMatchId(out string matchId);   // Получить ID с проверкой
playerData.GetMatchId();                        // Получить ID (может быть null)
```

#### User ID
```csharp
playerData.SetUserIdFromClient(string userId);  // CLIENT: установить ID
playerData.SetUserIdOnServer(string userId);    // SERVER: установить ID
playerData.TryGetUserId(out string userId);     // Получить ID с проверкой
playerData.GetUserId();                         // Получить ID (может быть null)
```

### PlayersHelper (для коллекции игроков)

#### Готовность
```csharp
PlayersHelper.GetReadyPlayersCount(controller);              // Количество готовых
PlayersHelper.GetReadyPlayers(controller);                   // Список готовых
PlayersHelper.AreAllPlayersReady(controller, minPlayers);    // Все готовы?
PlayersHelper.ResetAllPlayersReady(controller);              // SERVER: сбросить всех
```

#### Игроки в игре
```csharp
PlayersHelper.GetInGamePlayers(controller);                  // Список в игре
PlayersHelper.GetInGamePlayersCount(controller);             // Количество в игре
PlayersHelper.SetReadyPlayersInGame(controller);             // SERVER: готовых -> в игру
PlayersHelper.ResetAllPlayersInGame(controller);             // SERVER: сбросить всех
```

#### Другое
```csharp
PlayersHelper.SetMatchIdForAllPlayers(controller, matchId);  // SERVER: установить ID всем
PlayersHelper.TryFindPlayerByUserId(controller, userId, out playerData);  // Найти по UserId
PlayersHelper.GetPlayersStats(controller);                   // Статистика (ready, total)
PlayersHelper.GetDetailedStats(controller);                  // Детальная статистика
```

---

## ⚠️ Важные правила

### 1. CLIENT vs SERVER
```csharp
// ✅ ПРАВИЛЬНО
// На клиенте - используем методы FromClient
playerData.SetReadyFromClient(true);
playerData.SetUserIdFromClient("user123");

// На сервере - используем методы OnServer
if (IsServer)
{
    playerData.SetReadyOnServer(false);
    playerData.SetInGameOnServer(true);
}

// ❌ НЕПРАВИЛЬНО
// Нельзя вызывать серверные методы на клиенте!
playerData.SetInGameOnServer(true);  // Работает только на сервере!
```

### 2. Подписка на события
```csharp
private void Start()
{
    // ✅ Подписываемся
    _playersController.OnPlayerAdded += OnPlayerJoined;
}

private void OnDestroy()
{
    // ✅ Не забываем отписываться!
    _playersController.OnPlayerAdded -= OnPlayerJoined;
}
```

### 3. Проверка null
```csharp
// ✅ ПРАВИЛЬНО - проверяем результат
if (_playersController.TryGetPlayerData(playerId, out var playerData))
{
    // Безопасно используем playerData
    Debug.Log($"Игрок готов: {playerData.IsReady()}");
}

// ❌ НЕПРАВИЛЬНО - может быть null!
var playerData = _playersController.TryGetPlayerData(playerId, out var data);
Debug.Log(playerData.IsReady());  // NullReferenceException!
```

---

## 🎓 Шпаргалка для junior разработчиков

### Когда использовать SetDataFromClient?
✅ Когда игрок что-то делает на своем компьютере:
- Нажал кнопку "Готов"
- Ввел свое имя
- Выбрал персонажа

### Когда использовать SetDataOnServer?
✅ Когда сервер управляет игрой:
- Начало матча (установка MatchId)
- Перевод игроков в статус "в игре"
- Сброс готовности всех игроков
- Завершение матча

### Как понять что код выполняется на сервере?
```csharp
if (IsServer)  // или IsServerInitialized
{
    // Этот код выполнится только на сервере
}

if (IsClient)  // или IsClientInitialized
{
    // Этот код выполнится только на клиенте
}
```

### Что делать если нужно добавить новое поле?
1. Добавьте константу в `PlayerDataConst.cs`:
```csharp
public const string PlayerLevel = "PlayerLevel";
```

2. Добавьте extension методы в `PlayerDataExtensions.cs`:
```csharp
public static void SetPlayerLevelOnServer(this IPlayerData playerData, int level)
{
    playerData.SetDataOnServer(PlayerDataConst.PlayerLevel, level);
}

public static int GetPlayerLevel(this IPlayerData playerData)
{
    playerData.TryGetData(PlayerDataConst.PlayerLevel, out int level);
    return level;
}
```

3. Готово! Теперь можно использовать:
```csharp
playerData.SetPlayerLevelOnServer(42);
int level = playerData.GetPlayerLevel();
```

---

## 📞 Нужна помощь?

Если что-то непонятно:
1. Посмотрите примеры выше
2. Используйте IntelliSense (нажмите Ctrl+Space после точки)
3. Все методы имеют XML-комментарии с описанием
4. Обратитесь к старшим разработчикам

**Удачи в разработке! 🚀**
