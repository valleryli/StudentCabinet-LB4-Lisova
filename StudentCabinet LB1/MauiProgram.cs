using Microsoft.Extensions.Logging;
using StudentCabinet_LB1.Services;
using StudentCabinet_LB1.ViewModels;
using StudentCabinet_LB1.Views;
using StudentCabinet_LB1.Data;

namespace StudentCabinet_LB1
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Сервіси: один екземпляр на весь застосунок
            builder.Services.AddSingleton<IAppStateService, AppStateService>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();

            // ViewModel'і та сторінки: новий екземпляр при кожному відкритті
            builder.Services.AddTransient<StudentViewModel>();
            builder.Services.AddTransient<StudentDetailViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StudentDetailPage>();
            builder.Services.AddTransient<SettingsPage>();

            builder.Services.AddSingleton<AppDatabase>();
            builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
            builder.Services.AddSingleton<ISyncService, SyncService>();
            builder.Services.AddSingleton<IStudentRepository, StudentRepository>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}