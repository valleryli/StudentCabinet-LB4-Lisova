using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using StudentCabinet_LB1.Data;
using StudentCabinet_LB1.Messages;
using StudentCabinet_LB1.Models;

namespace StudentCabinet_LB1.Services
{
    public static class SyncOperation
    {
        public const string Insert = "Insert";
        public const string Update = "Update";
        public const string Delete = "Delete";
    }

    public interface ISyncService
    {
        bool IsOfflineMode { get; set; }   // імітація відсутності мережі
        bool IsOnline { get; }
        int PendingCount { get; }
        string LastSyncInfo { get; }

        Task EnqueueAsync(string operation, Student student);
        Task<int> RefreshPendingCountAsync();
        Task<int> SyncAsync();
    }

    public class SyncService : ISyncService
    {
        // Заглушковий публічний REST API: приймає POST/PUT/DELETE, нічого не зберігає
        private const string ApiBaseUrl = "https://jsonplaceholder.typicode.com";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        private readonly AppDatabase _database;
        private readonly IAppStateService _state;
        private readonly SemaphoreSlim _queueLock = new(1, 1);   // короткі операції над чергою
        private readonly SemaphoreSlim _syncLock = new(1, 1);    // одна синхронізація за раз

        public SyncService(AppDatabase database, IAppStateService state)
        {
            _database = database;
            _state = state;

            // З'явилась мережа → автоматично відправляємо чергу
            Connectivity.Current.ConnectivityChanged += async (s, e) =>
            {
                if (IsOnline) await SyncAsync();
            };
        }

        public bool IsOfflineMode { get; set; }

        public bool IsOnline =>
            !IsOfflineMode && Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        public int PendingCount { get; private set; }

        public string LastSyncInfo { get; private set; } = "ще не синхронізувалось";

        // Додає зміну в чергу. Кілька змін одного запису зливаються в одну
        // (стратегія "останній запис перемагає")
        public async Task EnqueueAsync(string operation, Student student)
        {
            var db = await _database.GetConnectionAsync();
            var entityType = nameof(Student);
            var entityId = student.Id;

            await _queueLock.WaitAsync();
            try
            {
                var pending = await db.Table<SyncQueueItem>()
                    .Where(q => q.EntityType == entityType && q.EntityId == entityId)
                    .ToListAsync();

                var pendingInsert = pending.FirstOrDefault(q => q.Operation == SyncOperation.Insert);

                if (operation == SyncOperation.Delete)
                {
                    // Старі зміни цього запису більше не потрібні
                    foreach (var old in pending)
                        await db.DeleteAsync(old);

                    // Якщо Insert ще не відправлявся, сервер про запис не знає — видалення теж не потрібне
                    if (pendingInsert is null)
                        await db.InsertAsync(CreateItem(SyncOperation.Delete, student));
                }
                else
                {
                    var existing = pendingInsert ?? pending.FirstOrDefault(q => q.Operation == SyncOperation.Update);

                    if (existing is not null)
                    {
                        existing.PayloadJson = BuildPayload(student);
                        existing.CreatedAt = DateTime.UtcNow;
                        await db.UpdateAsync(existing);
                    }
                    else
                    {
                        await db.InsertAsync(CreateItem(operation, student));
                    }
                }

                PendingCount = await db.Table<SyncQueueItem>().CountAsync();
            }
            finally
            {
                _queueLock.Release();
            }

            _state.Log($"Черга: {operation} #{entityId} (у черзі: {PendingCount})");
            Notify();

            // Якщо мережа є, відправляємо одразу у фоні
            if (IsOnline)
                _ = Task.Run(() => SyncAsync());
        }

        public async Task<int> RefreshPendingCountAsync()
        {
            var db = await _database.GetConnectionAsync();
            PendingCount = await db.Table<SyncQueueItem>().CountAsync();
            Notify();
            return PendingCount;
        }

        public async Task<int> SyncAsync()
        {
            if (!IsOnline)
            {
                _state.Log("Sync: немає мережі, зміни залишаються в черзі");
                return 0;
            }

            if (!await _syncLock.WaitAsync(0))
                return 0;   // синхронізація вже виконується

            var sent = 0;
            try
            {
                var db = await _database.GetConnectionAsync();
                var queue = await db.Table<SyncQueueItem>().OrderBy(q => q.Id).ToListAsync();

                foreach (var item in queue)
                {
                    try
                    {
                        await SendAsync(item);   // HTTP-запит поза блокуванням

                        await _queueLock.WaitAsync();
                        try
                        {
                            // Видаляємо, лише якщо запис не змінили під час відправки
                            var current = await db.FindAsync<SyncQueueItem>(item.Id);
                            if (current is not null && current.PayloadJson == item.PayloadJson)
                                await db.DeleteAsync(current);
                        }
                        finally
                        {
                            _queueLock.Release();
                        }

                        sent++;
                        _state.Log($"Sync: {item.Operation} #{item.EntityId} → сервер OK");
                    }
                    catch (Exception ex)
                    {
                        item.RetryCount++;
                        await db.UpdateAsync(item);
                        _state.Log($"Sync: помилка {item.Operation} #{item.EntityId}: {ex.Message}");
                        break;   // зберігаємо порядок змін, решта чекає наступної спроби
                    }
                }

                PendingCount = await db.Table<SyncQueueItem>().CountAsync();

                if (sent > 0)
                    LastSyncInfo = $"остання синхронізація {DateTime.Now:HH:mm:ss} (відправлено: {sent})";
            }
            finally
            {
                _syncLock.Release();
            }

            Notify();
            return sent;
        }

        private static async Task SendAsync(SyncQueueItem item)
        {
            // Заглушка містить пости з id 1–100
            var postId = ((item.EntityId - 1) % 100) + 1;
            var content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json");

            using var response = item.Operation switch
            {
                SyncOperation.Insert => await Http.PostAsync($"{ApiBaseUrl}/posts", content),
                SyncOperation.Update => await Http.PutAsync($"{ApiBaseUrl}/posts/{postId}", content),
                SyncOperation.Delete => await Http.DeleteAsync($"{ApiBaseUrl}/posts/{postId}"),
                _ => throw new InvalidOperationException($"Невідома операція: {item.Operation}")
            };

            response.EnsureSuccessStatusCode();
        }

        private static SyncQueueItem CreateItem(string operation, Student student) => new()
        {
            EntityType = nameof(Student),
            EntityId = student.Id,
            Operation = operation,
            PayloadJson = BuildPayload(student)
        };

        private static string BuildPayload(Student s) => JsonSerializer.Serialize(new
        {
            id = s.Id,
            fullName = s.FullName,
            group = s.Group,
            averageScore = s.AverageScore,
            passportNumber = s.PassportNumberEncrypted,   // на сервер іде лише зашифроване значення
            updatedAt = s.UpdatedAt
        });

        private void Notify() =>
            MainThread.BeginInvokeOnMainThread(() =>
                WeakReferenceMessenger.Default.Send(new SyncStateMessage(PendingCount)));
    }
}