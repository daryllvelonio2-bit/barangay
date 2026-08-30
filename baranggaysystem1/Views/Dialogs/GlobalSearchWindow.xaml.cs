using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using baranggaysystem1.helper;
using FontAwesome.Sharp;
namespace baranggaysystem1.Views.Dialogs;

/// <summary>
/// Cross-module global search window. Searches residents, clearances,
/// blotter cases, and payments from a single unified search box.
/// </summary>
public partial class GlobalSearchWindow : Window
{
    private CancellationTokenSource? _searchCts;
    private readonly List<SearchDisplayResult> _results = new();

    public GlobalSearchWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => searchBox.Focus();
        PreviewKeyDown += OnPreviewKeyDown;
        Deactivated += (s, e) => { if (IsVisible) Close(); };
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = searchBox.Text?.Trim() ?? "";
        if (query.Length < 2)
        {
            ShowEmptyState();
            resultCountLabel.Text = "Type at least 2 characters to search";
            return;
        }

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(250, token); // debounce
            if (token.IsCancellationRequested) return;

            string activeFilter = GetActiveFilter();
            var results = await Task.Run(() => ExecuteSearch(query, activeFilter), token);
            if (token.IsCancellationRequested) return;

            _results.Clear();
            _results.AddRange(results);
            RenderResults(results);
            resultCountLabel.Text = $"{results.Count} result(s) found";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            AppLogger.LogError("Global search failed.", ex);
            resultCountLabel.Text = "Search error occurred";
        }
    }

    private void FilterTab_Checked(object sender, RoutedEventArgs e)
    {
        if (IsLoaded && searchBox.Text?.Trim().Length >= 2)
        {
            SearchBox_TextChanged(searchBox, null!);
        }
    }

    private string GetActiveFilter()
    {
        if (filterResidents?.IsChecked == true) return "residents";
        if (filterClearances?.IsChecked == true) return "clearances";
        if (filterBlotter?.IsChecked == true) return "blotter";
        if (filterPayments?.IsChecked == true) return "payments";
        return "all";
    }

    private static List<SearchDisplayResult> ExecuteSearch(string query, string filter)
    {
        GlobalSearchScope scope = filter switch
        {
            "residents" => GlobalSearchScope.Residents,
            "clearances" => GlobalSearchScope.Certificates,
            "blotter" => GlobalSearchScope.Blotter,
            "payments" => GlobalSearchScope.Payments,
            _ => GlobalSearchScope.All
        };

        return baranggaysystem1.GlobalSearchService.Search(query, scope, 10)
            .Select(MapResult)
            .Where(result => RouteAuthorization.CanNavigate(result.Route))
            .ToList();
    }

    private static SearchDisplayResult MapResult(baranggaysystem1.GlobalSearchResult result)
    {
        return result.EntityType switch
        {
            GlobalSearchEntityType.Resident => new SearchDisplayResult(
                "Residents", IconChar.Users, "#16A34A", result.Title, result.Subtitle,
                "ResidentWorkspace", result.Id),
            GlobalSearchEntityType.Certificate => new SearchDisplayResult(
                "Clearances", IconChar.Stamp, "#D97706", result.Title, result.Subtitle,
                "Clearances", result.Id),
            GlobalSearchEntityType.Blotter => new SearchDisplayResult(
                "Blotter", IconChar.Gavel, "#DC2626", result.Title, result.Subtitle,
                "ResidentCases", result.Id),
            GlobalSearchEntityType.Payment => new SearchDisplayResult(
                "Payments", IconChar.MoneyBill, "#2563EB", result.Title, result.Subtitle,
                "ResidentPayments", result.Id),
            _ => new SearchDisplayResult(
                "Users", IconChar.UserShield, "#64748B", result.Title, result.Subtitle,
                "StaffUsers", result.Id)
        };
    }

    private void RenderResults(List<SearchDisplayResult> results)
    {
        resultsPanel.Children.Clear();

        if (results.Count == 0)
        {
            var noResults = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) };
            noResults.Children.Add(new TextBlock { Text = "No results found", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("Slate500Brush"), HorizontalAlignment = HorizontalAlignment.Center });
            noResults.Children.Add(new TextBlock { Text = "Try a different search term or filter", FontSize = 11, Foreground = (Brush)FindResource("Slate400Brush"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
            resultsPanel.Children.Add(noResults);
            return;
        }

        // Group by module
        var grouped = results.GroupBy(r => r.Module);
        foreach (var group in grouped)
        {
            var header = new TextBlock
            {
                Text = group.Key.ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("Slate400Brush"),
                Margin = new Thickness(0, 12, 0, 6)
            };
            resultsPanel.Children.Add(header);

            foreach (var result in group)
            {
                var card = BuildResultCard(result);
                resultsPanel.Children.Add(card);
            }
        }
    }

    private Border BuildResultCard(SearchDisplayResult result)
    {
        var card = new Border
        {
            Background = (Brush)FindResource("ThemeCardBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 4),
            Cursor = Cursors.Hand,
            BorderBrush = (Brush)FindResource("ThemeBorderSubtleBrush"),
            BorderThickness = new Thickness(1)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Icon
        var iconBorder = new Border
        {
            Width = 32, Height = 32, CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.IconColor + "18")),
            VerticalAlignment = VerticalAlignment.Center
        };
        var icon = new IconBlock { Icon = result.Icon, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.IconColor));
        iconBorder.Child = icon;
        Grid.SetColumn(iconBorder, 0);
        grid.Children.Add(iconBorder);

        // Text
        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
        textPanel.Children.Add(new TextBlock { Text = result.Title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("ThemeTextPrimaryBrush"), TextTrimming = TextTrimming.CharacterEllipsis });
        textPanel.Children.Add(new TextBlock { Text = result.Subtitle, FontSize = 10, Foreground = (Brush)FindResource("ThemeTextSecondaryBrush"), Margin = new Thickness(0, 2, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(textPanel, 1);
        grid.Children.Add(textPanel);

        // Module badge
        var badge = new Border
        {
            Background = (Brush)FindResource("ThemeCardHoverBrush"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.Child = new TextBlock { Text = result.Module, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("ThemeTextSecondaryBrush") };
        Grid.SetColumn(badge, 2);
        grid.Children.Add(badge);

        card.Child = grid;

        // Hover effect
        card.MouseEnter += (s, e) => card.Background = (Brush)FindResource("ThemeCardHoverBrush");
        card.MouseLeave += (s, e) => card.Background = (Brush)FindResource("ThemeCardBrush");

        // Click to navigate
        card.MouseLeftButtonDown += (s, e) =>
        {
            if (Application.Current.MainWindow is Views.MainWindow mainWindow)
            {
                mainWindow.NavigatePage(result.Route);
            }
            Close();
        };

        return card;
    }

    private void ShowEmptyState()
    {
        resultsPanel.Children.Clear();
        resultsPanel.Children.Add(emptyState);
        emptyState.Visibility = Visibility.Visible;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}

/// <summary>
/// Represents a single search result from any module.
/// </summary>
internal sealed record SearchDisplayResult(
    string Module,
    IconChar Icon,
    string IconColor,
    string Title,
    string Subtitle,
    string Route,
    int RecordId);
