using System.Collections.ObjectModel;
using System.Diagnostics;
using StudentCabinet_LB1.Models;

namespace StudentCabinet_LB1.Services
{
    public interface IAppStateService
    {
        DateTime SessionStartedAt { get; }
        int BackgroundCount { get; set; }

        // Кеш для UI. Джерело істини — база даних SQLite
        ObservableCollection<Student> Students { get; }

        void SaveDraft(string fullName, string group, double averageScore);
        (string FullName, string Group, double AverageScore) LoadDraft();
        void ClearDraft();

        ObservableCollection<string> LifecycleLog { get; }
        void Log(string message);
    }

    public class AppStateService : IAppStateService
    {
        private const string DraftNameKey = "draft_fullname";
        private const string DraftGroupKey = "draft_group";
        private const string DraftScoreKey = "draft_score";

        public DateTime SessionStartedAt { get; } = DateTime.Now;
        public int BackgroundCount { get; set; }

        public ObservableCollection<Student> Students { get; } = new();
        public ObservableCollection<string> LifecycleLog { get; } = new();

        public void Log(string message)
        {
            var line = $"{DateTime.Now:HH:mm:ss}  {message}";
            Debug.WriteLine($"[Lifecycle] {line}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LifecycleLog.Insert(0, line);
                while (LifecycleLog.Count > 50)
                    LifecycleLog.RemoveAt(LifecycleLog.Count - 1);
            });
        }

        public void SaveDraft(string fullName, string group, double averageScore)
        {
            Preferences.Set(DraftNameKey, fullName ?? string.Empty);
            Preferences.Set(DraftGroupKey, group ?? string.Empty);
            Preferences.Set(DraftScoreKey, averageScore);
        }

        public (string FullName, string Group, double AverageScore) LoadDraft() =>
            (Preferences.Get(DraftNameKey, string.Empty),
             Preferences.Get(DraftGroupKey, string.Empty),
             Preferences.Get(DraftScoreKey, 0.0));

        public void ClearDraft()
        {
            Preferences.Remove(DraftNameKey);
            Preferences.Remove(DraftGroupKey);
            Preferences.Remove(DraftScoreKey);
        }
    }
}