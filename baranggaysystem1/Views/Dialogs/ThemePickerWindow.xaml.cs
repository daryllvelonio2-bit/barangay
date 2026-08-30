using System.Windows;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

/// <summary>
/// Modal popup shown after first login to let the user pick Light or Dark mode.
/// Only shown once (controlled by system_config key 'theme_picker_shown').
/// </summary>
public partial class ThemePickerWindow : Window
{
    public ThemePickerWindow()
    {
        InitializeComponent();
    }

    private void BtnLight_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Instance.SetTheme(isDark: false);
        MarkShown();
        Close();
    }

    private void BtnDark_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Instance.SetTheme(isDark: true);
        MarkShown();
        Close();
    }

    private static void MarkShown()
    {
        try
        {
            SystemConfigService.EnsureTable();
            SystemConfigService.Set("theme_picker_shown", "true");
        }
        catch { }
    }

    /// <summary>
    /// Returns true if the picker has already been shown to this installation.
    /// </summary>
    public static bool HasBeenShown()
    {
        try
        {
            SystemConfigService.EnsureTable();
            string val = SystemConfigService.Get("theme_picker_shown", "false");
            return string.Equals(val, "true", System.StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Shows the theme picker dialog centered on the owner window, if it hasn't been shown before.
    /// </summary>
    public static void ShowIfNeeded(Window owner)
    {
        if (HasBeenShown()) return;
        var picker = new ThemePickerWindow
        {
            Owner = owner
        };
        picker.ShowDialog();
    }
}
