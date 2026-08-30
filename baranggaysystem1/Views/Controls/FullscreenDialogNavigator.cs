using System;
using System.Collections.Generic;
using System.Windows;
using FontAwesome.Sharp;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Opens operational Window-based forms inside the application's full-screen
/// workspace. Compact confirmations, credential prompts, and file pickers
/// intentionally continue to use modal windows.
/// </summary>
public static class FullscreenDialogNavigator
{
    public static DialogContentAdapter Open(
        Window dialog,
        string title,
        string subtitle,
        string originRoute,
        IconChar icon,
        string? primaryActionLabel = null,
        Action? onSaved = null)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        var adapter = new DialogContentAdapter(dialog);
        if (!string.IsNullOrWhiteSpace(primaryActionLabel))
        {
            System.Windows.Automation.AutomationProperties.SetHelpText(
                adapter,
                $"Primary action: {primaryActionLabel}. Use the action button inside the form.");
        }

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = title,
            Subtitle = subtitle,
            OriginRoute = originRoute,
            Content = adapter,
            Icon = icon,
            ToolbarItems = new List<UIElement>(),
            ShowSideToolbar = false,
            OnSaved = onSaved
        });

        return adapter;
    }
}
