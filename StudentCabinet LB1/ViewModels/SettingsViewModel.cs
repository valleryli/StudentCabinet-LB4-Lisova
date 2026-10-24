using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StudentCabinet_LB1.Services;
using StudentCabinet_LB1.Data;


namespace StudentCabinet_LB1.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IThemeService _theme;
        private readonly IAppStateService _state;
        private readonly IDeepLinkService _deepLinks;

        public SettingsViewModel(IThemeService theme, IAppStateService state, IDeepLinkService deepLinks)
        {
            _theme = theme;
            _state = state;
            _deepLinks = deepLinks;

            TestDeepLinkCommand = new Command(TestDeepLink);
        }

        public bool IsDarkTheme
        {
            get => _theme.IsDark;
            set
            {
                if (_theme.IsDark != value)
                {
                    _theme.SetDark(value);
                    OnPropertyChanged();
                    _state.Log($"Тему змінено: {(value ? "темна" : "світла")}");
                }
            }
        }

        public ObservableCollection<string> LifecycleLog => _state.LifecycleLog;

        public string SessionInfo =>
            $"Сесію розпочато о {_state.SessionStartedAt:HH:mm:ss} · згортань: {_state.BackgroundCount}";
        public string DatabasePath => DatabaseConstants.FullPath;
        public string DeepLinkHint => "studentcabinet://student/{Id або ПІБ студента}";

        public ICommand TestDeepLinkCommand { get; }

        // Імітує зовнішнє посилання на першого студента зі списку
        private void TestDeepLink()
        {
            var student = _state.Students.FirstOrDefault();
            if (student is null)
            {
                _state.Log("Deep link (тест): спочатку додайте студента");
                return;
            }

            _deepLinks.Receive(new Uri($"studentcabinet://student/{student.Id}"));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}