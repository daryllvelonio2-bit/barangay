using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class DeceasedRegistryPage : UserControl, IRefreshable
{
	private readonly ResidentDeathRecordService _service = new();
	private DataTable? _data;

	public DeceasedRegistryPage()
	{
		InitializeComponent();
		Loaded += async (_, _) => await LoadAsync();
	}

	public DeceasedRegistryPage(string route) : this() { }

	private async Task LoadAsync()
	{
		try
		{
			_data = await _service.GetRegistryAsync();
			mainGrid.ItemsSource = _data.DefaultView;
			recordCountLabel.Text = $"{_data.Rows.Count:N0} verified registry record(s)";
			footerCountLabel.Text = $"Showing {_data.Rows.Count:N0} record(s)";
			ApplyFilter();
			UpdateSelection(null);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DeceasedRegistryPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "The deceased registry could not be loaded.";
			emptyState.Visibility = Visibility.Visible;
		}
	}

	private void ApplyFilter()
	{
		if (_data == null) return;
		string q = searchBox.Text.Trim().Replace("'", "''");
		_data.DefaultView.RowFilter = string.IsNullOrWhiteSpace(q) ? string.Empty :
			$"Convert([resident_name], 'System.String') LIKE '%{q}%' OR " +
			$"Convert([certificate_reference], 'System.String') LIKE '%{q}%' OR " +
			$"Convert([reported_by], 'System.String') LIKE '%{q}%'";
		emptyState.Visibility = _data.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		footerCountLabel.Text = $"Showing {_data.DefaultView.Count:N0} of {_data.Rows.Count:N0} record(s)";
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
		selectedRecordLabel.Text = Convert.ToString(row["resident_name"]) ?? "Resident";
		string status = Convert.ToString(row["record_status"]) ?? "CONFIRMED";
		btnCorrect.Visibility = status.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase)
			? Visibility.Visible : Visibility.Collapsed;
		contextActionBar.Visibility = Visibility.Visible;
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		FullscreenDialogNavigator.Open(
			new ResidentDeathRecordWindow(),
			"Record Resident Death",
			"Select the resident, enter the supporting record, then verify before confirming.",
			"DeceasedRegistry",
			IconChar.User,
			"Confirm Record",
			RefreshData);
	}

	private async void BtnCorrect_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is not DataRowView row) return;
		string resident = Convert.ToString(row["resident_name"]) ?? "this resident";
		var prompt = new ReasonPromptWindow(
			"Correct Death Record",
			$"Explain why the confirmed record for {resident} must be reversed.",
			"Reverse record")
		{
			Owner = Window.GetWindow(this)
		};
		if (prompt.ShowDialog() != true) return;
		try
		{
			await _service.ReverseAsync(
				Convert.ToInt32(row["death_record_id"]),
				Convert.ToInt32(row["resident_id"]),
				prompt.Reason);
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Death record correction failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
		}
	}

	public void RefreshData() => _ = LoadAsync();
}
