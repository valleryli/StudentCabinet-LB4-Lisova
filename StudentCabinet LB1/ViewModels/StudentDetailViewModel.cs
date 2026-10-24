using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using StudentCabinet_LB1.Data;
using StudentCabinet_LB1.Messages;
using StudentCabinet_LB1.Models;
using StudentCabinet_LB1.Services;

namespace StudentCabinet_LB1.ViewModels
{
    public class StudentDetailViewModel : IQueryAttributable, INotifyPropertyChanged
    {
        private readonly IStudentRepository _repository;
        private readonly IAppStateService _state;
        private Student? _originalStudent;

        public StudentDetailViewModel(IStudentRepository repository, IAppStateService state)
        {
            _repository = repository;
            _state = state;

            GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            SaveCommand = new Command(async () => await SaveAsync());
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set { if (_fullName != value) { _fullName = value; OnPropertyChanged(); } }
        }

        private string _group = string.Empty;
        public string Group
        {
            get => _group;
            set { if (_group != value) { _group = value; OnPropertyChanged(); } }
        }

        private double _averageScore;
        public double AverageScore
        {
            get => _averageScore;
            set
            {
                if (Math.Abs(_averageScore - value) > 0.0001)
                {
                    _averageScore = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _passportNumber = string.Empty;
        public string PassportNumber
        {
            get => _passportNumber;
            set { if (_passportNumber != value) { _passportNumber = value; OnPropertyChanged(); } }
        }

        // Те, що реально лежить у базі даних (для демонстрації шифрування)
        private string _storedCiphertext = string.Empty;
        public string StoredCiphertext
        {
            get => _storedCiphertext;
            private set { if (_storedCiphertext != value) { _storedCiphertext = value; OnPropertyChanged(); } }
        }

        public ICommand GoBackCommand { get; }
        public ICommand SaveCommand { get; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedStudent", out var value) && value is Student student)
            {
                _originalStudent = student;
                FullName = student.FullName;
                Group = student.Group;
                AverageScore = student.AverageScore;
                PassportNumber = student.PassportNumber;
                StoredCiphertext = string.IsNullOrEmpty(student.PassportNumberEncrypted)
                    ? "(порожньо)"
                    : student.PassportNumberEncrypted;
            }
        }

        // UPDATE
        private async Task SaveAsync()
        {
            if (_originalStudent is null) return;

            if (string.IsNullOrWhiteSpace(FullName))
            {
                await Shell.Current.DisplayAlert("Помилка", "ПІБ не може бути порожнім", "OK");
                return;
            }

            try
            {
                _originalStudent.FullName = FullName.Trim();
                _originalStudent.Group = Group.Trim();
                _originalStudent.AverageScore = AverageScore;
                _originalStudent.PassportNumber = PassportNumber.Trim();

                await _repository.SaveAsync(_originalStudent);   // UPDATE + запис у чергу синхронізації
                StoredCiphertext = _originalStudent.PassportNumberEncrypted;

                // Просимо список оновити рядок
                WeakReferenceMessenger.Default.Send(new StudentUpdatedMessage(_originalStudent));

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                _state.Log($"Помилка (редагування): {ex.Message}");
                await Shell.Current.DisplayAlert("Помилка", ex.Message, "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}