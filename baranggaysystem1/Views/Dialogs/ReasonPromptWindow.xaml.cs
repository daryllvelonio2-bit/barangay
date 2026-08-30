using System.Windows;

namespace baranggaysystem1.Views.Dialogs;

public partial class ReasonPromptWindow : Window
{
	public string Reason => reasonBox.Text.Trim();

	public ReasonPromptWindow(string title, string prompt, string confirmText = "Confirm")
	{
		InitializeComponent();
		Title = title;
		titleText.Text = title;
		promptText.Text = prompt;
		confirmButton.Content = confirmText;
		Loaded += (sender, args) => reasonBox.Focus();
	}

	private void ConfirmButton_Click(object sender, RoutedEventArgs e)
	{
		if (Reason.Length < 5)
		{
			validationText.Visibility = Visibility.Visible;
			reasonBox.Focus();
			return;
		}
		DialogResult = true;
	}
}
