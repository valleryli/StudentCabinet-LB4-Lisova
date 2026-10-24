namespace StudentCabinet_LB1.Services
{
	public interface IThemeService
	{
		bool IsDark { get; }
		void SetDark(bool isDark);
	}

	public class ThemeService : IThemeService
	{
		private const string ThemeKey = "app_theme_is_dark";

		public bool IsDark => Preferences.Get(ThemeKey, false);

		public void SetDark(bool isDark)
		{
			Preferences.Set(ThemeKey, isDark);   // вибір зберігається між запусками

			if (Application.Current is not null)
				Application.Current.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
		}
	}
}