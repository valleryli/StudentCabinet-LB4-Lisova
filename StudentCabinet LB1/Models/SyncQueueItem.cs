using SQLite;

namespace StudentCabinet_LB1.Models
{
    public class SyncQueueItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string EntityType { get; set; } = nameof(Student);
        public int EntityId { get; set; }
        public string Operation { get; set; } = string.Empty;   // Insert, Update, Delete
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; }
    }
}