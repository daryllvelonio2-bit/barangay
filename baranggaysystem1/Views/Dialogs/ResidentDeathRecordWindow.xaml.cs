using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using baranggaysystem1.helper;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class ResidentDeathRecordWindow : Window
{
	private readonly ResidentDeathRecordService _service = new();
	private readonly BarangayOfficialService _residentService = new();
	private int _step;

	public ResidentDeathRecordWindow()
	{
		InitializeComponent();
		deathDatePicker.SelectedDate = DateTime.Today;
		Loaded += async (_, _) => await LoadResidentsAsync();
	}

	private async Task LoadResidentsAsync()
	{
		try
		{
			residentCombo.ItemsSource = (await _residentService.GetResidentOptionsAsync())
				.Where(x => !x.Status.Equals("DECEASED", StringComparison.OrdinalIgnoreCase))
				.OrderBy(x => x.FullName).ToList();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Death record resident list failed.", ex);
			DialogService.Instance.ShowError("The resident list could not be loaded.");
		}
	}

	private void Back_Click(object sender, RoutedEventArgs e)
	{
		if (_step > 0) _step--;
		UpdateStep();
	}

	private async void Next_Click(object sender, RoutedEventArgs e)
	{
		if (_step == 0 && residentCombo.SelectedItem is not OfficialResidentOption)
		{
			DialogService.Instance.ShowWarning("Select the resident first.");
			return;
		}
		if (_step == 1)
		{
			if (!deathDatePicker.SelectedDate.HasValue || deathDatePicker.SelectedDate.Value.Date > DateTime.Today)
			{
				DialogService.Instance.ShowWarning("Enter a valid date of death.");
				return;
			}
			if (referenceBox.Text.Trim().Length < 3 || reportedByBox.Text.Trim().Length < 3)
			{
				DialogService.Instance.ShowWarning("Enter the supporting reference and verifier.");
				return;
			}
		}
		if (_step < 2)
		{
			_step++;
			UpdateStep();
			return;
		}
		if (verifiedCheck.IsChecked != true)
		{
			DialogService.Instance.ShowWarning("Confirm that the identity and supporting record were verified.");
			return;
		}
		var resident = (OfficialResidentOption)residentCombo.SelectedItem;
		try
		{
			nextButton.IsEnabled = false;
			await _service.ConfirmAsync(new ResidentDeathRecord
			{
				ResidentId = resident.ResidentId,
				ResidentName = resident.FullName,
				DateOfDeath = deathDatePicker.SelectedDate!.Value,
				PlaceOfDeath = placeBox.Text,
				CauseOfDeath = causeBox.Text,
				CertificateReference = referenceBox.Text,
				ReportedBy = reportedByBox.Text,
				Notes = notesBox.Text
			});
			EmbeddedDialogSupport.Complete(this);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Confirm death record failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
			nextButton.IsEnabled = true;
		}
	}

	private void UpdateStep()
	{
		residentStep.Visibility = _step == 0 ? Visibility.Visible : Visibility.Collapsed;
		detailsStep.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
		reviewStep.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
		backButton.Visibility = _step == 0 ? Visibility.Collapsed : Visibility.Visible;
		nextButton.Content = _step == 2 ? "Confirm Record" : "Continue";
		stepTitle.Text = _step switch { 0 => "1. Select resident", 1 => "2. Record death details", _ => "3. Verify and confirm" };
		stepHint.Text = _step switch
		{
			0 => "Find the active resident whose record must be updated.",
			1 => "Capture the official details and supporting reference.",
			_ => "Check the record before changing resident status."
		};
		if (_step == 2 && residentCombo.SelectedItem is OfficialResidentOption resident)
		{
			reviewText.Text =
				$"Resident: {resident.FullName}\n" +
				$"Date of death: {deathDatePicker.SelectedDate:MMMM d, yyyy}\n" +
				$"Place: {placeBox.Text.Trim()}\n" +
				$"Cause: {causeBox.Text.Trim()}\n" +
				$"Reference: {referenceBox.Text.Trim()}\n" +
				$"Reported / verified by: {reportedByBox.Text.Trim()}";
		}
	}
}
