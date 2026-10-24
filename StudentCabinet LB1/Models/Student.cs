using SQLite;

namespace StudentCabinet_LB1.Models
{
    public class Student
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;

        public double AverageScore { get; set; }

        // Відкритий текст: існує лише в пам'яті, у БД не потрапляє
        [Ignore]
        public string PassportNumber { get; set; } = string.Empty;

        // У БД зберігається лише зашифроване значення (AES-256, Base64)
        public string PassportNumberEncrypted { get; set; } = string.Empty;

        // UTC-час останньої зміни (для стратегії "останній запис перемагає")
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}