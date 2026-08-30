using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class PaymentWindow : Window
{
	private readonly PaymentViewModel _viewModel;
	public PaymentWindow()
		: this(new PaymentViewModel())
	{
	}

	public PaymentWindow(int residentId, string residentName)
		: this(new PaymentViewModel(residentId, residentName))
	{
	}

	private PaymentWindow(PaymentViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		base.DataContext = _viewModel;
		base.Loaded += PaymentWindow_Loaded;
		_viewModel.CloseRequested += delegate(bool? dialogResult)
		{
			if (base.Dispatcher.CheckAccess())
			{
				TrySetDialogResult(dialogResult);
			}
			else
			{
				base.Dispatcher.Invoke(() => TrySetDialogResult(dialogResult));
			}
		};
	}

	private void TrySetDialogResult(bool? dialogResult)
	{
		if (dialogResult == true)
			EmbeddedDialogSupport.Complete(this);
		else
			Close();
	}

	private async void PaymentWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await _viewModel.InitializeAsync();
	}
}
