using StudentCabinet_LB1.Views;

namespace StudentCabinet_LB1.Services
{
    public interface IDeepLinkService
    {
        void Receive(Uri uri);
        Task ProcessPendingAsync();
    }

    public class DeepLinkService : IDeepLinkService
    {
        private readonly IAppStateService _state;
        private Uri? _pending;
        private bool _uiReady;

        public DeepLinkService(IAppStateService state) => _state = state;

        // Викликається з MainActivity (Android), коли прийшло посилання
        public void Receive(Uri uri)
        {
            _state.Log($"Deep link отримано: {uri}");
            _pending = uri;

            // Холодний старт: UI ще не готовий, посилання обробить MainPage при появі
            if (_uiReady)
                MainThread.BeginInvokeOnMainThread(async () => await ProcessPendingAsync());
        }

        public async Task ProcessPendingAsync()
        {
            _uiReady = true;
            if (_pending is null || Shell.Current is null) return;

            var uri = _pending;
            _pending = null;

            if (!string.Equals(uri.Scheme, "studentcabinet", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, "student", StringComparison.OrdinalIgnoreCase))
            {
                _state.Log("Deep link: невідомий формат, пропущено");
                return;
            }

            var key = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));

            
            var student = _state.Students.FirstOrDefault(s =>
                s.Id.ToString().Equals(key, StringComparison.OrdinalIgnoreCase) ||
                s.FullName.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (student is null)
            {
                _state.Log($"Deep link: студента \"{key}\" не знайдено");
                return;
            }

            try
            {
                await Shell.Current.GoToAsync("//MainPage");
                await Shell.Current.GoToAsync(nameof(StudentDetailPage),
                    new Dictionary<string, object> { { "SelectedStudent", student } });

                _state.Log($"Deep link: відкрито деталі \"{student.FullName}\"");
            }
            catch (Exception ex)
            {
                _state.Log($"Deep link: помилка навігації — {ex.Message}");
            }
        }
    }
}