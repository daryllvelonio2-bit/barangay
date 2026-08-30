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

public partial class OfficialsPage : UserControl, IRefreshable
{
	private readonly BarangayOfficialService _service = new();
	private DataTable? _data;

	public OfficialsPage()
	{
		InitializeComponent();
		Loaded += async (_, _) => await LoadAsync();
	}

	public OfficialsPage(string route) : this() { }

	private async Task LoadAsync()
	{
		try
		{
			_data = await _service.GetBarangayOfficialsAsync();
			mainGrid.ItemsSource = _data.DefaultView;
			recordCountLabel.Text = $"{_data.Rows.Count:N0} appointment(s)";
			footerCountLabel.Text = $"Showing {_data.Rows.Count:N0} appointment(s)";
			ApplyFilter();
			UpdateSelection(null);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("OfficialsPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "The official registry could not be loaded.";
			emptyState.Visibility = Visibility.Visible;
		}
	}

	private void ApplyFilter()
	{
		if (_data == null) return;
		string q = searchBox.Text.Trim().Replace("'", "''");
		string status = Convert.ToString((statusFilter.SelectedItem as ComboBoxItem)?.Content) ?? "All Statuses";
		string textFilter = string.IsNullOrWhiteSpace(q) ? string.Empty :
			$"(Convert([full_name], 'System.String') LIKE '%{q}%' OR Convert([position], 'System.String') LIKE '%{q}%' OR Convert([committee], 'System.String') LIKE '%{q}%')";
		string statusClause = status == "All Statuses" ? string.Empty : $"[status_display] = '{status.Replace("'", "''")}'";
		_data.DefaultView.RowFilter = string.Join(" AND ", new[] { textFilter, statusClause }.Where(x => !string.IsNullOrWhiteSpace(x)));
		emptyState.Visibility = _data.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		footerCountLabel.Text = $"Showing {_data.DefaultView.Count:N0} of {_data.Rows.Count:N0} appointment(s)";
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) ApplyFilter(); }
	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelection(mainGrid.SelectedItem as DataRowView);
	private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
	private void BtnClearSelection_Click(object sender, RoutedEventArgs e) => mainGrid.SelectedItem = null;

	private void UpdateSelection(DataRowView? row)
	{
		if (row == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			return;
		}
		string status = Convert.ToString(row["status_display"]) ?? "ACTIVE";
		selectedRecordLabel.Text = Convert.ToString(row["full_name"]) ?? "Official";
		btnStatus.Content = status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) ? "End Appointment" : "Reactivate";
		contextActionBar.Visibility = Visibility.Visible;
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		FullscreenDialogNavigator.Open(
			new OfficialDetailsWindow(),
			"New Official Appointment",
			"Assign one registered resident to an official term and position.",
			"Officials",
			IconChar.UserShield,
			"Save Appointment",
			RefreshData);
	}

	private void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is not DataRowView row) return;
		string name = Convert.ToString(row["full_name"]) ?? "Official";
		FullscreenDialogNavigator.Open(
			new OfficialDetailsWindow(Convert.ToInt32(row["official_id"])),
			"Edit Official Appointment",
			name,
			"Officials",
			IconChar.UserEdit,
			"Save Changes",
			RefreshData);
	}

	private async void BtnStatus_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is not DataRowView row) return;
		int id = Convert.ToInt32(row["official_id"]);
		string status = Convert.ToString(row["status_display"]) ?? "ACTIVE";
		try
		{
			if (status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
			{
				var prompt = new ReasonPromptWindow("End Appointment", "Record why this official appointment is ending.", "End appointment")
				{ Owner = Window.GetWindow(this) };
				if (prompt.ShowDialog() != true) return;
				await _service.SetBarangayOfficialStatusAsync(id, "INACTIVE", prompt.Reason);
			}
			else
			{
				if (!DialogService.Instance.Confirm("Reactivate this official appointment?", "Reactivate Appointment")) return;
				await _service.SetBarangayOfficialStatusAsync(id, "ACTIVE");
			}
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Official status change failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
		}
	}

	public void RefreshData() => _ = LoadAsync();
}
