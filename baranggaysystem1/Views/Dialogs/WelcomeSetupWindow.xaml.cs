using System.Windows;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class WelcomeSetupWindow : Window
{
    private const string FirstLaunchKey = "first_launch_completed";

    public WelcomeSetupWindow()
    {
        InitializeComponent();
    }

    private void BtnGetStarted_Click(object sender, RoutedEventArgs e)
    {
        if (chkDontShowAgain.IsChecked == true)
        {
            MarkFirstLaunchCompleted();
        }
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Returns true if this is the very first launch (welcome screen should show).
    /// </summary>
    public static bool ShouldShow()
    {
        try
        {
            SystemConfigService.EnsureTable();
            string value = SystemConfigService.Get(FirstLaunchKey, "");
            return string.IsNullOrWhiteSpace(value) || value != "true";
        }
        catch
        {
            // If we can't read config, show the welcome
            return true;
        }
    }

    private static void MarkFirstLaunchCompleted()
    {
        try
        {
            SystemConfigService.EnsureTable();
            SystemConfigService.Set(FirstLaunchKey, "true");
        }
        catch
        {
            // Non-critical - we just won't suppress next time
        }
    }
}
