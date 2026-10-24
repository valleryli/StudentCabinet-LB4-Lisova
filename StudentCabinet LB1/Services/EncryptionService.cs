using System.Security.Cryptography;
using System.Text;

namespace StudentCabinet_LB1.Services
{
    public interface IEncryptionService
    {
        Task<string> EncryptAsync(string plainText);
        Task<string> DecryptAsync(string cipherText);
    }

    // AES-256-CBC, для кожного значення новий випадковий IV, ключ зберігається в SecureStorage
    public class EncryptionService : IEncryptionService
    {
        private const string KeyName = "db_encryption_key";

        private readonly SemaphoreSlim _keyLock = new(1, 1);
        private byte[]? _key;

        public async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            var key = await GetKeyAsync();

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // IV (16 байт) + шифротекст → Base64
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public async Task<string> DecryptAsync(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                var key = await GetKeyAsync();
                var all = Convert.FromBase64String(cipherText);

                var iv = new byte[16];
                Buffer.BlockCopy(all, 0, iv, 0, 16);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(all, 16, all.Length - 16);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception)
            {
                // Ключ змінився або дані пошкоджені
                return string.Empty;
            }
        }

        private async Task<byte[]> GetKeyAsync()
        {
            if (_key is not null) return _key;

            await _keyLock.WaitAsync();
            try
            {
                if (_key is not null) return _key;

                var base64 = await ReadKeyAsync();
                if (string.IsNullOrEmpty(base64))
                {
                    base64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    await WriteKeyAsync(base64);
                }

                _key = Convert.FromBase64String(base64);
                return _key;
            }
            finally
            {
                _keyLock.Release();
            }
        }

        private static async Task<string> ReadKeyAsync()
        {
            try
            {
                var secure = await SecureStorage.Default.GetAsync(KeyName);
                if (!string.IsNullOrEmpty(secure)) return secure;
            }
            catch { /* SecureStorage недоступне на цій платформі — резервний варіант нижче */ }

            return Preferences.Get(KeyName, string.Empty);
        }

        private static async Task WriteKeyAsync(string value)
        {
            try { await SecureStorage.Default.SetAsync(KeyName, value); }
            catch { Preferences.Set(KeyName, value); }   // резервний варіант (менш захищений)
        }
    }
}
