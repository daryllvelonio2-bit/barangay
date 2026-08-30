using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class DashboardPage : UserControl
{
	private readonly AnnouncementService _announcementService = new AnnouncementService();

	private readonly DashboardReminderService _dashboardReminderService = new DashboardReminderService();

	private readonly ProjectService _projectService = new ProjectService();

	private readonly bool _showReminderEntry;

































	public DashboardPage(bool showReminderEntry = false)
	{
		_showReminderEntry = showReminderEntry;
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadDashboardAsync();
		};
		actionCalendar.SelectedDatesChanged += Calendar_SelectedDatesChanged;
		string text = UserSession.Username ?? "User";
		int hour = DateTime.Now.Hour;
		welcomeGreeting.Text = (((hour < 12) ? "Good morning" : ((hour < 17) ? "Good afternoon" : "Good evening")) + ", " + text).ToUpperInvariant();
		welcomeDate.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
		// Set avatar initial from username
		try
		{
			if (welcomeAvatarInitial != null && !string.IsNullOrEmpty(text))
			{
				welcomeAvatarInitial.Text = text.Substring(0, 1).ToUpperInvariant();
			}
			LoadUserAvatarPhoto();
		}
		catch { }
		if (_showReminderEntry)
		{
			ShowReminderCenter();
		}
		else
		{
			ShowDashboardOverview();
		}
		ApplyRoleDashboard();
	}

	private void LoadUserAvatarPhoto()
	{
		// Avatar loading removed - photo column not present in user_account.
	}

	private void ApplyRoleDashboard()
	{
		bool flag = string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);
		bool num = string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase) || flag;
		bool flag2 = num || Permissions.CanManageAnnouncements;
		bool flag3 = num || Permissions.CanManageProjects;
		bool flag4 = flag2 || flag3;
		if (!num && !Permissions.CanCreateBlotter)
		{
			kpiBlotter.Visibility = Visibility.Collapsed;
		}
		if (!num)
		{
			quickTileBlotter.Visibility = ((!Permissions.CanCreateBlotter) ? Visibility.Collapsed : Visibility.Visible);
			quickTileReports.Visibility = ((!Permissions.CanViewHotspotReports) ? Visibility.Collapsed : Visibility.Visible);
			int num2 = 2;
			if (quickTileBlotter.Visibility == Visibility.Visible)
			{
				num2++;
			}
			if (quickTileReports.Visibility == Visibility.Visible)
			{
				num2++;
			}
			quickLaunchGrid.Columns = num2;
			governanceToolsPanel.Visibility = Visibility.Collapsed;
		}
		if (!num && !flag3)
		{
			projectsPanel.Visibility = Visibility.Collapsed;
		}
		btnAnnouncementNew.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
		btnProjectNew.Visibility = ((!flag3) ? Visibility.Collapsed : Visibility.Visible);
		btnAnnouncementRegistry.Visibility = ((!flag4) ? Visibility.Collapsed : Visibility.Visible);
		btnProjectRegistry.Visibility = ((!flag4) ? Visibility.Collapsed : Visibility.Visible);
	}

	private async Task LoadDashboardAsync()
	{
		if (_showReminderEntry)
		{
			await LoadReminderCenterAsync();
		}
		else
		{
			await LoadDashboardOverviewAsync();
		}
	}

	private async Task LoadDashboardOverviewAsync()
	{
		_ = 1;
		try
		{
			UpdateStatCards(await FetchStats());
			await LoadFeaturePanelsAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: failed to load stats.", ex);
		}
	}

	private async Task LoadReminderCenterAsync()
	{
		reminderStatusNote.Text = "Loading reminders...";
		importantReminderCards.Children.Clear();
		planReminderCards.Children.Clear();
		try
		{
			ApplyReminderSnapshot(await _dashboardReminderService.LoadSnapshotAsync());
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: reminder center load failed.", ex);
			urgentReminderCountText.Text = "0";
			importantReminderCountText.Text = "0";
			planReminderCountText.Text = "0";
			reminderStatusNote.Text = "Reminders unavailable.";
			importantReminderCards.Children.Clear();
			planReminderCards.Children.Clear();
			importantReminderCards.Children.Add(BuildReminderEmptyRow("Unable to load important notifications right now."));
			planReminderCards.Children.Add(BuildReminderEmptyRow("Unable to load planned work right now."));
		}
	}

	private async Task<(int residents, int active, int households, int certs, int blotter, int meetings, int bookings, int shifts)> FetchStats()
	{
		var residentsTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM resident WHERE is_deleted=0 OR is_deleted IS NULL");
		var activeTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM resident WHERE status='ACTIVE' AND (is_deleted=0 OR is_deleted IS NULL)");
		var householdsTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM household");
		var certsTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM document_request WHERE status='SUBMITTED'");
		var blotterTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM case_record WHERE UPPER(status) IN ('OPEN','ONGOING')");
		var meetingsTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM barangay_meeting WHERE UPPER(status)='SCHEDULED'");
		var bookingsTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM facility_booking WHERE UPPER(status)='PENDING'");
		var shiftsTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tanod_shift WHERE DATE(shift_date) = DATE('now')");
		await Task.WhenAll(residentsTask, activeTask, householdsTask, certsTask, blotterTask,
			meetingsTask, bookingsTask, shiftsTask);
		return (residentsTask.Result, activeTask.Result, householdsTask.Result, certsTask.Result,
			blotterTask.Result, meetingsTask.Result, bookingsTask.Result, shiftsTask.Result);
	}

	private void UpdateStatCards((int residents, int active, int households, int certs, int blotter, int meetings, int bookings, int shifts) stats)
	{
		statResidentsValue.Text = stats.residents.ToString("N0");
		statActiveValue.Text = stats.active.ToString("N0");
		statHouseholdsValue.Text = stats.households.ToString("N0");
		statCertsValue.Text = stats.certs.ToString("N0");
		statBlotterValue.Text = stats.blotter.ToString("N0");

		// Active out of total residents (real ratio).
		double activePct = stats.residents > 0 ? (double)stats.active / stats.residents : 0d;
		// Residents fill toward a soft target so the ring reads as activity, not a hard cap.
		double residentsPct = ComputeProgressFraction(stats.residents, GetSoftTarget(stats.residents, 100));
		double householdsPct = ComputeProgressFraction(stats.households, GetSoftTarget(stats.households, 50));
		// Pending / Blotters are inverse: more = fuller (caps at 20).
		double certsPct = ComputeProgressFraction(stats.certs, 20);
		double blotterPct = ComputeProgressFraction(stats.blotter, 10);

		ApplyRingProgress(ringResidentsArc, ringResidentsPct, residentsPct);
		ApplyRingProgress(ringActiveArc, ringActivePct, activePct);
		ApplyRingProgress(ringHouseholdsArc, ringHouseholdsPct, householdsPct);
		ApplyRingProgress(ringCertsArc, ringCertsPct, certsPct);
		ApplyRingProgress(ringBlotterArc, ringBlotterPct, blotterPct);
	}

	private static double GetSoftTarget(int current, int baseline)
	{
		// Round up to the next "nice" multiple of baseline so a freshly-loaded
		// system with 30 residents shows a healthy 30% ring rather than 100%.
		if (current <= 0) return baseline;
		int multiplier = (current / baseline) + 1;
		return baseline * multiplier;
	}

	private static double ComputeProgressFraction(double value, double target)
	{
		if (target <= 0) return 0d;
		double pct = value / target;
		if (pct < 0d) pct = 0d;
		if (pct > 1d) pct = 1d;
		return pct;
	}

	private static void ApplyRingProgress(System.Windows.Shapes.Path arc, TextBlock pctText, double fraction)
	{
		if (arc == null || pctText == null) return;
		// Ring sits in a 42x42 host; padded 3px so the stroke doesn't clip.
		const double size = 42d;
		const double radius = (size - 6d) / 2d; // 18
		double centerX = size / 2d;
		double centerY = size / 2d;

		// Render percent label first.
		pctText.Text = $"{(int)System.Math.Round(fraction * 100):0}%";

		if (fraction <= 0.001)
		{
			arc.Data = null;
			return;
		}

		bool isFullCircle = fraction >= 0.999;
		double angle = (isFullCircle ? 359.999 : fraction * 360d) - 90d; // start at 12 o'clock
		double startAngle = -90d;
		double endRad = angle * System.Math.PI / 180d;
		double startRad = startAngle * System.Math.PI / 180d;

		var startPoint = new System.Windows.Point(
			centerX + radius * System.Math.Cos(startRad),
			centerY + radius * System.Math.Sin(startRad));
		var endPoint = new System.Windows.Point(
			centerX + radius * System.Math.Cos(endRad),
			centerY + radius * System.Math.Sin(endRad));

		bool isLargeArc = fraction > 0.5;
		var figure = new System.Windows.Media.PathFigure
		{
			StartPoint = startPoint,
			IsClosed = false
		};
		figure.Segments.Add(new System.Windows.Media.ArcSegment(
			endPoint,
			new System.Windows.Size(radius, radius),
			0d,
			isLargeArc,
			System.Windows.Media.SweepDirection.Clockwise,
			true));
		var geometry = new System.Windows.Media.PathGeometry();
		geometry.Figures.Add(figure);
		arc.Data = geometry;
	}

	private async Task LoadFeaturePanelsAsync()
	{
		try
		{
			Task<IReadOnlyList<AnnouncementRecord>> announcementTask = _announcementService.GetRecentAnnouncementsAsync();
			Task<IReadOnlyList<ProjectRecord>> projectTask = _projectService.GetRecentProjectsAsync();
			await Task.WhenAll(announcementTask, projectTask);
			RenderAnnouncementCards(announcementTask.Result);
			RenderProjectCards(projectTask.Result);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: feature panel load failed.", ex);
			announcementCards.Children.Clear();
			projectCards.Children.Clear();
			announcementCards.Children.Add(BuildEmptyState("Unable to load announcements right now."));
			projectCards.Children.Add(BuildEmptyState("Unable to load projects and programs right now."));
		}
	}

	private void RenderAnnouncementCards(IReadOnlyList<AnnouncementRecord> announcements)
	{
		announcementCards.Children.Clear();
		bool hasItems = announcements != null && announcements.Count > 0;
		if (announcementsEmptyState != null)
		{
			announcementsEmptyState.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
		}
		if (announcementsScroller != null)
		{
			announcementsScroller.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
		}
		if (!hasItems)
		{
			return;
		}
		foreach (AnnouncementRecord announcement in announcements)
		{
			announcementCards.Children.Add(BuildAnnouncementCard(announcement));
		}
	}

	private void RenderProjectCards(IReadOnlyList<ProjectRecord> projects)
	{
		projectCards.Children.Clear();
		bool hasItems = projects != null && projects.Count > 0;
		if (projectsEmptyState != null)
		{
			projectsEmptyState.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
		}
		if (projectsScroller != null)
		{
			projectsScroller.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
		}
		if (!hasItems)
		{
			return;
		}
		foreach (ProjectRecord project in projects)
		{
			projectCards.Children.Add(BuildProjectCard(project));
		}
	}

	private Border BuildAnnouncementCard(AnnouncementRecord announcement)
	{
		AnnouncementRecord announcement2 = announcement;
		Border border = CreateFeatureCardShell();
		StackPanel stackPanel = new StackPanel();
		Grid grid = new Grid
		{
			ColumnDefinitions = 
			{
				new ColumnDefinition(),
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			}
		};
		var titleText = new TextBlock
		{
			Text = announcement2.Title,
			FontWeight = FontWeights.Bold,
			FontSize = 13.0,
			TextWrapping = TextWrapping.Wrap
		};
		titleText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextPrimaryBrush");
		grid.Children.Add((UIElement)titleText);
		if (announcement2.IsPinned)
		{
			Border border2 = CreateChip("Pinned", "#DBEAFE", "#1D4ED8");
			Grid.SetColumn(border2, 1);
			border2.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
			grid.Children.Add(border2);
		}
		stackPanel.Children.Add(grid);
		WrapPanel wrapPanel = new WrapPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		wrapPanel.Children.Add(CreateAnnouncementPriorityChip(announcement2.Priority));
		wrapPanel.Children.Add(CreateAnnouncementStatusChip(announcement2.Status));
		stackPanel.Children.Add(wrapPanel);
		var dateText = new TextBlock
		{
			Text = announcement2.CreatedAtDisplay,
			FontSize = 11.0
		};
		dateText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextSecondaryBrush");
		stackPanel.Children.Add(dateText);
		if (CanManageAnnouncements())
		{
			stackPanel.Children.Add(CreateActionRow(async delegate
			{
				await OpenAnnouncementEditorAsync(announcement2);
			}, async delegate
			{
				await DeleteAnnouncementAsync(announcement2);
			}));
		}
		border.Child = stackPanel;
		return border;
	}

	private Border BuildProjectCard(ProjectRecord project)
	{
		ProjectRecord project2 = project;
		Border border = CreateFeatureCardShell();
		var projectTitle = new TextBlock
		{
			Text = project2.Name,
			FontWeight = FontWeights.Bold,
			FontSize = 13.0,
			TextWrapping = TextWrapping.Wrap
		};
		projectTitle.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextPrimaryBrush");
		StackPanel stackPanel = new StackPanel
		{
			Children = { (UIElement)projectTitle }
		};
		WrapPanel wrapPanel = new WrapPanel
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 8.0)
		};
		wrapPanel.Children.Add(CreateProjectTypeChip(project2.RecordType));
		wrapPanel.Children.Add(CreateProjectStatusChip(project2.Status));
		wrapPanel.Children.Add(CreateProjectOutcomeChip(project2.OutcomeStatus));
		stackPanel.Children.Add(wrapPanel);
		var scheduleText = new TextBlock
		{
			Text = BuildProjectScheduleLabel(project2),
			FontSize = 11.0
		};
		scheduleText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextSecondaryBrush");
		stackPanel.Children.Add(scheduleText);
		if (project2.LastActivityDate.HasValue)
		{
			var lastActiveText = new TextBlock
			{
				Text = "Last active " + project2.LastActivityDisplay,
				FontSize = 11.0,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			};
			lastActiveText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextSecondaryBrush");
			stackPanel.Children.Add(lastActiveText);
		}
		if (CanManageProjects())
		{
			stackPanel.Children.Add(CreateActionRow(async delegate
			{
				await OpenProjectEditorAsync(project2);
			}, async delegate
			{
				await DeleteProjectAsync(project2);
			}));
		}
		border.Child = stackPanel;
		return border;
	}

	private Border CreateFeatureCardShell()
	{
		var border = new Border
		{
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
			BorderThickness = new Thickness(1.0)
		};
		border.SetResourceReference(Border.BackgroundProperty, "ThemeCardHoverBrush");
		border.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");
		return border;
	}

	private Border CreateChip(string text, string backgroundHex, string foregroundHex)
	{
		return new Border
		{
			Background = (Brush)new BrushConverter().ConvertFromString(backgroundHex),
			CornerRadius = new CornerRadius(999.0),
			Padding = new Thickness(9.0, 3.0, 9.0, 3.0),
			Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
			Child = new TextBlock
			{
				Text = text,
				FontSize = 10.5,
				FontWeight = FontWeights.Bold,
				Foreground = (Brush)new BrushConverter().ConvertFromString(foregroundHex)
			}
		};
	}

	private Border CreateAnnouncementPriorityChip(string priority)
	{
		if (!(priority == "High"))
		{
			if (priority == "Low")
			{
				return CreateChip("Low Priority", "#ECFDF5", "#166534");
			}
			return CreateChip("Normal Priority", "#FEF3C7", "#B45309");
		}
		return CreateChip("High Priority", "#FEE2E2", "#B91C1C");
	}

	private Border CreateAnnouncementStatusChip(string status)
	{
		if (!(status == "Draft"))
		{
			if (status == "Archived")
			{
				return CreateChip("Archived", "#F3F4F6", "#4B5563");
			}
			return CreateChip("Published", "#DBEAFE", "#1D4ED8");
		}
		return CreateChip("Draft", "#E5E7EB", "#374151");
	}

	private Border CreateProjectStatusChip(string status)
	{
		return status switch
		{
			"Completed" => CreateChip("Completed", "#DCFCE7", "#166534"), 
			"Ongoing" => CreateChip("Ongoing", "#DBEAFE", "#1D4ED8"), 
			"On hold" => CreateChip("On Hold", "#FEE2E2", "#B91C1C"), 
			_ => CreateChip("Planned", "#FEF3C7", "#B45309"), 
		};
	}

	private Border CreateProjectTypeChip(string recordType)
	{
		if (!string.Equals(recordType, "Program", StringComparison.OrdinalIgnoreCase))
		{
			return CreateChip("Project", "#E0F2FE", "#0369A1");
		}
		return CreateChip("Program", "#CCFBF1", "#0F766E");
	}

	private Border CreateProjectOutcomeChip(string outcomeStatus)
	{
		return outcomeStatus switch
		{
			"Achieved" => CreateChip("Outcome Achieved", "#DCFCE7", "#166534"), 
			"Needs follow-up" => CreateChip("Needs Follow-up", "#FEE2E2", "#B91C1C"), 
			"In progress" => CreateChip("Outcome In Progress", "#DBEAFE", "#1D4ED8"), 
			_ => CreateChip("Outcome Pending", "#E5E7EB", "#374151"), 
		};
	}

	private UIElement CreateActionRow(Func<Task> editAction, Func<Task> deleteAction)
	{
		Func<Task> editAction2 = editAction;
		Func<Task> deleteAction2 = deleteAction;
		StackPanel obj = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		Button button = new Button
		{
			Content = "Edit",
			Style = (Style)Application.Current.Resources["GhostButtonStyle"],
			Height = 30.0,
			MinWidth = 68.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			Margin = new Thickness(0.0, 0.0, 4.0, 0.0)
		};
		button.Click += async delegate
		{
			await editAction2();
		};
		Button button2 = new Button
		{
			Content = "Delete",
			Style = (Style)Application.Current.Resources["GhostButtonStyle"],
			Height = 30.0,
			MinWidth = 72.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			Foreground = (Brush)new BrushConverter().ConvertFromString("#B91C1C")
		};
		button2.Click += async delegate
		{
			await deleteAction2();
		};
		obj.Children.Add(button);
		obj.Children.Add(button2);
		return obj;
	}

	private static UIElement BuildEmptyState(string message, bool isPlanColumn = false)
	{
		// Frosted card with calendar icon (or check icon for the urgent column),
		// matching the Dashboard Notifications mockup empty states.
		var card = new Border
		{
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(14.0),
			Padding = new Thickness(20.0, 28.0, 20.0, 28.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		card.SetResourceReference(Border.BackgroundProperty, "ThemeCardBrush");
		card.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");

		var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
		var iconHolder = new Grid
		{
			Width = 64,
			Height = 64,
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		var iconBubble = new Border
		{
			Width = 64,
			Height = 64,
			CornerRadius = new CornerRadius(12.0)
		};
		iconBubble.SetResourceReference(Border.BackgroundProperty, "ThemeCardHoverBrush");
		iconHolder.Children.Add(iconBubble);
		var icon = new FontAwesome.Sharp.IconBlock
		{
			Icon = isPlanColumn ? FontAwesome.Sharp.IconChar.CalendarTimes : FontAwesome.Sharp.IconChar.CheckCircle,
			FontSize = 32.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		if (isPlanColumn)
		{
			icon.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "ThemeTextSecondaryBrush");
		}
		else
		{
			icon.Foreground = (Brush)new BrushConverter().ConvertFromString("#22C55E");
		}
		iconHolder.Children.Add(icon);
		stack.Children.Add(iconHolder);
		var label = new TextBlock
		{
			Text = message,
			FontSize = 12.0,
			TextAlignment = TextAlignment.Center,
			TextWrapping = TextWrapping.Wrap,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		label.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextSecondaryBrush");
		stack.Children.Add(label);
		card.Child = stack;
		return card;
	}

	private static string BuildProjectScheduleLabel(ProjectRecord project)
	{
		if (project.StartDate.HasValue && project.EndDate.HasValue)
		{
			return $"{project.StartDate.Value:MMM dd, yyyy} - {project.EndDate.Value:MMM dd, yyyy}";
		}
		if (project.StartDate.HasValue)
		{
			return $"Starts {project.StartDate.Value:MMM dd, yyyy}";
		}
		if (project.EndDate.HasValue)
		{
			return $"Target end {project.EndDate.Value:MMM dd, yyyy}";
		}
		return "Created " + project.CreatedAtDisplay;
	}

	private void ApplyReminderSnapshot(DashboardReminderSnapshot snapshot)
	{
		urgentReminderCountText.Text = snapshot.UrgentCount.ToString("N0");
		importantReminderCountText.Text = snapshot.NotificationCount.ToString("N0");
		planReminderCountText.Text = snapshot.PlanCount.ToString("N0");
		bool flag = snapshot.NotificationCount > 0 || snapshot.PlanCount > 0;
		reminderStatusNote.Text = (flag ? $"{snapshot.NotificationCount:N0} important item(s) | {snapshot.PlanCount:N0} plan item(s)" : "No urgent reminders right now.");
		try
		{
			if (urgentSectionTotal != null)
			{
				urgentSectionTotal.Text = $"{snapshot.NotificationCount:N0} Total";
			}
			if (reminderTimestampText != null)
			{
				reminderTimestampText.Text = DateTime.Now.ToString("MMM d, yyyy • h:mm tt");
			}
			if (reminderFooterTopText != null)
			{
				reminderFooterTopText.Text = (snapshot.NotificationCount == 0 && snapshot.PlanCount == 0)
					? "You have reviewed all critical items and upcoming plans."
					: $"You have {snapshot.NotificationCount:N0} item(s) needing attention and {snapshot.PlanCount:N0} planned item(s).";
			}
		}
		catch { }
		RenderReminderCards(importantReminderCards, snapshot.Notifications, "No urgent items right now. You're all caught up.", isPlanColumn: false);
		RenderReminderCards(planReminderCards, snapshot.Plans, "No upcoming plans or schedules were found for this cycle.\nCheck back later.", isPlanColumn: true);
	}

	private void RenderReminderCards(Panel host, IReadOnlyList<DashboardReminderItem> items, string emptyMessage, bool isPlanColumn = false)
	{
		host.Children.Clear();
		if (items == null || items.Count == 0)
		{
			host.Children.Add(BuildReminderEmptyRow(emptyMessage));
			return;
		}
		foreach (DashboardReminderItem item in items)
		{
			host.Children.Add(BuildReminderCard(item));
		}
	}

	private Border BuildReminderCard(DashboardReminderItem item)
	{
		DashboardReminderItem current = item;
		string accentKey = current.Severity switch
		{
			DashboardReminderSeverity.Urgent => "ReminderUrgentAccentBrush",
			DashboardReminderSeverity.Attention => "ReminderAttentionAccentBrush",
			_ => "ReminderPlanAccentBrush"
		};

		Border row = new Border
		{
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0),
			Padding = new Thickness(8.0, 10.0, 8.0, 10.0)
		};
		row.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");

		Grid layout = new Grid();
		layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100.0) });
		layout.ColumnDefinitions.Add(new ColumnDefinition());
		layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135.0) });

		TextBlock severity = new TextBlock
		{
			Text = ResolveSeverityLabel(current.Severity).ToUpperInvariant(),
			FontSize = 9.5,
			FontWeight = FontWeights.Bold,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(2.0, 2.0, 8.0, 0.0)
		};
		severity.SetResourceReference(TextBlock.ForegroundProperty, accentKey);
		layout.Children.Add(severity);

		StackPanel details = new StackPanel { Margin = new Thickness(8.0, 0.0, 12.0, 0.0) };
		Grid.SetColumn(details, 1);
		TextBlock title = new TextBlock
		{
			Text = current.Title ?? string.Empty,
			FontSize = 12.0,
			FontWeight = FontWeights.SemiBold,
			TextWrapping = TextWrapping.Wrap
		};
		title.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextPrimaryBrush");
		details.Children.Add(title);
		if (!string.IsNullOrWhiteSpace(current.Description))
		{
			TextBlock description = new TextBlock
			{
				Text = current.Description,
				FontSize = 10.5,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
			};
			description.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextSecondaryBrush");
			details.Children.Add(description);
		}
		if (!string.IsNullOrWhiteSpace(current.Footnote))
		{
			TextBlock footnote = new TextBlock
			{
				Text = current.Footnote,
				FontSize = 10.0,
				FontStyle = FontStyles.Italic,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
			};
			footnote.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextMutedBrush");
			details.Children.Add(footnote);
		}
		layout.Children.Add(details);

		if (!string.IsNullOrWhiteSpace(current.Route) && CanOpenReminderRoute(current.Route))
		{
			Button action = new Button
			{
				Content = string.IsNullOrWhiteSpace(current.ActionLabel) ? "Open" : current.ActionLabel,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(1.0),
				FontSize = 10.5,
				FontWeight = FontWeights.SemiBold,
				Padding = new Thickness(10.0, 5.0, 10.0, 5.0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Cursor = System.Windows.Input.Cursors.Hand,
				Template = BuildClassicActionButtonTemplate()
			};
			action.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, accentKey);
			action.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, accentKey);
			action.Click += (s, e) => NavigateReminderRoute(current.Route);
			Grid.SetColumn(action, 2);
			layout.Children.Add(action);
		}

		row.Child = layout;
		return row;
	}

	private static UIElement BuildReminderEmptyRow(string message)
	{
		Border row = new Border
		{
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0),
			Padding = new Thickness(10.0, 18.0, 10.0, 18.0)
		};
		row.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");
		TextBlock label = new TextBlock
		{
			Text = message,
			FontSize = 10.5,
			FontStyle = FontStyles.Italic,
			TextWrapping = TextWrapping.Wrap
		};
		label.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextSecondaryBrush");
		row.Child = label;
		return row;
	}

	private static System.Windows.Controls.ControlTemplate BuildClassicActionButtonTemplate()
	{
		var template = new System.Windows.Controls.ControlTemplate(typeof(Button));
		var border = new System.Windows.FrameworkElementFactory(typeof(Border));
		border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
		border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
		border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
		border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
		var presenter = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
		presenter.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		presenter.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		border.AppendChild(presenter);
		template.VisualTree = border;
		return template;
	}

	private static (string accentHex, string bgHex, string borderHex, string iconBgHex) ResolveReminderCardPalette_LegacyUnused(DashboardReminderSeverity severity)
	{
		return severity switch
		{
			DashboardReminderSeverity.Urgent => (
				accentHex: "#DC2626",
				bgHex: "#22EF4444",
				borderHex: "#88EF4444",
				iconBgHex: "#33EF4444"),
			DashboardReminderSeverity.Attention => (
				accentHex: "#D97706",
				bgHex: "#22F59E0B",
				borderHex: "#88F59E0B",
				iconBgHex: "#33F59E0B"),
			_ => (
				accentHex: "#10B981",
				bgHex: "#2210B981",
				borderHex: "#8810B981",
				iconBgHex: "#3310B981"),
		};
	}

	private static string ResolveSeverityLabel(DashboardReminderSeverity severity)
	{
		return severity switch
		{
			DashboardReminderSeverity.Urgent => "Urgent", 
			DashboardReminderSeverity.Attention => "Needs Attention", 
			_ => "Planned", 
		};
	}

	private bool CanOpenReminderRoute(string route)
	{
		bool flag = string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase) || flag;
		return route switch
		{
			"Clearances" => flag2 || Permissions.CanRequestCertificates || Permissions.CanEditCertificateRequests || Permissions.CanApproveCertificates || Permissions.CanIssueCertificates || Permissions.CanCancelCertificates || Permissions.CanExportCertificates, 
			"ResidentCases" => flag2 || Permissions.CanCreateBlotter || Permissions.CanUpdateBlotterStatus, 
			"GovernanceRegistry" => flag2 || Permissions.CanManageAnnouncements || Permissions.CanManageProjects, 
			"NotificationOutbox" => flag2, 
			_ => true, 
		};
	}

	private void NavigateReminderRoute(string route)
	{
		if (Application.Current.MainWindow is MainWindow mainWindow)
		{
			mainWindow.NavigatePage(route);
		}
	}

	private void ShowReminderCenter()
	{
		reminderCenterSection.Visibility = Visibility.Visible;
		dashboardOverviewSection.Visibility = Visibility.Collapsed;
	}

	private void ShowDashboardOverview()
	{
		reminderCenterSection.Visibility = Visibility.Collapsed;
		dashboardOverviewSection.Visibility = Visibility.Visible;
	}

	private void Calendar_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
	{
		DateTime? selectedDate = actionCalendar.SelectedDate;
		if (selectedDate.HasValue)
		{
			DateTime valueOrDefault = selectedDate.GetValueOrDefault();
			calendarInfoText.Text = $"Selected: {valueOrDefault:ddd, MMM dd, yyyy}\nNo urgent items scheduled.";
			calendarInfoText.Visibility = Visibility.Visible;
		}
	}

	private async void BtnRefreshReminderCenter_Click(object sender, RoutedEventArgs e)
	{
		await LoadReminderCenterAsync();
	}

	private void BtnOpenDashboardOverview_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("Dashboard");
	}

	private void BtnOpenReminderCenter_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("DashboardNotifications");
	}

	private void WelcomeAvatar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		try
		{
			var profile = new Models.StaffProfileDetails { UserId = UserSession.UserId };
			var staffWindow = new Dialogs.StaffDetailsWindow(profile);
			FullscreenDialogNavigator.Open(
				staffWindow,
				"Edit My Profile",
				"Update your account and staff profile information.",
				"Dashboard",
				IconChar.UserEdit,
				"Save Changes",
				LoadUserAvatarPhoto);
		}
		catch (Exception ex)
		{
			helper.AppLogger.LogWarning("Failed to open profile editor from dashboard avatar.", ex);
		}
	}

	private void QuickAddResident_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("ResidentWorkspace");
	}

	private void QuickAddCertificate_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("Clearances");
	}

	private void QuickAddBlotter_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("ResidentCases");
	}

	private void QuickOpenReports_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("Reports");
	}

	private async void BtnAnnouncementNew_Click(object sender, RoutedEventArgs e)
	{
		await OpenAnnouncementEditorAsync(null);
	}

	private async void BtnAnnouncementRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadFeaturePanelsAsync();
	}

	private async void BtnProjectNew_Click(object sender, RoutedEventArgs e)
	{
		await OpenProjectEditorAsync(null);
	}

	private async void BtnProjectRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadFeaturePanelsAsync();
	}

	private void BtnOpenGovernanceRegistry_Click(object sender, RoutedEventArgs e)
	{
		if (!CanOpenGovernanceRegistry())
		{
			DialogService.Instance.ShowWarning("You do not have permission to open the announcements and projects registry.");
		}
		else
		{
			(Application.Current.MainWindow as MainWindow)?.NavigatePage("GovernanceRegistry");
		}
	}

	private bool CanManageAnnouncements()
	{
		if (!Permissions.IsAdmin)
		{
			return Permissions.CanManageAnnouncements;
		}
		return true;
	}

	private bool CanManageProjects()
	{
		if (!Permissions.IsAdmin)
		{
			return Permissions.CanManageProjects;
		}
		return true;
	}

	private bool CanOpenGovernanceRegistry()
	{
		if (!CanManageAnnouncements())
		{
			return CanManageProjects();
		}
		return true;
	}

	private async Task OpenAnnouncementEditorAsync(AnnouncementRecord? announcement)
	{
		if (!CanManageAnnouncements())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage announcements.");
			return;
		}
		AnnouncementWindow window = ((announcement == null) ? new AnnouncementWindow() : new AnnouncementWindow(announcement));
		FullscreenDialogNavigator.Open(
			window,
			announcement == null ? "New Announcement" : "Edit Announcement",
			announcement?.Title ?? "Publish a clear message for residents and staff.",
			"Dashboard",
			IconChar.Bullhorn,
			announcement == null ? "Publish Announcement" : "Save Changes",
			() => _ = LoadFeaturePanelsAsync());
		await Task.CompletedTask;
	}

	private async Task OpenProjectEditorAsync(ProjectRecord? project)
	{
		if (!CanManageProjects())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage projects.");
			return;
		}
		ProjectWindow window = ((project == null) ? new ProjectWindow() : new ProjectWindow(project));
		FullscreenDialogNavigator.Open(
			window,
			project == null ? "New Project or Program" : "Edit Project or Program",
			project?.Name ?? "Track one community initiative from planning through outcome.",
			"Dashboard",
			IconChar.ProjectDiagram,
			project == null ? "Save Record" : "Save Changes",
			() => _ = LoadFeaturePanelsAsync());
		await Task.CompletedTask;
	}

	private async Task DeleteAnnouncementAsync(AnnouncementRecord announcement)
	{
		if (!CanManageAnnouncements())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage announcements.");
		}
		else
		{
			string? reason = DialogService.Instance.PromptForReason(
				"Archive Announcement",
				"Why should this announcement be removed from the active dashboard feed?",
				"Archive");
			if (reason == null || !DialogService.Instance.Confirm(
					"Archive announcement \"" + announcement.Title + "\"? The record will be retained.",
					"Archive Announcement"))
			{
				return;
			}
			try
			{
				await _announcementService.ArchiveAnnouncementAsync(announcement.AnnouncementId, reason);
				await LoadFeaturePanelsAsync();
			}
			catch (Exception ex)
			{
				AppLogger.LogError("DashboardPage: failed to archive announcement.", ex);
				DialogService.Instance.ShowError(ex.Message, "Archive Announcement");
			}
		}
	}

	private async Task DeleteProjectAsync(ProjectRecord project)
	{
		if (!CanManageProjects())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage projects.");
			return;
		}
		string value = (string.Equals(project.RecordType, "Program", StringComparison.OrdinalIgnoreCase) ? "program" : "project");
		string? reason = DialogService.Instance.PromptForReason(
			"Archive Initiative",
			$"Why should this {value} be archived?",
			"Archive");
		if (reason == null || !DialogService.Instance.Confirm(
				$"Archive {value} \"{project.Name}\"? The record will be retained.",
				"Archive Initiative"))
		{
			return;
		}
		try
		{
			await _projectService.ArchiveProjectAsync(project.ProjectId, reason);
			await LoadFeaturePanelsAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: failed to archive project.", ex);
			DialogService.Instance.ShowError(ex.Message, "Archive Initiative");
		}
	}}
