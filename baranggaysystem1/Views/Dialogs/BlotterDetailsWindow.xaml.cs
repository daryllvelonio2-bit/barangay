using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class BlotterDetailsWindow : Window
{
	private readonly BlotterDetailsViewModel _viewModel;
	public BlotterDetailsWindow(BlotterDto? existingRecord = null)
	{
		InitializeComponent();
		base.WindowState = WindowState.Maximized;
		_viewModel = new BlotterDetailsViewModel(existingRecord)
		{
			CloseAction = saved =>
			{
				if (saved)
					EmbeddedDialogSupport.Complete(this);
				else
					Close();
			}
		};
		base.DataContext = _viewModel;
		base.Loaded += OnLoadedAsync;
	}

	private async void OnLoadedAsync(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoadedAsync;
		try
		{
			await _viewModel.InitializeAsync();
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("SQLite mode"))
		{
			// In offline/SQLite mode, initialize with just the seed data (no DB load needed for new cases)
			AppLogger.LogWarning("Blotter form running in offline mode - limited functionality.", ex);
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError(ex.Message, "Blotter");
		}
	}}
