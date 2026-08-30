using System;
using System.IO;
using System.Windows;
using baranggaysystem1.Database;
using Microsoft.Win32;

namespace baranggaysystem1.Views.Dialogs;

public partial class CustomDatabaseConnectionWindow : Window
{
    public DatabaseConnectionProfile ConnectionProfile { get; private set; } = DatabaseConnectionProfile.CreateDefault();

    public string? BackupZipPath { get; private set; }

    public CustomDatabaseConnectionWindow(DatabaseConnectionProfile initialProfile)
    {
        InitializeComponent();
        LoadProfile(initialProfile);
    }

    private void LoadProfile(DatabaseConnectionProfile profile)
    {
        DatabaseConnectionProfile value = profile ?? DatabaseConnectionProfile.CreateDefault();
        txtDbHost.Text = value.Server;
        txtDbPort.Text = value.Port.ToString();
        txtDbName.Text = value.Database;
        txtDbUser.Text = value.Username;
        txtDbPassword.Password = value.Password;
        chkDbUseSsl.IsChecked = value.UseSsl;
        txtBackupZipPath.Text = string.Empty;
        ConnectionProfile = value;
    }

    private void BtnBrowseBackup_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "Select backup ZIP",
            Filter = "ZIP Backup Files|*.zip|All Files|*.*",
            CheckFileExists = true,
            CheckPathExists = true
        };
        if (dialog.ShowDialog() == true)
        {
            txtBackupZipPath.Text = dialog.FileName;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ConnectionProfile = new DatabaseConnectionProfile
            {
                Server = RequireValue(txtDbHost.Text, "Database host is required."),
                Port = ParsePort(txtDbPort.Text),
                Database = RequireValue(txtDbName.Text, "Database name is required."),
                Username = RequireValue(txtDbUser.Text, "Username is required."),
                Password = txtDbPassword.Password ?? string.Empty,
                UseSsl = chkDbUseSsl.IsChecked.GetValueOrDefault()
            };
            string text = txtBackupZipPath.Text.Trim();
            if (!string.IsNullOrWhiteSpace(text) && !File.Exists(text))
            {
                throw new InvalidOperationException("The selected backup ZIP file could not be found.");
            }
            BackupZipPath = (string.IsNullOrWhiteSpace(text) ? null : text);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Custom Database Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string RequireValue(string? value, string message)
    {
        string text = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(message);
        }
        return text;
    }

    private static uint ParsePort(string? rawPort)
    {
        string text = rawPort?.Trim() ?? string.Empty;
        if (!uint.TryParse(text, out uint result) || result == 0 || result > 65535)
        {
            throw new InvalidOperationException("Port must be a number between 1 and 65535.");
        }
        return result;
    }
}