using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using StudentCabinet_LB1.Messages;
using StudentCabinet_LB1.Models;
using StudentCabinet_LB1.Views;

namespace StudentCabinet_LB1.ViewModels
{
    public class StudentViewModel : INotifyPropertyChanged
    {
        private Student _student = new();

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

        public string Greeting => $"Студент: {FullName}, група {Group}";
        public bool IsHighScore => AverageScore >= 4.0;

        public ObservableCollection<Student> Students { get; } = new();

        public ICommand AddStudentCommand { get; }
        public ICommand OpenDetailsCommand { get; }

        public StudentViewModel()
        {
            AddStudentCommand = new Command(AddStudent, CanAddStudent);
            OpenDetailsCommand = new Command<Student>(async student => await OpenDetailsAsync(student));

            WeakReferenceMessenger.Default.Register<StudentUpdatedMessage>(this, (recipient, message) =>
            {
                var updated = message.Value;
                var index = Students.IndexOf(updated);
                if (index >= 0)
                {
                    Students[index] = updated;
                }
            });
        }

        private void AddStudent()
        {
            Students.Add(new Student
            {
                FullName = FullName,
                Group = Group,
                AverageScore = AverageScore
            });
            FullName = string.Empty;
            Group = string.Empty;
            AverageScore = 0;
        }

        private bool CanAddStudent() => !string.IsNullOrWhiteSpace(FullName);

        private async Task OpenDetailsAsync(Student? student)
        {
            if (student is null) return;

            var parameters = new Dictionary<string, object>
            {
                { "SelectedStudent", student }
            };

            await Shell.Current.GoToAsync(nameof(StudentDetailPage), parameters);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}