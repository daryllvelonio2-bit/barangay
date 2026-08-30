using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;
using FontAwesome.Sharp;
using Microsoft.Win32;

namespace baranggaysystem1.Views.Pages;

public partial class PaymentsPage : UserControl, IRefreshable
{
	private readonly PaymentLedgerService _paymentLedgerService = new PaymentLedgerService();

	private DataTable? _data;













	public PaymentsPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public PaymentsPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		try
		{
			_data = await _paymentLedgerService.GetLedgerAsync();
			ApplyDataToGrid(_data);
			PopulateFilterOptions(_data);
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("PaymentsPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load payment data. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Unable to load ledger.";
			recordCountLabel.Text = "Ledger unavailable";
		}
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		if (table == null || table.Rows.Count == 0)
		{
			mainGrid.ItemsSource = null;
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "No payment records found.";
			recordCountLabel.Text = "No transactions.";
			return;
		}
		emptyState.Visibility = Visibility.Collapsed;
		mainGrid.Columns.Clear();
		(string, string, double)[] array = new(string, string, double)[7]
		{
			("OR No", "or_no", 120.0),
			("Resident", "resident_name", 190.0),
			("Item", "item_type", 150.0),
			("Amount", "amount", 120.0),
			("Method", "payment_method", 110.0),
			("Status", "payment_status", 95.0),
			("Date Paid", "paid_at", 160.0)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (header, path, value) = array[i];
			mainGrid.Columns.Add(new DataGridTextColumn
			{
				Header = header,
				Binding = new Binding(path)
				{
					StringFormat = path == "amount" ? "PHP {0:N2}" : null
				},
				Width = new DataGridLength(value, DataGridLengthUnitType.Auto)
			});
		}
		mainGrid.ItemsSource = table.DefaultView;
	}

	private void PopulateFilterOptions(DataTable? table)
	{
		typeFilter.Items.Clear();
		typeFilter.Items.Add("All Types");
		methodFilter.Items.Clear();
		methodFilter.Items.Add("All Methods");
		statusFilter.Items.Clear();
		statusFilter.Items.Add("All Statuses");
		if (table != null)
		{
			foreach (string item in from value in (from row in table.AsEnumerable()
					select Convert.ToString(row["item_type"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				typeFilter.Items.Add(item);
			}
			foreach (string item2 in from value in (from row in table.AsEnumerable()
					select Convert.ToString(row["payment_method"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				methodFilter.Items.Add(item2);
			}
			foreach (string status in table.AsEnumerable()
				.Select(row => Convert.ToString(row["payment_status"]) ?? string.Empty)
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(value => value))
			{
				statusFilter.Items.Add(status);
			}
		}
		typeFilter.SelectedIndex = 0;
		methodFilter.SelectedIndex = 0;
		statusFilter.SelectedIndex = 0;
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded)
		{
			ApplyFilters();
		}
	}

	private void ApplyFilters()
	{
		if (_data == null)
		{
			return;
		}
		List<string> list = new List<string>();
		string value = searchBox.Text.Trim();
		if (!string.IsNullOrWhiteSpace(value))
		{
			string escaped = EscapeForRowFilter(value);
			string[] source = new string[6] { "or_no", "resident_name", "item_type", "amount", "payment_method", "paid_at" };
			list.Add("(" + string.Join(" OR ", source.Select((string column) => $"Convert([{column}], 'System.String') LIKE '%{escaped}%'")) + ")");
		}
		if (typeFilter.SelectedItem is string text && !string.Equals(text, "All Types", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[item_type] = '" + EscapeForRowFilter(text) + "'");
		}
		if (methodFilter.SelectedItem is string text2 && !string.Equals(text2, "All Methods", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[payment_method] = '" + EscapeForRowFilter(text2) + "'");
		}
		if (statusFilter.SelectedItem is string status && !string.Equals(status, "All Statuses", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[payment_status] = '" + EscapeForRowFilter(status) + "'");
		}
		_data.DefaultView.RowFilter = string.Join(" AND ", list);
		int count = _data.DefaultView.Count;
		decimal paidTotal = _data.DefaultView.Cast<DataRowView>()
			.Where(row => !string.Equals(Convert.ToString(row["payment_status"]), "VOID", StringComparison.OrdinalIgnoreCase))
			.Sum(row => row["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["amount"]));
		int voidCount = _data.DefaultView.Cast<DataRowView>()
			.Count(row => string.Equals(Convert.ToString(row["payment_status"]), "VOID", StringComparison.OrdinalIgnoreCase));
		emptyState.Visibility = ((count != 0) ? Visibility.Collapsed : Visibility.Visible);
		emptyLabel.Text = ((count == 0) ? "No transactions match the current filters." : "No payment history found for current filters.");
		footerCountLabel.Text = $"Showing {count:N0} transaction(s)";
		recordCountLabel.Text = $"{count:N0} payment transaction(s)";
		ledgerSummaryLabel.Text = $"Valid collection: PHP {paidTotal:N2}  ·  {voidCount:N0} void";
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			return;
		}
		contextActionBar.Visibility = Visibility.Visible;
		selectedRecordLabel.Text = Convert.ToString(dataRowView["or_no"]) ?? "Unknown OR";
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.SelectedItem = null;
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		PaymentWindow window = new PaymentWindow();
		var adapter = new DialogContentAdapter(window);

		var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Payment", IconChar.Save,
			(s, args) =>
			{
				NavigationService.Instance.NavigateBackFromFullscreen("ResidentPayments", refreshOnReturn: true);
			});

		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "Record New Payment",
			Subtitle = "Process a new payment transaction",
			OriginRoute = "ResidentPayments",
			Content = adapter,
			Icon = IconChar.MoneyBill,
			ToolbarItems = new List<UIElement> { saveButton },
			ShowSideToolbar = false,
			OnSaved = () => RefreshData()
		});
	}

	private void BtnPrintOR_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select a payment first.");
			return;
		}
		bool isVoid = string.Equals(Convert.ToString(dataRowView["payment_status"]), "VOID", StringComparison.OrdinalIgnoreCase);
		string receipt = $"{(isVoid ? "VOID — NOT VALID FOR COLLECTION" : "OFFICIAL PAYMENT RECORD")}\n\nOfficial Receipt: {dataRowView["or_no"]}\nResident: {dataRowView["resident_name"]}\nAmount: PHP {Convert.ToDecimal(dataRowView["amount"]):N2}\nPaid: {dataRowView["paid_at"]}";
		if (isVoid)
		{
			receipt += $"\nVoid reason: {dataRowView["void_reason"]}\nVoided: {dataRowView["voided_at"]}";
		}
		DialogService.Instance.ShowInfo(receipt, isVoid ? "Void Payment Record" : "Payment Receipt");
	}

	private void BtnView_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select a payment row first.");
			return;
		}
		string details = $"Status: {dataRowView["payment_status"]}\nOR No: {dataRowView["or_no"]}\nResident: {dataRowView["resident_name"]}\nItem: {dataRowView["item_type"]}\nAmount: PHP {Convert.ToDecimal(dataRowView["amount"]):N2}\nMethod: {dataRowView["payment_method"]}\nDate Paid: {dataRowView["paid_at"]}\nReference: {dataRowView["document_no"]}\n\n{dataRowView["remarks"]}";
		if (string.Equals(Convert.ToString(dataRowView["payment_status"]), "VOID", StringComparison.OrdinalIgnoreCase))
		{
			details += $"\n\nVoid reason: {dataRowView["void_reason"]}\nVoided: {dataRowView["voided_at"]}";
		}
		DialogService.Instance.ShowInfo(details, "Payment Details");
	}

	private async void BtnVoid_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView row))
		{
			DialogService.Instance.ShowWarning("Select a payment to void.");
			return;
		}
		if (string.Equals(Convert.ToString(row["payment_status"]), "VOID", StringComparison.OrdinalIgnoreCase))
		{
			DialogService.Instance.ShowWarning("This payment is already void.");
			return;
		}
		string? reason = DialogService.Instance.PromptForReason(
			"Void Payment",
			$"Explain why payment {row["or_no"]} for {row["resident_name"]} must be voided. The original entry will remain in the audit ledger.",
			"Void Payment");
		if (reason == null)
		{
			return;
		}
		if (!DialogService.Instance.Confirm(
			$"Void payment {row["or_no"]} for PHP {Convert.ToDecimal(row["amount"]):N2}?\n\nThis keeps the original entry but removes it from valid collection totals.",
			"Confirm Payment Void"))
		{
			return;
		}
		try
		{
			await _paymentLedgerService.VoidPaymentAsync(
				Convert.ToString(row["payment_source"]) ?? string.Empty,
				Convert.ToInt32(row["payment_id"]),
				reason);
			DialogService.Instance.ShowInfo("Payment voided. The original ledger entry was retained for audit.");
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Payment void failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Void Payment");
		}
	}

	private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
	{
		if (_data == null || _data.DefaultView.Count == 0)
		{
			DialogService.Instance.ShowWarning("There are no visible payment records to export.");
			return;
		}
		SaveFileDialog dialog = new SaveFileDialog
		{
			Title = "Export Payment Ledger",
			Filter = "CSV file (*.csv)|*.csv",
			FileName = $"payment-ledger-{DateTime.Now:yyyyMMdd-HHmm}.csv"
		};
		if (dialog.ShowDialog() != true)
		{
			return;
		}
		try
		{
			using StreamWriter writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true));
			writer.WriteLine("OR Number,Resident,Item,Amount,Method,Status,Paid At,Document,Remarks,Void Reason,Voided At");
			foreach (DataRowView row in _data.DefaultView)
			{
				string[] values =
				{
					Convert.ToString(row["or_no"]) ?? string.Empty,
					Convert.ToString(row["resident_name"]) ?? string.Empty,
					Convert.ToString(row["item_type"]) ?? string.Empty,
					Convert.ToDecimal(row["amount"]).ToString("0.00"),
					Convert.ToString(row["payment_method"]) ?? string.Empty,
					Convert.ToString(row["payment_status"]) ?? string.Empty,
					Convert.ToString(row["paid_at"]) ?? string.Empty,
					Convert.ToString(row["document_no"]) ?? string.Empty,
					Convert.ToString(row["remarks"]) ?? string.Empty,
					Convert.ToString(row["void_reason"]) ?? string.Empty,
					Convert.ToString(row["voided_at"]) ?? string.Empty
				};
				writer.WriteLine(string.Join(",", values.Select(CsvEscape)));
			}
			DialogService.Instance.ShowInfo($"Exported {_data.DefaultView.Count:N0} payment record(s).", "Export Complete");
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Payment export failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Export Payment Ledger");
		}
	}

	private static string CsvEscape(string value)
	{
		return "\"" + value.Replace("\"", "\"\"") + "\"";
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
