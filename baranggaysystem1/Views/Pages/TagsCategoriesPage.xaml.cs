using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class TagsCategoriesPage : UserControl, IRefreshable
{
	private readonly ResidentClassificationService _service = new ResidentClassificationService();

	private IReadOnlyList<ResidentClassificationRecord> _records = Array.Empty<ResidentClassificationRecord>();

	private bool _isLoaded;

















	private bool CanManage => ResidentClassificationService.CanManageClassifications();

	private ResidentClassificationRecord? SelectedRecord => mainGrid.SelectedItem as ResidentClassificationRecord;

	public TagsCategoriesPage()
	{
		InitializeComponent();
		ConfigureFilters();
		base.Loaded += async delegate
		{
			if (!_isLoaded)
			{
				_isLoaded = true;
				await LoadAsync();
			}
		};
	}

	public TagsCategoriesPage(string route)
		: this()
	{
	}

	private void ConfigureFilters()
	{
		typeFilter.ItemsSource = new string[3] { "All Types", "Categories", "Tags" };
		statusFilter.ItemsSource = new string[3] { "All Statuses", "Active", "Archived" };
		typeFilter.SelectedIndex = 0;
		statusFilter.SelectedIndex = 0;
	}

	private async Task LoadAsync(int? selectId = null)
	{
		if (!CanManage)
		{
			btnAdd.IsEnabled = false;
			btnRefresh.IsEnabled = false;
			mainGrid.ItemsSource = null;
			contextActionBar.Visibility = Visibility.Collapsed;
			emptyLabel.Text = "Only administrator accounts can manage tags and categories.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Access restricted.";
			recordCountLabel.Text = "Access restricted.";
			return;
		}
		try
		{
			SetLoadingState(isLoading: true);
			_records = await _service.GetClassificationsAsync();
			ApplyFilters(selectId);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("TagsCategoriesPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load tags and categories. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Tags and categories could not be loaded.";
		}
		finally
		{
			SetLoadingState(isLoading: false);
		}
	}

	private void SetLoadingState(bool isLoading)
	{
		btnAdd.IsEnabled = !isLoading && CanManage;
		btnRefresh.IsEnabled = !isLoading && CanManage;
		footerCountLabel.Text = (isLoading ? "Loading tags and categories..." : footerCountLabel.Text);
	}

	private void ApplyFilters(int? selectId = null)
	{
		string query = searchBox.Text.Trim();
		string type = Convert.ToString(typeFilter.SelectedItem) ?? "All Types";
		string status = Convert.ToString(statusFilter.SelectedItem) ?? "All Statuses";
		List<ResidentClassificationRecord> list = (from record in _records
			where MatchesSearch(record, query)
			where MatchesType(record, type)
			where MatchesStatus(record, status)
			select record).ToList();
		mainGrid.ItemsSource = list;
		emptyLabel.Text = (string.IsNullOrWhiteSpace(query) ? "No tags or categories found." : "No matching tags or categories found.");
		emptyState.Visibility = ((list.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		int num = _records.Count((ResidentClassificationRecord record) => string.Equals(record.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase));
		int value = _records.Count - num;
		recordCountLabel.Text = $"{num:N0} active, {value:N0} archived classification(s).";
		footerCountLabel.Text = $"Showing {list.Count:N0} of {_records.Count:N0} tag/category record(s).";
		if (selectId.HasValue)
		{
			ResidentClassificationRecord residentClassificationRecord = list.FirstOrDefault((ResidentClassificationRecord record) => record.ClassificationId == selectId.Value);
			mainGrid.SelectedItem = residentClassificationRecord;
			if (residentClassificationRecord != null)
			{
				mainGrid.ScrollIntoView(residentClassificationRecord);
			}
		}
		UpdateSelectionState();
	}

	private static bool MatchesSearch(ResidentClassificationRecord record, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return true;
		}
		if (!Contains(record.Name, query) && !Contains(record.Description, query) && !Contains(record.TypeDisplay, query) && !Contains(record.StatusDisplay, query))
		{
			return Contains(record.SourceDisplay, query);
		}
		return true;
	}

	private static bool MatchesType(ResidentClassificationRecord record, string type)
	{
		if (string.Equals(type, "Categories", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.ClassificationType, "CATEGORY", StringComparison.OrdinalIgnoreCase);
		}
		if (string.Equals(type, "Tags", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.ClassificationType, "TAG", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool MatchesStatus(ResidentClassificationRecord record, string status)
	{
		if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
		}
		if (string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool Contains(string? value, string query)
	{
		return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (mainGrid != null)
		{
			ApplyFilters();
		}
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateSelectionState();
	}

	private void UpdateSelectionState()
	{
		ResidentClassificationRecord selectedRecord = SelectedRecord;
		if (selectedRecord == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			return;
		}
		contextActionBar.Visibility = Visibility.Visible;
		selectedRecordLabel.Text = selectedRecord.Name;
		selectedRecordMetaLabel.Text = $"{selectedRecord.TypeDisplay} - {selectedRecord.StatusDisplay} - {selectedRecord.SourceDisplay} - {selectedRecord.UsageDisplay}";
		toggleStatusText.Text = (string.Equals(selectedRecord.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "Reactivate" : "Archive");
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.SelectedItem = null;
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync(SelectedRecord?.ClassificationId);
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		FullscreenDialogNavigator.Open(
			new ResidentClassificationWindow(),
			"New Tag or Category",
			"Create one reusable classification for resident records.",
			"ResidentCategories",
			IconChar.UserEdit,
			"Create Classification",
			RefreshData);
	}

	private async void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		await OpenSelectedRecordAsync();
	}

	private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		await OpenSelectedRecordAsync();
	}

	private async Task OpenSelectedRecordAsync()
	{
		ResidentClassificationRecord selectedRecord = SelectedRecord;
		if (selectedRecord == null)
		{
			DialogService.Instance.ShowWarning("Please select a tag or category to edit.", "Tags & Categories");
			return;
		}
		ResidentClassificationRecord residentClassificationRecord = await _service.GetClassificationAsync(selectedRecord.ClassificationId);
		if (residentClassificationRecord == null)
		{
			DialogService.Instance.ShowWarning("The selected tag or category could not be found anymore.", "Tags & Categories");
			await LoadAsync();
			return;
		}
		ResidentClassificationWindow residentClassificationWindow = new ResidentClassificationWindow(residentClassificationRecord);
		FullscreenDialogNavigator.Open(
			residentClassificationWindow,
			"Edit Tag or Category",
			residentClassificationRecord.Name,
			"ResidentCategories",
			IconChar.Edit,
			"Save Changes",
			RefreshData);
	}

	private async void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
	{
		ResidentClassificationRecord record = SelectedRecord;
		if (record == null)
		{
			DialogService.Instance.ShowWarning("Please select a tag or category first.", "Tags & Categories");
			return;
		}
		string text = (string.Equals(record.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "ARCHIVED");
		string value = (string.Equals(text, "ACTIVE", StringComparison.OrdinalIgnoreCase) ? "reactivate" : "archive");
		if (!DialogService.Instance.Confirm($"Do you want to {value} '{record.Name}'?", "Tags & Categories"))
		{
			return;
		}
		try
		{
			await _service.SetStatusAsync(record.ClassificationId, text);
			await LoadAsync(record.ClassificationId);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("TagsCategoriesPage status update failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Tags & Categories");
		}
	}

	public void RefreshData() => _ = LoadAsync();

	}
