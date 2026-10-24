using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using StudentCabinet_LB1.Data;
using StudentCabinet_LB1.Messages;
using StudentCabinet_LB1.Models;
using StudentCabinet_LB1.Services;
using StudentCabinet_LB1.Views;

namespace StudentCabinet_LB1.ViewModels
{
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly IAppStateService _state;
        private readonly IDeepLinkService _deepLinks;
        private readonly IStudentRepository _repository;
        private readonly ISyncService _sync;
        private readonly Student _student = new();   // поточні значення форми (у БД не зберігається)

        public StudentViewModel(
            IAppStateService state,
            IDeepLinkService deepLinks,
            IStudentRepository repository,
            ISyncService sync)
        {
            _state = state;
            _deepLinks = deepLinks;
            _repository = repository;
            _sync = sync;

            AddStudentCommand = new Command(async () => await AddStudentAsync(), CanAddStudent);
            OpenDetailsCommand = new Command<Student>(async s => await OpenDetailsAsync(s));
            OpenSettingsCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(SettingsPage)));
            LoadCommand = new Command(async () => await LoadAsync());
            DeleteCommand = new Command<Student>(async s => await DeleteAsync(s));
            SyncNowCommand = new Command(async () => await SyncNowAsync());

            RestoreDraft();
            UpdateSyncText();

            WeakReferenceMessenger.Default.Register<StudentViewModel, StudentUpdatedMessage>(
                this, (r, m) => r.OnStudentUpdated(m.Value));
            WeakReferenceMessenger.Default.Register<StudentViewModel, AppLifecycleMessage>(
                this, (r, m) => r.OnLifecycle(m.Value));
            WeakReferenceMessenger.Default.Register<StudentViewModel, SyncStateMessage>(
                this, (r, m) => r.UpdateSyncText());
        }

        // ---------- Властивості форми ----------

        public string FullName
        {
            get => _student.FullName;
            set
            {
                if (_student.FullName != value)
                {
                    _student.FullName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Greeting));
                    (AddStudentCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public string Group
        {
            get => _student.Group;
            set
            {
                if (_student.Group != value)
                {
                    _student.Group = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Greeting));
                }
            }
        }

        public double AverageScore
        {
            get => _student.AverageScore;
            set
            {
                if (Math.Abs(_student.AverageScore - value) > 0.0001)
                {
                    _student.AverageScore = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsHighScore));
                }
            }
        }

        public string PassportNumber
        {
            get => _student.PassportNumber;
            set
            {
                if (_student.PassportNumber != value)
                {
                    _student.PassportNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Greeting => $"Студент: {FullName}, група {Group}";
        public bool IsHighScore => AverageScore >= 4.0;

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            private set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _syncText = string.Empty;
        public string SyncText
        {
            get => _syncText;
            private set
            {
                if (_syncText != value)
                {
                    _syncText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOfflineMode
        {
            get => _sync.IsOfflineMode;
            set
            {
                if (_sync.IsOfflineMode == value) return;

                _sync.IsOfflineMode = value;
                OnPropertyChanged();
                UpdateSyncText();
                _state.Log($"Режим мережі: {(value ? "офлайн (імітація)" : "онлайн")}");

                if (!value)
                    _ = SyncNowAsync();   // мережа "з'явилась" → відправляємо чергу
            }
        }

        public ObservableCollection<Student> Students => _state.Students;

        // ---------- Команди ----------

        public ICommand AddStudentCommand { get; }
        public ICommand OpenDetailsCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SyncNowCommand { get; }

        // READ: завантаження списку з SQLite через репозиторій
        private async Task LoadAsync()
        {
            try
            {
                var items = await _repository.GetAllAsync();

                Students.Clear();
                foreach (var item in items)
                    Students.Add(item);

                await _sync.RefreshPendingCountAsync();
                UpdateSyncText();

                StatusText = $"Завантажено з SQLite: {Students.Count} · {DateTime.Now:HH:mm:ss}";
                _state.Log("MainPage: OnAppearing → LoadCommand → SQLite");
            }
            catch (Exception ex)
            {
                ReportError("завантаження", ex);
            }

            await _deepLinks.ProcessPendingAsync();
        }

        // CREATE
        private async Task AddStudentAsync()
        {
            try
            {
                var student = new Student
                {
                    FullName = FullName.Trim(),
                    Group = Group.Trim(),
                    AverageScore = AverageScore,
                    PassportNumber = PassportNumber.Trim()
                };

                await _repository.SaveAsync(student);   // INSERT + запис у чергу синхронізації
                Students.Add(student);

                FullName = string.Empty;
                Group = string.Empty;
                AverageScore = 0;
                PassportNumber = string.Empty;
                _state.ClearDraft();

                StatusText = $"Додано в БД (Id = {student.Id}). Усього: {Students.Count}";
            }
            catch (Exception ex)
            {
                ReportError("додавання", ex);
            }
        }

        private bool CanAddStudent() => !string.IsNullOrWhiteSpace(FullName);

        // DELETE
        private async Task DeleteAsync(Student? student)
        {
            if (student is null) return;

            try
            {
                var confirmed = await Shell.Current.DisplayAlert(
                    "Видалення", $"Видалити «{student.FullName}»?", "Так", "Ні");
                if (!confirmed) return;

                await _repository.DeleteAsync(student);   // DELETE + запис у чергу синхронізації
                Students.Remove(student);

                StatusText = $"Видалено з БД. Усього: {Students.Count}";
            }
            catch (Exception ex)
            {
                ReportError("видалення", ex);
            }
        }

        // UPDATE виконується на сторінці деталей (StudentDetailViewModel)
        private async Task OpenDetailsAsync(Student? student)
        {
            if (student is null) return;

            var parameters = new Dictionary<string, object> { { "SelectedStudent", student } };
            await Shell.Current.GoToAsync(nameof(StudentDetailPage), parameters);
        }

        private async Task SyncNowAsync()
        {
            await _sync.SyncAsync();
            UpdateSyncText();
        }

        public void OnPageDisappearing() => _state.Log("MainPage: OnDisappearing");

        // ---------- Допоміжне ----------

        private void UpdateSyncText()
        {
            var mode = _sync.IsOnline ? "онлайн" : "офлайн";
            SyncText = $"Режим: {mode} · у черзі: {_sync.PendingCount} · {_sync.LastSyncInfo}";
        }

        private void ReportError(string action, Exception ex)
        {
            _state.Log($"Помилка ({action}): {ex.Message}");
            StatusText = $"Помилка ({action}): {ex.Message}";
        }

        private void OnLifecycle(AppLifecycleEvent evt)
        {
            switch (evt)
            {
                case AppLifecycleEvent.Stopped:
                case AppLifecycleEvent.Destroying:
                    _state.SaveDraft(FullName, Group, AverageScore);
                    _state.Log("Стан збережено (чернетка форми)");
                    break;

                case AppLifecycleEvent.Resumed:
                    RestoreDraft();
                    _state.Log("Стан відновлено (чернетка форми)");
                    _ = SyncNowAsync();   // повернулись у застосунок → пробуємо відправити чергу
                    break;
            }
        }

        private void RestoreDraft()
        {
            if (!string.IsNullOrEmpty(FullName) || !string.IsNullOrEmpty(Group) || AverageScore != 0)
                return;

            var draft = _state.LoadDraft();
            FullName = draft.FullName;
            Group = draft.Group;
            AverageScore = draft.AverageScore;
        }

        // Збереження в БД зробила сторінка деталей, тут лише оновлюємо рядок у списку
        private void OnStudentUpdated(Student updated)
        {
            var index = Students.IndexOf(updated);
            if (index >= 0)
                Students[index] = updated;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}