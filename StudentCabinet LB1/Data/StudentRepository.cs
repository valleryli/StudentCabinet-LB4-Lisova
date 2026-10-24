using StudentCabinet_LB1.Models;
using StudentCabinet_LB1.Services;

namespace StudentCabinet_LB1.Data
{
    public interface IStudentRepository
    {
        Task<List<Student>> GetAllAsync();
        Task<Student?> GetByIdAsync(int id);
        Task<int> SaveAsync(Student student);
        Task<int> DeleteAsync(Student student);
    }

    public class StudentRepository : IStudentRepository
    {
        private readonly AppDatabase _database;
        private readonly IEncryptionService _crypto;
        private readonly ISyncService _sync;

        public StudentRepository(AppDatabase database, IEncryptionService crypto, ISyncService sync)
        {
            _database = database;
            _crypto = crypto;
            _sync = sync;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            var db = await _database.GetConnectionAsync();
            var list = await db.Table<Student>().ToListAsync();

            foreach (var student in list)
                student.PassportNumber = await _crypto.DecryptAsync(student.PassportNumberEncrypted);

            return list;
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            var db = await _database.GetConnectionAsync();
            var student = await db.FindAsync<Student>(id);

            if (student is not null)
                student.PassportNumber = await _crypto.DecryptAsync(student.PassportNumberEncrypted);

            return student;
        }

        // Id == 0 → INSERT, інакше UPDATE. Кожна зміна потрапляє в чергу синхронізації
        public async Task<int> SaveAsync(Student student)
        {
            var db = await _database.GetConnectionAsync();

            student.PassportNumberEncrypted = await _crypto.EncryptAsync(student.PassportNumber);
            student.UpdatedAt = DateTime.UtcNow;

            var isNew = student.Id == 0;
            var rows = isNew ? await db.InsertAsync(student) : await db.UpdateAsync(student);

            await _sync.EnqueueAsync(isNew ? SyncOperation.Insert : SyncOperation.Update, student);
            return rows;
        }

        public async Task<int> DeleteAsync(Student student)
        {
            var db = await _database.GetConnectionAsync();
            var rows = await db.DeleteAsync(student);

            await _sync.EnqueueAsync(SyncOperation.Delete, student);
            return rows;
        }
    }
}