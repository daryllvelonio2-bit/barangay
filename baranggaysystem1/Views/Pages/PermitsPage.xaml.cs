using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.Views.Panels;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class PermitsPage : UserControl, IRefreshable
{
	private readonly CertificateRequestService _service = new();
	private DataTable? _data;

	public PermitsPage()
	{
		InitializeComponent();
		Loaded += async (_, _) => await LoadAsync();
	}

	public PermitsPage(string route) : this() { }

	private async Task LoadAsync()
	{
		try
		{
			DataTable queue = await _service.GetQueueAsync();
			_data = queue.Clone();
			foreach (DataRow row in queue.Rows)
			{
				string type = Convert.ToString(row["certification_type"]) ?? string.Empty;
				if (type.Contains("BUSINESS", StringComparison.OrdinalIgnoreCase) ||
					type.Contains("PERMIT", StringComparison.OrdinalIgnoreCase))
				{
					_data.ImportRow(row);
				}
			}
			mainGrid.ItemsSource = _data.DefaultView;
			recordCountLabel.Text = $"{_data.Rows.Count:N0} permit request(s)";
			footerCountLabel.Text = _data.Rows.Count == 0
				? "No permit requests found."
				: $"Showing {_data.Rows.Count:N0} permit request(s)";
			ApplyFilter();
			UpdateSelection(null);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("PermitsPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "The permit queue could not be loaded.";
			emptyState.Visibility = Visibility.Visible;
		}
	}

	private void ApplyFilter()
	{
		if (_data == null) return;
		string q = searchBox.Text.Trim().Replace("'", "''");
		_data.DefaultView.RowFilter = string.IsNullOrWhiteSpace(q)
			? string.Empty
			: $"Convert([tracking_code], 'System.String') LIKE '%{q}%' OR " +
			  $"Convert([resident_name], 'System.String') LIKE '%{q}%' OR " +
			  $"Convert([certification_type], 'System.String') LIKE '%{q}%'";
		emptyState.Visibility = _data.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		footerCountLabel.Text = $"Showing {_data.DefaultView.Count:N0} of {_data.Rows.Count:N0} permit request(s)";
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
		UpdateSelection(mainGrid.SelectedItem as DataRowView);
	private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

	private void UpdateSelection(DataRowView? row)
	{
		if (row == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			return;
		}
		string status = Convert.ToString(row["status"]) ?? "SUBMITTED";
		selectedRecordLabel.Text = Convert.ToString(row["tracking_code"]) ?? "Permit request";
		actionLabel.Text = status.ToUpperInvariant() switch
		{
			"SUBMITTED" => "Start Review",
			"UNDER_REVIEW" => "Approve Request",
			"APPROVED" or "READY_FOR_RELEASE" => "Release Permit",
			_ => "No Action"
		};
		btnAdvance.IsEnabled = status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
			status.Equals("UNDER_REVIEW", StringComparison.OrdinalIgnoreCase) ||
			status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) ||
			status.Equals("READY_FOR_RELEASE", StringComparison.OrdinalIgnoreCase);
		btnCancel.IsEnabled = !status.Equals("RELEASED", StringComparison.OrdinalIgnoreCase) &&
			!status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);
		contextActionBar.Visibility = Visibility.Visible;
	}

	private async void BtnAdvance_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is not DataRowView row) return;
		int requestId = Convert.ToInt32(row["doc_request_id"]);
		string status = Convert.ToString(row["status"]) ?? "SUBMITTED";
		string label = Convert.ToString(row["tracking_code"]) ?? "Permit request";
		try
		{
			if (status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
			{
				await _service.StartReviewAsync(requestId, label);
				await LoadAsync();
				return;
			}
			if (status.Equals("UNDER_REVIEW", StringComparison.OrdinalIgnoreCase))
			{
				if (!DialogService.Instance.Confirm("Approve this permit after completing the review?", "Approve Permit")) return;
				await _service.ApproveRequestAsync(requestId, label);
				await LoadAsync();
				return;
			}
			if (status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) ||
				status.Equals("READY_FOR_RELEASE", StringComparison.OrdinalIgnoreCase))
			{
				OpenReleaseForm(requestId, label);
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Permit workflow action failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
		}
	}

	private async void BtnCancel_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is not DataRowView row) return;
		string label = Convert.ToString(row["tracking_code"]) ?? "permit request";
		var prompt = new ReasonPromptWindow("Cancel Permit", $"Explain why {label} is being cancelled.", "Cancel permit")
		{
			Owner = Window.GetWindow(this)
		};
		if (prompt.ShowDialog() != true) return;
		try
		{
			await _service.CancelRequestAsync(Convert.ToInt32(row["doc_request_id"]), label, prompt.Reason);
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Permit cancellation failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
		}
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		var form = new CertificateFormPanel(CertificateDialogMode.Request, businessOnly: true);
		var save = CreateToolbarButton("Submit Permit", IconChar.PaperPlane, async (_, _) =>
		{
			if (await form.TrySaveAsync())
				NavigationService.Instance.NavigateBackFromFullscreen("Permits", refreshOnReturn: true);
		});
		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "New Business Permit Request",
			Subtitle = "Select the resident and business document, then submit it for review",
			OriginRoute = "Permits",
			Content = form,
			Icon = IconChar.ShieldAlt,
			ToolbarItems = new List<UIElement> { save },
			ShowSideToolbar = false,
			OnSaved = RefreshData
		});
	}

	private void OpenReleaseForm(int requestId, string label)
	{
		var form = new CertificateFormPanel(CertificateDialogMode.Issue, requestId, loadExisting: true);
		var save = CreateToolbarButton("Release Permit", IconChar.Print, async (_, _) =>
		{
			if (await form.TrySaveAsync())
				NavigationService.Instance.NavigateBackFromFullscreen("Permits", refreshOnReturn: true);
		});
		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "Release: " + label,
			Subtitle = "Record the receipt, verify the details, and release the approved permit",
			OriginRoute = "Permits",
			Content = form,
			Icon = IconChar.Stamp,
			ToolbarItems = new List<UIElement> { save },
			ShowSideToolbar = false,
			OnSaved = RefreshData
		});
	}

	private static Button CreateToolbarButton(string label, IconChar icon, RoutedEventHandler handler)
	{
		var panel = new StackPanel { Orientation = Orientation.Horizontal };
		panel.Children.Add(new IconBlock { Icon = icon, FontSize = 13, Margin = new Thickness(0, 0, 6, 0) });
		panel.Children.Add(new TextBlock { Text = label });
		var button = new Button { Content = panel, Padding = new Thickness(12, 6, 12, 6) };
		button.Click += handler;
		return button;
	}

	public void RefreshData() => _ = LoadAsync();
}
