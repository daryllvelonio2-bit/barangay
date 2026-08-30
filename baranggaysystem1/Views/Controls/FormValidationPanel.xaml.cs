using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Inline form validation feedback panel.
/// Provides consistent error/success/info display for all forms.
/// </summary>
public partial class FormValidationPanel : UserControl
{
    public FormValidationPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Show validation errors.
    /// </summary>
    public void ShowErrors(IEnumerable<string> errors)
    {
        var errorList2 = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList()
            ?? new List<string>();

        if (errorList2.Count == 0)
        {
            Clear();
            return;
        }

        ApplyStyle(ValidationStyle.Error);
        headerText.Text = errorList2.Count == 1 ? "Validation Error" : $"Please fix {errorList2.Count} issue(s):";
        headerText.Visibility = Visibility.Visible;
        errorList.ItemsSource = errorList2;
        errorList.Visibility = Visibility.Visible;
        singleMessage.Visibility = Visibility.Collapsed;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show a single error message.
    /// </summary>
    public void ShowError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Clear();
            return;
        }

        ApplyStyle(ValidationStyle.Error);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show a success message.
    /// </summary>
    public void ShowSuccess(string message)
    {
        ApplyStyle(ValidationStyle.Success);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show an info message.
    /// </summary>
    public void ShowInfo(string message)
    {
        ApplyStyle(ValidationStyle.Info);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show a warning message.
    /// </summary>
    public void ShowWarning(string message)
    {
        ApplyStyle(ValidationStyle.Warning);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Clear and hide the panel.
    /// </summary>
    public void Clear()
    {
        Visibility = Visibility.Collapsed;
        errorList.ItemsSource = null;
        singleMessage.Text = "";
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        Clear();
    }

    private void ApplyStyle(ValidationStyle style)
    {
        switch (style)
        {
            case ValidationStyle.Error:
                // Translucent red overlay so the dark mode bg shows through
                panelBorder.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xEF, 0x44, 0x44));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x88, 0xEF, 0x44, 0x44));
                statusIcon.Text = "⚠";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
                headerText.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
                break;

            case ValidationStyle.Success:
                panelBorder.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x22, 0xC5, 0x5E));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x88, 0x22, 0xC5, 0x5E));
                statusIcon.Text = "✓";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
                break;

            case ValidationStyle.Warning:
                panelBorder.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xF5, 0x9E, 0x0B));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x88, 0xF5, 0x9E, 0x0B));
                statusIcon.Text = "⚠";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                break;

            case ValidationStyle.Info:
                panelBorder.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x3B, 0x82, 0xF6));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x88, 0x3B, 0x82, 0xF6));
                statusIcon.Text = "ℹ";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
                break;
        }
    }

    private enum ValidationStyle
    {
        Error,
        Success,
        Warning,
        Info
    }
}
