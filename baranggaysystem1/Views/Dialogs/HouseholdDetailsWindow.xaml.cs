using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class HouseholdDetailsWindow : Window
{
	private readonly HouseholdDetailsViewModel _vm;
	public HouseholdDetailsWindow(int? householdId, int? currentResidentId = null)
	{
		InitializeComponent();
		_vm = new HouseholdDetailsViewModel(householdId, currentResidentId);
		base.DataContext = _vm;
	}

	private async void BtnAddMember_Click(object sender, RoutedEventArgs e)
	{
		if (!_vm.CanManageMembers || !_vm.HouseholdId.HasValue)
		{
			DialogService.Instance.ShowWarning("Create or select a household first before adding family members.");
			return;
		}

		var picker = new HouseholdMemberPickerWindow(_vm.HouseholdId.Value);

		// Assign the nearest real parent Window as owner (supports both dialog and embedded/fullscreen modes)
		var ownerWindow = Window.GetWindow(this);
		if (ownerWindow != null && ownerWindow != this)
		{
			picker.Owner = ownerWindow;
		}

		if (picker.ShowDialog().GetValueOrDefault())
		{
			_vm.MarkChanged();
			await _vm.ReloadAsync();
		}
	}

	private void BtnClose_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			base.DialogResult = _vm.HasChanges;
		}
		catch (InvalidOperationException)
		{
			// Window is embedded via DialogContentAdapter, not shown as a dialog.
			// Signal result via Tag so the adapter can detect it.
			base.Tag = _vm.HasChanges;
			Close();
		}
	}

	private void BtnConfirm_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			base.DialogResult = true;
		}
		catch (InvalidOperationException)
		{
			base.Tag = true;
			Close();
		}
	}}
