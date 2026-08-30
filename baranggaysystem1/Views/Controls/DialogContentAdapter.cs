using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using baranggaysystem1.ViewModels.Navigation;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Adapter that hosts a Window's visual content within a fullscreen view.
/// Extracts the root content from a dialog Window and presents it as a UserControl
/// suitable for embedding in FullscreenViewHost.
///
/// This enables gradual migration of existing dialog windows to fullscreen views
/// without requiring immediate rewrite of each dialog's internal logic.
/// The adapter intercepts DialogResult and Close() calls to integrate with
/// the fullscreen navigation flow.
/// </summary>
public class DialogContentAdapter : UserControl, IUnsavedChangesSource
{
    private readonly Window _dialogWindow;
    private bool _resultSet;
    private bool _trackingEnabled;
    private bool _inputHandlersAttached;

    public bool IsDirty { get; set; }

    /// <summary>
    /// Gets whether the dialog completed with a positive result (save/ok).
    /// </summary>
    public bool DialogResultPositive { get; private set; }

    /// <summary>
    /// Event raised when the dialog signals completion (save or cancel).
    /// </summary>
    public event EventHandler<bool>? DialogCompleted;

    /// <summary>
    /// Creates an adapter that hosts the given Window's content.
    /// The Window is never shown as a separate window — its content tree
    /// is extracted and displayed inline within the fullscreen view.
    /// </summary>
    /// <param name="dialogWindow">The dialog Window whose content to host.</param>
    public DialogContentAdapter(Window dialogWindow)
    {
        _dialogWindow = dialogWindow ?? throw new ArgumentNullException(nameof(dialogWindow));

        // Extract the content from the Window and host it in this UserControl
        var content = _dialogWindow.Content as UIElement;
        if (content != null)
        {
            _dialogWindow.Content = null; // Detach from Window's visual tree
            Content = content;

            // Propagate the Window's DataContext to the extracted content
            // so that bindings (e.g., IsProcessing) resolve correctly
            if (_dialogWindow.DataContext != null)
            {
                DataContext = _dialogWindow.DataContext;
            }
        }
        else
        {
            // Fallback: use a placeholder if content extraction fails
            Content = new TextBlock
            {
                Text = "Form content loading...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        // Fire the Window's Loaded logic so InitializeAsync runs
        Loaded += (s, e) =>
        {
            PrepareEmbeddedCancelButtons();
            AttachDirtyTracking();
            _dialogWindow.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    IsDirty = false;
                    _trackingEnabled = true;
                }));
        };

        _dialogWindow.Closed += (s, e) =>
        {
            bool success = false;
            try
            {
                if (_dialogWindow.DialogResult == true)
                {
                    success = true;
                }
            }
            catch (InvalidOperationException)
            {
                // Window was not shown as dialog
            }

            if (_dialogWindow.Tag is bool tagSuccess && tagSuccess)
            {
                success = true;
            }

            if (success)
            {
                SignalSuccess();
            }
            else
            {
                SignalCancel();
            }
        };
    }

    /// <summary>
    /// Searches for a save button (btnSave) in the hosted content's visual tree and programmatically clicks it.
    /// </summary>
    public void TriggerSave()
    {
        TriggerPrimaryAction();
    }

    public bool TriggerPrimaryAction()
    {
        var buttons = FindButtons(this)
            .Where(button => button.Visibility == Visibility.Visible && button.IsEnabled)
            .Select(button => new { Button = button, Score = ScorePrimaryAction(button) })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ToList();

        var primaryButton = buttons.FirstOrDefault()?.Button;
        if (primaryButton == null)
        {
            return false;
        }
        primaryButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        return true;
    }

    private void PrepareEmbeddedCancelButtons()
    {
        foreach (var button in FindButtons(this).Where(button => button.IsCancel))
        {
            button.IsCancel = false;
            button.Click += (sender, args) => _dialogWindow.Close();
        }
    }

    private void AttachDirtyTracking()
    {
        if (_inputHandlersAttached)
        {
            return;
        }
        _inputHandlersAttached = true;
        foreach (DependencyObject element in FindDescendants(this))
        {
            switch (element)
            {
                case TextBox textBox:
                    textBox.TextChanged += MarkDirty;
                    break;
                case PasswordBox passwordBox:
                    passwordBox.PasswordChanged += MarkDirty;
                    break;
                case Selector selector:
                    selector.SelectionChanged += MarkDirty;
                    break;
                case DatePicker datePicker:
                    datePicker.SelectedDateChanged += MarkDirty;
                    break;
                case ToggleButton toggle:
                    toggle.Checked += MarkDirty;
                    toggle.Unchecked += MarkDirty;
                    break;
            }
        }
    }

    private void MarkDirty(object sender, RoutedEventArgs e)
    {
        if (_trackingEnabled && !_resultSet)
        {
            IsDirty = true;
        }
    }

    private static IEnumerable<DependencyObject> FindDescendants(DependencyObject parent)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (DependencyObject descendant in FindDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<Button> FindButtons(DependencyObject parent)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, index);
            if (child is Button button)
            {
                yield return button;
            }
            foreach (var descendant in FindButtons(child))
            {
                yield return descendant;
            }
        }
    }

    private static int ScorePrimaryAction(Button button)
    {
        string name = button.Name ?? string.Empty;
        string label = GetContentText(button.Content).Trim();
        string normalizedLabel = label.ToUpperInvariant();
        if (normalizedLabel.Contains("CANCEL") ||
            normalizedLabel == "CLOSE" ||
            normalizedLabel.StartsWith("BACK") ||
            normalizedLabel.Contains("DELETE") ||
            normalizedLabel.Contains("REMOVE") ||
            normalizedLabel == "CLEAR")
        {
            return 0;
        }
        if (name.Equals("btnSave", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("saveButton", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("btnConfirm", StringComparison.OrdinalIgnoreCase))
        {
            return 120;
        }
        if (button.IsDefault)
        {
            return 110;
        }
        string[] actionPrefixes =
        {
            "SAVE", "CREATE", "UPDATE", "CONFIRM", "COMPLETE", "TRANSFER",
            "GENERATE", "POST", "RECORD", "ISSUE", "APPROVE", "ADD"
        };
        return actionPrefixes.Any(prefix => normalizedLabel.StartsWith(prefix, StringComparison.Ordinal))
            ? 100
            : 0;
    }

    private static string GetContentText(object? content)
    {
        return content switch
        {
            null => string.Empty,
            string text => text,
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            Panel panel => string.Join(" ", panel.Children
                .Cast<UIElement>()
                .OfType<ContentControl>()
                .Select(control => GetContentText(control.Content))
                .Concat(panel.Children.Cast<UIElement>().OfType<TextBlock>().Select(block => block.Text))),
            ContentControl contentControl => GetContentText(contentControl.Content),
            _ => Convert.ToString(content) ?? string.Empty
        };
    }

    /// <summary>
    /// Signals that the dialog completed successfully.
    /// Call this when the embedded form's save operation succeeds.
    /// </summary>
    public void SignalSuccess()
    {
        if (!_resultSet)
        {
            _resultSet = true;
            IsDirty = false;
            DialogResultPositive = true;
            DialogCompleted?.Invoke(this, true);
        }
    }

    /// <summary>
    /// Signals that the dialog was cancelled.
    /// </summary>
    public void SignalCancel()
    {
        if (!_resultSet)
        {
            _resultSet = true;
            IsDirty = false;
            DialogResultPositive = false;
            DialogCompleted?.Invoke(this, false);
        }
    }

    /// <summary>
    /// Gets the underlying dialog Window instance for accessing dialog-specific properties.
    /// </summary>
    public Window DialogWindow => _dialogWindow;
}
