using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class AyudaPage : UserControl, IRefreshable
{
	private readonly AyudaService _ayudaService = new AyudaService();

	private DataTable? _programData;

	private DataTable? _releaseData;

	private int? _selectedProgramId;

	private bool _isApplyingWorkflowState;






















	public AyudaPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public AyudaPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		_ = 1;
		try
		{
			_programData = await _ayudaService.GetProgramLedgerAsync();
			_releaseData = await _ayudaService.GetReleaseLedgerAsync();
			EnrichProgramTable(_programData);
			EnrichReleaseTable(_releaseData);
			ApplyDataSources();
			PopulateFilterOptions();
			DataRowView? dataRowView = FindProgramRow(_selectedProgramId);
			if (dataRowView != null)
			{
				ShowProgramDetail(dataRowView, resetFilters: false);
			}
			else
			{
				ShowProgramList(resetFilters: false);
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("AyudaPage load failed.", ex);
			programGrid.ItemsSource = null;
			releaseGrid.ItemsSource = null;
			programEmptyLabel.Text = "Failed to load ayuda budgets.";
			releaseEmptyLabel.Text = "Failed to load ayuda releases.";
			programEmptyState.Visibility = Visibility.Visible;
			releaseEmptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Unable to load ayuda records.";
			recordCountLabel.Text = "Ayuda module unavailable";
			totalBudgetMetric.Text = "PHP 0.00";
			spentBudgetMetric.Text = "PHP 0.00";
			remainingBudgetMetric.Text = "PHP 0.00";
			beneficiaryMetric.Text = "0";
			programViewPanel.Visibility = Visibility.Visible;
			releaseViewPanel.Visibility = Visibility.Collapsed;
			contextActionBar.Visibility = Visibility.Collapsed;
			btnAddProgram.Visibility = Visibility.Visible;
			btnReleaseAyuda.Visibility = Visibility.Collapsed;
		}
	}

	private void ApplyDataSources()
	{
		programGrid.ItemsSource = _programData?.DefaultView;
		releaseGrid.ItemsSource = _releaseData?.DefaultView;
		programEmptyState.Visibility = ((_programData != null && _programData.Rows.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		releaseEmptyState.Visibility = ((_releaseData != null && _releaseData.Rows.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void PopulateFilterOptions()
	{
		programFilter.Items.Clear();
		programFilter.Items.Add("All Programs");
		statusFilter.Items.Clear();
		statusFilter.Items.Add("All Release Status");
		if (_programData != null)
		{
			foreach (string item in from value in (from row in _programData.AsEnumerable()
					select Convert.ToString(row["program_name"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				programFilter.Items.Add(item);
			}
		}
		if (_releaseData != null)
		{
			foreach (string item2 in from value in (from row in _releaseData.AsEnumerable()
					select Convert.ToString(row["release_status"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				statusFilter.Items.Add(item2);
			}
		}
		programFilter.SelectedIndex = 0;
		statusFilter.SelectedIndex = 0;
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && !_isApplyingWorkflowState)
		{
			ApplyFilters();
		}
	}

	private void ApplyFilters()
	{
		if (_programData == null || _releaseData == null)
		{
			return;
		}
		string value = searchBox.Text.Trim();
		string text2 = statusFilter.SelectedItem as string;
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (_selectedProgramId.HasValue)
		{
			list2.Add("[program_id] = " + _selectedProgramId.Value.ToString(CultureInfo.InvariantCulture));
			if (!string.IsNullOrWhiteSpace(value))
			{
				string escaped = EscapeForRowFilter(value);
				string[] source = new string[8] { "reference_no", "batch_reference", "reference_meta", "resident_name", "amount_display", "released_at", "notes_display", "release_status_display" };
				list2.Add("(" + string.Join(" OR ", source.Select((string column) => $"Convert([{column}], 'System.String') LIKE '%{escaped}%'")) + ")");
			}
			if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, "All Release Status", StringComparison.OrdinalIgnoreCase))
			{
				list2.Add("[release_status] = '" + EscapeForRowFilter(text2) + "'");
			}
		}
		else if (!string.IsNullOrWhiteSpace(value))
		{
			string escaped2 = EscapeForRowFilter(value);
			string[] source2 = new string[7] { "program_name", "category", "status_display", "allocated_budget_display", "budget_meta", "schedule_display", "notes" };
			list.Add("(" + string.Join(" OR ", source2.Select((string column) => $"Convert([{column}], 'System.String') LIKE '%{escaped2}%'")) + ")");
		}
		_programData.DefaultView.RowFilter = string.Join(" AND ", list);
		_releaseData.DefaultView.RowFilter = string.Join(" AND ", list2);
		List<DataRowView> list3 = _programData.DefaultView.Cast<DataRowView>().ToList();
		List<DataRowView> list4 = _releaseData.DefaultView.Cast<DataRowView>().ToList();
		DataRowView? dataRowView = FindProgramRow(_selectedProgramId);
		IEnumerable<DataRowView> source3 = ((dataRowView == null) ? list3 : new DataRowView[1] { dataRowView });
		decimal amount = source3.Sum((DataRowView row) => GetDecimal(row, "allocated_budget"));
		decimal amount2 = source3.Sum((DataRowView row) => GetDecimal(row, "spent_budget"));
		decimal amount3 = source3.Sum((DataRowView row) => GetDecimal(row, "remaining_budget"));
		int num = (from row in list4
			where !string.Equals(Convert.ToString(row["release_status"]), "CANCELLED", StringComparison.OrdinalIgnoreCase)
			select Convert.ToInt32(row["resident_id"], CultureInfo.InvariantCulture)).Distinct().Count();
		totalBudgetMetric.Text = FormatCurrency(amount);
		spentBudgetMetric.Text = FormatCurrency(amount2);
		remainingBudgetMetric.Text = FormatCurrency(amount3);
		beneficiaryMetric.Text = num.ToString("N0", CultureInfo.InvariantCulture);
		programTableVisibleLabel.Text = $"{list3.Count:N0} program(s)";
		releaseTableVisibleLabel.Text = $"{list4.Count:N0} release(s)";
		if (_selectedProgramId.HasValue)
		{
			recordCountLabel.Text = $"{list4.Count:N0} release(s) for selected budget";
			footerCountLabel.Text = $"{list4.Count:N0} distribution record(s) shown";
		}
		else
		{
			recordCountLabel.Text = $"{list3.Count:N0} budget program(s)";
			footerCountLabel.Text = $"Showing {list3.Count:N0} ayuda budget program(s)";
		}
		programEmptyState.Visibility = ((list3.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		releaseEmptyState.Visibility = ((list4.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		programEmptyLabel.Text = ((list3.Count == 0) ? "No ayuda programs match the current filters." : "No ayuda programs found.");
		releaseEmptyLabel.Text = ((!string.IsNullOrWhiteSpace(value) || statusFilter.SelectedIndex > 0) ? "No distributions match the current filters." : "No distributions have been recorded for this budget.");
		UpdateReleaseSelectionState(releaseGrid.SelectedItem as DataRowView);
	}

	private void ProgramGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isApplyingWorkflowState && programGrid.SelectedItem is DataRowView dataRowView)
		{
			ShowProgramDetail(dataRowView);
		}
	}

	private void ReleaseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateReleaseSelectionState(releaseGrid.SelectedItem as DataRowView);
	}

	private void ShowProgramList(bool resetFilters = true)
	{
		_isApplyingWorkflowState = true;
		try
		{
			_selectedProgramId = null;
			programViewPanel.Visibility = Visibility.Visible;
			releaseViewPanel.Visibility = Visibility.Collapsed;
			contextActionBar.Visibility = Visibility.Collapsed;
			btnAddProgram.Visibility = Visibility.Visible;
			btnReleaseAyuda.Visibility = Visibility.Collapsed;
			statusFilter.Visibility = Visibility.Collapsed;
			workflowStepLabel.Text = "Step 1 of 2  •  Choose a budget program";
			searchPlaceholderLabel.Text = "Search budget programs";
			releaseGrid.SelectedItem = null;
			if (resetFilters)
			{
				searchBox.Clear();
				statusFilter.SelectedIndex = 0;
			}
		}
		finally
		{
			_isApplyingWorkflowState = false;
		}
		ApplyFilters();
	}

	private void ShowProgramDetail(DataRowView row, bool resetFilters = true)
	{
		_isApplyingWorkflowState = true;
		try
		{
			_selectedProgramId = Convert.ToInt32(row["program_id"], CultureInfo.InvariantCulture);
			programGrid.SelectedItem = row;
			programViewPanel.Visibility = Visibility.Collapsed;
			releaseViewPanel.Visibility = Visibility.Visible;
			contextActionBar.Visibility = Visibility.Visible;
			btnAddProgram.Visibility = Visibility.Collapsed;
			statusFilter.Visibility = Visibility.Visible;
			workflowStepLabel.Text = "Step 2 of 2  •  Review this budget's distributions";
			searchPlaceholderLabel.Text = "Search this budget's distributions";
			selectedProgramTitleText.Text = Convert.ToString(row["program_name"]) ?? "Selected Budget";
			selectedProgramSummaryText.Text = (Convert.ToString(row["category"]) ?? "Assistance")
				+ "  •  Allocated " + (Convert.ToString(row["allocated_budget_display"]) ?? "PHP 0.00")
				+ "  •  Remaining " + (Convert.ToString(row["remaining_budget_display"]) ?? "PHP 0.00");
			bool flag = string.Equals(Convert.ToString(row["status"]), "ACTIVE", StringComparison.OrdinalIgnoreCase) && GetDecimal(row, "remaining_budget") > 0m;
			btnReleaseAyuda.Visibility = flag ? Visibility.Visible : Visibility.Collapsed;
			int num = GetProgramReleaseRecordCount(_selectedProgramId.Value);
			btnDeleteProgram.Visibility = num == 0 ? Visibility.Visible : Visibility.Collapsed;
			programActionsPanel.Visibility = Visibility.Visible;
			releaseActionsPanel.Visibility = Visibility.Collapsed;
			contextHelpLabel.Text = flag ? "Choose a distribution to see its actions, or start a new distribution." : "Choose a distribution to see its available actions.";
			releaseGrid.SelectedItem = null;
			if (resetFilters)
			{
				searchBox.Clear();
				statusFilter.SelectedIndex = 0;
			}
		}
		finally
		{
			_isApplyingWorkflowState = false;
		}
		ApplyFilters();
	}

	private DataRowView? FindProgramRow(int? programId)
	{
		if (!programId.HasValue || _programData == null)
		{
			return null;
		}
		return _programData.DefaultView.Cast<DataRowView>().FirstOrDefault((DataRowView row) => Convert.ToInt32(row["program_id"], CultureInfo.InvariantCulture) == programId.Value)
			?? _programData.AsEnumerable()
				.Where((DataRow row) => Convert.ToInt32(row["program_id"], CultureInfo.InvariantCulture) == programId.Value)
				.Select((DataRow row) => _programData.DefaultView.Cast<DataRowView>().FirstOrDefault((DataRowView view) => view.Row == row))
				.FirstOrDefault();
	}

	private int GetProgramReleaseRecordCount(int programId)
	{
		if (_releaseData == null)
		{
			return 0;
		}
		return _releaseData.AsEnumerable().Count((DataRow row) => Convert.ToInt32(row["program_id"], CultureInfo.InvariantCulture) == programId);
	}

	private void BtnBackToPrograms_Click(object sender, RoutedEventArgs e)
	{
		ShowProgramList();
	}

	private void BtnClearReleaseSelection_Click(object sender, RoutedEventArgs e)
	{
		releaseGrid.SelectedItem = null;
		UpdateReleaseSelectionState(null);
	}

	private void UpdateReleaseSelectionState(DataRowView? row)
	{
		if (row == null)
		{
			releaseTableSelectionLabel.Text = "No selection";
			programActionsPanel.Visibility = Visibility.Visible;
			releaseActionsPanel.Visibility = Visibility.Collapsed;
			contextHelpLabel.Text = "Choose a distribution to see only the actions available for it.";
			return;
		}
		string text = Convert.ToString(row["reference_no"]) ?? "Release";
		string text2 = Convert.ToString(row["batch_reference"]) ?? string.Empty;
		releaseTableSelectionLabel.Text = (string.IsNullOrWhiteSpace(text2) ? ("Selected: " + text) : ("Selected: " + text + " | " + text2));
		bool flag = string.Equals(Convert.ToString(row["release_status"]), "CANCELLED", StringComparison.OrdinalIgnoreCase);
		bool flag2 = !string.IsNullOrWhiteSpace(Convert.ToString(row["report_file_path"]));
		programActionsPanel.Visibility = Visibility.Collapsed;
		releaseActionsPanel.Visibility = Visibility.Visible;
		btnCancelRelease.Visibility = flag ? Visibility.Collapsed : Visibility.Visible;
		btnOpenReport.Visibility = flag2 ? Visibility.Visible : Visibility.Collapsed;
		contextHelpLabel.Text = flag ? "This cancelled distribution is retained for audit." : "Actions shown apply only to " + text + ".";
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private void BtnAddProgram_Click(object sender, RoutedEventArgs e)
	{
		AyudaProgramWindow window = new AyudaProgramWindow();
		var adapter = new DialogContentAdapter(window);

		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "New Ayuda Program",
			Subtitle = "Create a new ayuda budget program",
			OriginRoute = "Ayuda",
			Content = adapter,
			Icon = IconChar.HandHoldingHeart,
			ToolbarItems = new List<UIElement>(),
			ShowSideToolbar = false,
			OnSaved = () => RefreshData()
		});
	}

	private void BtnEditProgram_Click(object sender, RoutedEventArgs e)
	{
		if (!(programGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda budget program to edit first.");
			return;
		}
		AyudaProgramWindow window = new AyudaProgramWindow(Convert.ToInt32(dataRowView["program_id"], CultureInfo.InvariantCulture));
		var adapter = new DialogContentAdapter(window);

		string programName = Convert.ToString(dataRowView["program_name"], CultureInfo.InvariantCulture) ?? "Program";
		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "Edit Ayuda Program",
			Subtitle = programName,
			OriginRoute = "Ayuda",
			Content = adapter,
			Icon = IconChar.Edit,
			ToolbarItems = new List<UIElement>(),
			ShowSideToolbar = false,
			OnSaved = () => RefreshData()
		});
	}

	private async void BtnDeleteProgram_Click(object sender, RoutedEventArgs e)
	{
		if (!(programGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda budget program to delete first.");
			return;
		}
		int programId = Convert.ToInt32(dataRowView["program_id"], CultureInfo.InvariantCulture);
		string text = Convert.ToString(dataRowView["program_name"], CultureInfo.InvariantCulture) ?? "this program";
		var reasonPrompt = new ReasonPromptWindow(
			"Archive Ayuda Program",
			"Explain why \"" + text + "\" is being archived.",
			"Archive program")
		{
			Owner = Window.GetWindow(this)
		};
		if (reasonPrompt.ShowDialog() != true)
		{
			return;
		}
		try
		{
			await _ayudaService.ArchiveProgramAsync(programId, reasonPrompt.Reason);
			DialogService.Instance.ShowInfo("Ayuda budget program archived. Its history remains available.");
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda program deletion failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Delete Ayuda Program");
		}
	}

	private void BtnReleaseAyuda_Click(object sender, RoutedEventArgs e)
	{
		int? initialProgramId = null;
		if (programGrid.SelectedItem is DataRowView dataRowView)
		{
			initialProgramId = Convert.ToInt32(dataRowView["program_id"], CultureInfo.InvariantCulture);
		}
		else if (releaseGrid.SelectedItem is DataRowView dataRowView2)
		{
			initialProgramId = Convert.ToInt32(dataRowView2["program_id"], CultureInfo.InvariantCulture);
		}
		AyudaReleaseWindow window = new AyudaReleaseWindow(initialProgramId);
		var adapter = new DialogContentAdapter(window);

		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "New Ayuda Release",
			Subtitle = "Record a new ayuda distribution",
			OriginRoute = "Ayuda",
			Content = adapter,
			Icon = IconChar.HandHoldingUsd,
			ToolbarItems = new List<UIElement>(),
			ShowSideToolbar = false,
			OnSaved = () => RefreshData()
		});
	}

	private async void BtnCancelRelease_Click(object sender, RoutedEventArgs e)
	{
		if (!(releaseGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda release to cancel first.");
			return;
		}
		int releaseId = Convert.ToInt32(dataRowView["release_id"], CultureInfo.InvariantCulture);
		string text = Convert.ToString(dataRowView["resident_name"], CultureInfo.InvariantCulture) ?? "this beneficiary";
		string value = Convert.ToString(dataRowView["batch_reference"], CultureInfo.InvariantCulture) ?? string.Empty;
		string status = Convert.ToString(dataRowView["release_status"], CultureInfo.InvariantCulture) ?? string.Empty;
		if (string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
		{
			DialogService.Instance.ShowWarning("This ayuda release is already cancelled.");
			return;
		}
		var reasonPrompt = new ReasonPromptWindow(
			"Reverse Ayuda Release",
			string.IsNullOrWhiteSpace(value)
				? $"Explain why the release for {text} is being reversed."
				: $"Explain why the release for {text} from batch {value} is being reversed.",
			"Reverse release")
		{
			Owner = Window.GetWindow(this)
		};
		if (reasonPrompt.ShowDialog() != true)
		{
			return;
		}
		try
		{
			await _ayudaService.CancelReleaseAsync(releaseId, reasonPrompt.Reason);
			DialogService.Instance.ShowInfo("Ayuda release reversed. The original record was retained and its amount returned to the available budget.");
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda release cancellation failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Cancel Ayuda Release");
		}
	}

	private void BtnOpenReport_Click(object sender, RoutedEventArgs e)
	{
		if (!(releaseGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda release first.");
			return;
		}
		string text = Convert.ToString(dataRowView["report_file_path"], CultureInfo.InvariantCulture) ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			DialogService.Instance.ShowWarning("No generated report is linked to the selected release.");
		}
		else
		{
			AyudaReleaseReportService.TryOpenGeneratedFile(text);
		}
	}

	private static void EnrichProgramTable(DataTable table)
	{
		EnsureStringColumn(table, "allocated_budget_display");
		EnsureStringColumn(table, "spent_budget_display");
		EnsureStringColumn(table, "remaining_budget_display");
		EnsureStringColumn(table, "status_display");
		EnsureStringColumn(table, "budget_meta");
		EnsureStringColumn(table, "schedule_display");
		foreach (DataRow row in table.Rows)
		{
			decimal amount = ((row["spent_budget"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["spent_budget"], CultureInfo.InvariantCulture));
			decimal amount2 = ((row["remaining_budget"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["remaining_budget"], CultureInfo.InvariantCulture));
			int value = ((row["release_count"] != DBNull.Value) ? Convert.ToInt32(row["release_count"], CultureInfo.InvariantCulture) : 0);
			int value2 = ((row["beneficiary_count"] != DBNull.Value) ? Convert.ToInt32(row["beneficiary_count"], CultureInfo.InvariantCulture) : 0);
			string text = Convert.ToString(row["start_date_display"]) ?? string.Empty;
			string text2 = Convert.ToString(row["end_date_display"]) ?? string.Empty;
			string value3 = Convert.ToString(row["status"]) ?? "ACTIVE";
			row["allocated_budget_display"] = FormatCurrency(GetDecimal(row, "allocated_budget"));
			row["spent_budget_display"] = FormatCurrency(amount);
			row["remaining_budget_display"] = FormatCurrency(amount2);
			row["status_display"] = ToTitleCase(value3);
			row["budget_meta"] = $"Released {FormatCurrency(amount)} | {value2:N0} beneficiary(ies) | {value:N0} release(s)";
			row["schedule_display"] = ((string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2)) ? "No schedule range set" : (string.IsNullOrWhiteSpace(text2) ? ("Started " + text) : (string.IsNullOrWhiteSpace(text) ? ("Until " + text2) : (text + " to " + text2))));
		}
	}

	private static void EnrichReleaseTable(DataTable table)
	{
		EnsureStringColumn(table, "amount_display");
		EnsureStringColumn(table, "release_status_display");
		EnsureStringColumn(table, "resident_meta");
		EnsureStringColumn(table, "notes_display");
		EnsureStringColumn(table, "reference_meta");
		foreach (DataRow row in table.Rows)
		{
			int num = ((row["resident_id"] != DBNull.Value) ? Convert.ToInt32(row["resident_id"], CultureInfo.InvariantCulture) : 0);
			int value = ((row["beneficiary_count"] == DBNull.Value) ? 1 : Convert.ToInt32(row["beneficiary_count"], CultureInfo.InvariantCulture));
			string text = Convert.ToString(row["notes"]) ?? string.Empty;
			string value2 = Convert.ToString(row["release_status"]) ?? "RELEASED";
			string value3 = Convert.ToString(row["batch_reference"]) ?? string.Empty;
			string text2 = Convert.ToString(row["contact_no"]) ?? string.Empty;
			string text3 = Convert.ToString(row["released_at"]) ?? string.Empty;
			row["amount_display"] = FormatCurrency(GetDecimal(row, "amount"));
			row["release_status_display"] = ToTitleCase(value2);
			row["resident_meta"] = ((!string.IsNullOrWhiteSpace(text2)) ? text2 : ((num > 0) ? $"Resident ID #{num}" : "Resident record"));
			row["notes_display"] = (string.IsNullOrWhiteSpace(text) ? "No notes recorded" : text.Trim());
			row["reference_meta"] = (string.IsNullOrWhiteSpace(value3) ? text3 : $"{text3} | Batch {value3} | {value:N0} beneficiary(ies)");
		}
	}

	private static void EnsureStringColumn(DataTable table, string columnName)
	{
		if (!table.Columns.Contains(columnName))
		{
			table.Columns.Add(columnName, typeof(string));
		}
	}

	private static decimal GetDecimal(DataRowView row, string columnName)
	{
		if (row[columnName] != DBNull.Value)
		{
			return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
		}
		return 0m;
	}

	private static decimal GetDecimal(DataRow row, string columnName)
	{
		if (row[columnName] != DBNull.Value)
		{
			return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
		}
		return 0m;
	}

	private static string FormatCurrency(decimal amount)
	{
		return $"PHP {amount:N2}";
	}

	private static string ToTitleCase(string value)
	{
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase((value ?? string.Empty).ToLowerInvariant());
	}

	private static string EscapeForRowFilter(string value)
	{
		return value.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]")
			.Replace("*", "[*]");
	}

	#region IRefreshable Implementation

	/// <summary>
	/// Refreshes the page data after returning from a fullscreen view.
	/// </summary>
	public void RefreshData()
	{
		_ = LoadAsync();
	}

	#endregion
}
