using System;
using System.Threading.Tasks;

namespace MentalHealthTracker.Services
{
    public class ThemeService
    {
        private bool _isDarkMode = false;
        public bool IsDarkMode => _isDarkMode;
        public event Action? OnThemeChanged;

        public Task ToggleTheme()
        {
            _isDarkMode = !_isDarkMode;
            OnThemeChanged?.Invoke();
            return Task.CompletedTask;
        }

        public Task SetTheme(bool isDark)
        {
            if (_isDarkMode != isDark)
            {
                _isDarkMode = isDark;
                OnThemeChanged?.Invoke();
            }
            return Task.CompletedTask;
        }
    }
} 