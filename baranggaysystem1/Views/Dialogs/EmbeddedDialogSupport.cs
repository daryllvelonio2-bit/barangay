using System;
using System.Windows;

namespace baranggaysystem1.Views.Dialogs;

/// <summary>
/// Completes a form consistently whether it is shown as a modal Window or hosted
/// inside DialogContentAdapter's fullscreen workspace.
/// </summary>
internal static class EmbeddedDialogSupport
{
    public static void Complete(Window window)
    {
        try
        {
            window.DialogResult = true;
        }
        catch (InvalidOperationException)
        {
            window.Tag = true;
            window.Close();
        }
    }
}
