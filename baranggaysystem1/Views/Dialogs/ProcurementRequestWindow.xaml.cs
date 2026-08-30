using System;
using System.Globalization;
using System.Windows;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class ProcurementRequestWindow : Window
{
	private readonly FinanceOperationsService _service = new();
	private readonly ProcurementRequestRecord _existing;

	public ProcurementRequestWindow(ProcurementRequestRecord? existing = null)
	{
		_existing = existing ?? new ProcurementRequestRecord();
		InitializeComponent();
		typeCombo.ItemsSource = new[] { "PROCUREMENT", "PURCHASE ORDER", "EMERGENCY PURCHASE" };
		typeCombo.Text = _existing.RequestType;
		requestDatePicker.SelectedDate = _existing.RequestDate == default ? DateTime.Today : _existing.RequestDate;
		neededByPicker.SelectedDate = _existing.NeededByDate;
		titleBox.Text = _existing.RequestTitle;
		categoryBox.Text = _existing.ProcurementCategory;
		itemsBox.Text = _existing.ItemSummary;
		amountBox.Text = _existing.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
		vendorBox.Text = _existing.VendorName;
		poBox.Text = _existing.PurchaseOrderNo;
		notesBox.Text = _existing.Notes;
		saveButton.Content = _existing.ProcurementId > 0 ? "Save Changes" : "Save Draft";
	}

	private async void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (!decimal.TryParse(amountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal amount) &&
			!decimal.TryParse(amountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
		{
			DialogService.Instance.ShowWarning("Enter a valid estimated amount.");
			return;
		}
		try
		{
			saveButton.IsEnabled = false;
			await _service.SaveProcurementRequestAsync(new ProcurementRequestRecord
			{
				ProcurementId = _existing.ProcurementId,
				RequestType = typeCombo.Text,
				RequestDate = requestDatePicker.SelectedDate ?? DateTime.Today,
				NeededByDate = neededByPicker.SelectedDate,
				RequestTitle = titleBox.Text,
				ProcurementCategory = categoryBox.Text,
				VendorName = vendorBox.Text,
				RequestedByName = _existing.RequestedByName,
				TotalAmount = amount,
				WorkflowStatus = _existing.WorkflowStatus,
				PurchaseOrderNo = poBox.Text,
				ApprovedByName = _existing.ApprovedByName,
				ApprovedAt = _existing.ApprovedAt,
				ItemSummary = itemsBox.Text,
				ApprovalNotes = _existing.ApprovalNotes,
				Notes = notesBox.Text
			});
			EmbeddedDialogSupport.Complete(this);
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError(ex.Message, "Procurement Request");
			saveButton.IsEnabled = true;
		}
	}
}
