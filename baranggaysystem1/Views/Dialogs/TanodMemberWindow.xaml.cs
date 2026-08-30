using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class TanodMemberWindow : Window
{
    private readonly TanodService _service = new TanodService();
    private readonly int? _tanodId;

    public TanodMemberWindow() : this(null)
    {
    }

    public TanodMemberWindow(int? tanodId)
    {
        InitializeComponent();
        _tanodId = tanodId;
        dpAssigned.SelectedDate = DateTime.Today;
        if (tanodId.HasValue)
        {
            Title = "Edit Tanod Member";
            headerEyebrow.Text = "EDIT TANOD";
            headerTitle.Text = "Update tanod member";
            btnSave.Content = "Save Changes";
            chkActive.Visibility = Visibility.Visible;
            _ = LoadAsync(tanodId.Value);
        }
    }

    private async Task LoadAsync(int tanodId)
    {
        try
        {
            DataTable table = await _service.GetMemberAsync(tanodId);
            if (table.Rows.Count == 0) return;
            DataRow row = table.Rows[0];
            txtFullName.Text = row["full_name"]?.ToString() ?? string.Empty;
            txtContact.Text = row["contact_number"]?.ToString() ?? string.Empty;
            txtRank.Text = row["rank_title"]?.ToString() ?? string.Empty;
            txtRemarks.Text = row["remarks"]?.ToString() ?? string.Empty;
            if (row["date_assigned"] != DBNull.Value)
                dpAssigned.SelectedDate = Convert.ToDateTime(row["date_assigned"]);
            chkActive.IsChecked = row["is_active"] != DBNull.Value
                                  && Convert.ToInt32(row["is_active"]) == 1;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load tanod member failed.", ex);
            MessageBox.Show(ex.Message, "Unable to load member",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtFullName.Text))
        {
            MessageBox.Show("Full name is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtFullName.Focus();
            return;
        }

        btnSave.IsEnabled = false;
        try
        {
            if (_tanodId.HasValue)
            {
                await _service.UpdateMemberAsync(_tanodId.Value, txtFullName.Text, txtContact.Text,
                    txtRank.Text, dpAssigned.SelectedDate, chkActive.IsChecked == true, txtRemarks.Text);
            }
            else
            {
                await _service.CreateMemberAsync(txtFullName.Text, txtContact.Text, txtRank.Text,
                    dpAssigned.SelectedDate, null, txtRemarks.Text);
            }
            try
            {
                DialogResult = true;
            }
            catch (InvalidOperationException)
            {
                // Window hosted in fullscreen via DialogContentAdapter — use Tag as success flag
                this.Tag = true;
            }
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save tanod member failed.", ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnSave.IsEnabled = true;
        }
    }
}
