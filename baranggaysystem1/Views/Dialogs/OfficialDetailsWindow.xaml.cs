using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class OfficialDetailsWindow : Window
{
	private readonly BarangayOfficialService _service = new();
	private readonly int _officialId;
	private BarangayOfficial? _existing;

	public OfficialDetailsWindow() : this(0) { }

	public OfficialDetailsWindow(int officialId)
	{
		_officialId = officialId;
		InitializeComponent();
		titleLabel.Text = officialId > 0 ? "Edit Official Appointment" : "New Official Appointment";
		Loaded += async (_, _) => await LoadAsync();
	}

	private async Task LoadAsync()
	{
		try
		{
			var residents = (await _service.GetResidentOptionsAsync()).OrderBy(x => x.FullName).ToList();
			var terms = (await _service.GetTermOptionsAsync()).ToList();
			terms.Add(_service.CreateNewTermOption());
			residentCombo.ItemsSource = residents;
			termCombo.ItemsSource = terms;
			if (_officialId > 0)
			{
				_existing = await _service.GetBarangayOfficialDetailsAsync(_officialId)
					?? throw new InvalidOperationException("The official appointment no longer exists.");
				residentCombo.SelectedItem = residents.FirstOrDefault(x => x.ResidentId == _existing.ResidentId);
				termCombo.SelectedItem = terms.FirstOrDefault(x => x.TermId == _existing.TermId);
				positionBox.Text = _existing.Position;
				committeeBox.Text = _existing.Committee;
				foreach (ComboBoxItem item in statusCombo.Items)
					if (string.Equals(Convert.ToString(item.Content), _existing.Status, StringComparison.OrdinalIgnoreCase))
						statusCombo.SelectedItem = item;
			}
			else
			{
				termCombo.SelectedItem = terms.FirstOrDefault(x => x.IsCurrent) ?? terms.First();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Official appointment form load failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
			btnConfirm.IsEnabled = false;
		}
	}

	private void TermCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		newTermPanel.Visibility = termCombo.SelectedItem is OfficialTermOption term && term.IsCreateNewOption
			? Visibility.Visible : Visibility.Collapsed;
	}

	private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
	{
		if (residentCombo.SelectedItem is not OfficialResidentOption resident)
		{
			DialogService.Instance.ShowWarning("Select a resident.");
			return;
		}
		if (termCombo.SelectedItem is not OfficialTermOption term)
		{
			DialogService.Instance.ShowWarning("Select an official term.");
			return;
		}
		string position = positionBox.Text.Trim();
		if (position.Length < 2)
		{
			DialogService.Instance.ShowWarning("Enter the official position.");
			return;
		}
		if (term.IsCreateNewOption &&
			(!termStartPicker.SelectedDate.HasValue || !termEndPicker.SelectedDate.HasValue ||
			 termEndPicker.SelectedDate.Value.Date < termStartPicker.SelectedDate.Value.Date))
		{
			DialogService.Instance.ShowWarning("Enter a valid start and end date for the new term.");
			return;
		}
		int validationTermId = term.IsCreateNewOption ? 0 : term.TermId;
		if (validationTermId > 0 &&
			await _service.OfficialAssignmentExistsAsync(resident.ResidentId, validationTermId, _officialId))
		{
			DialogService.Instance.ShowWarning("This resident already has an appointment in the selected term.");
			return;
		}
		var model = new BarangayOfficial
		{
			OfficialId = _officialId,
			ResidentId = resident.ResidentId,
			TermId = validationTermId,
			Position = position,
			Committee = committeeBox.Text.Trim(),
			Status = Convert.ToString((statusCombo.SelectedItem as ComboBoxItem)?.Content) ?? "ACTIVE",
			CreateNewTerm = term.IsCreateNewOption,
			TermStart = termStartPicker.SelectedDate,
			TermEnd = termEndPicker.SelectedDate,
			TermNotes = termNotesBox.Text.Trim()
		};
		try
		{
			btnConfirm.IsEnabled = false;
			if (_officialId > 0) await _service.UpdateBarangayOfficialAsync(model);
			else await _service.AddBarangayOfficialAsync(model);
			EmbeddedDialogSupport.Complete(this);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Official appointment save failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
			btnConfirm.IsEnabled = true;
		}
	}
}
