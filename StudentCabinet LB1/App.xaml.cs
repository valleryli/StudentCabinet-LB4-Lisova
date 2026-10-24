using CommunityToolkit.Mvvm.Messaging;
using StudentCabinet_LB1.Messages;
using StudentCabinet_LB1.Services;

namespace StudentCabinet_LB1
{
    public partial class App : Application
    {
        private readonly IAppStateService _state;

        // Сервіси підставляє DI-контейнер
        public App(IAppStateService state, IThemeService theme)
        {
            InitializeComponent();
            _state = state;

            // Застосовуємо збережену тему до всіх сторінок
            UserAppTheme = theme.IsDark ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            window.Created += (s, e) => Notify(AppLifecycleEvent.Created, "App: Created — вікно створено");
            window.Activated += (s, e) => Notify(AppLifecycleEvent.Activated, "App: Activated — на передньому плані");
            window.Deactivated += (s, e) => Notify(AppLifecycleEvent.Deactivated, "App: Deactivated — втрачено фокус");
            window.Stopped += (s, e) =>
            {
                _state.BackgroundCount++;
                Notify(AppLifecycleEvent.Stopped, "App: Stopped — згорнуто у фон");
            };
            window.Resumed += (s, e) => Notify(AppLifecycleEvent.Resumed, "App: Resumed — повернення з фону");
            window.Destroying += (s, e) => Notify(AppLifecycleEvent.Destroying, "App: Destroying — завершення роботи");

            return window;
        }

        private void Notify(AppLifecycleEvent evt, string text)
        {
            _state.Log(text);
            WeakReferenceMessenger.Default.Send(new AppLifecycleMessage(evt));
        }
    }
}