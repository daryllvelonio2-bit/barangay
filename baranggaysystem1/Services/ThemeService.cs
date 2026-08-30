using System;
using System.Windows;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Manages light/dark theme switching at runtime.
/// Swaps the theme ResourceDictionary in Application.Current.Resources.
/// 
/// Usage:
///   ThemeService.Instance.ToggleTheme();
///   ThemeService.Instance.SetTheme(isDark: true);
///   bool isDark = ThemeService.Instance.IsDarkMode;
/// </summary>
public sealed class ThemeService
{
    private const string LightThemeUri = "themes/LightTheme.xaml";
    private const string DarkThemeUri = "themes/DarkTheme.xaml";
    private const string ConfigKey = "app_theme_mode";

    private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
    public static ThemeService Instance => _instance.Value;

    private ResourceDictionary? _currentThemeDictionary;

    public bool IsDarkMode { get; private set; }

    public event Action<bool>? ThemeChanged;

    private ThemeService()
    {
    }

    /// <summary>
    /// Initialize theme on app startup. Call once from App.xaml.cs or MainWindow.
    /// </summary>
    public void Initialize()
    {
        bool savedDark = LoadSavedPreference();
        ApplyTheme(savedDark, savePreference: false);
    }

    /// <summary>
    /// Toggle between light and dark mode.
    /// </summary>
    public void ToggleTheme()
    {
        SetTheme(!IsDarkMode);
    }

    /// <summary>
    /// Set a specific theme mode.
    /// </summary>
    public void SetTheme(bool isDark)
    {
        if (isDark == IsDarkMode) return;
        ApplyTheme(isDark, savePreference: true);
    }

    private void ApplyTheme(bool isDark, bool savePreference)
    {
        try
        {
            string uri = isDark ? DarkThemeUri : LightThemeUri;
            var newDict = new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Relative)
            };

            var appResources = Application.Current.Resources.MergedDictionaries;

            // Remove old theme dictionary if present
            if (_currentThemeDictionary != null)
            {
                appResources.Remove(_currentThemeDictionary);
            }

            // Add new theme dictionary
            appResources.Add(newDict);
            _currentThemeDictionary = newDict;
            IsDarkMode = isDark;

            if (savePreference)
            {
                SavePreference(isDark);
            }

            ThemeChanged?.Invoke(isDark);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to apply theme.", ex);
        }
    }

    private static bool LoadSavedPreference()
    {
        try
        {
            SystemConfigService.EnsureTable();
            string value = SystemConfigService.Get(ConfigKey, "light");
            return string.Equals(value, "dark", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void SavePreference(bool isDark)
    {
        try
        {
            SystemConfigService.EnsureTable();
            SystemConfigService.Set(ConfigKey, isDark ? "dark" : "light");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to save theme preference.", ex);
        }
    }
}
