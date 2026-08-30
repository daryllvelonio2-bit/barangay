using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.Views.Pages;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views;

public partial class MainWindow : Window
{
	private const double ExpandedSidebarWidth = 200.0;

	private const double CollapsedSidebarWidth = 52.0;

	private readonly DispatcherTimer _clockTimer = new DispatcherTimer();

	private readonly DispatcherTimer _notificationTimer = new DispatcherTimer();

	private bool _isSidebarCollapsed;

	private bool _isSynchronizingNavigation;

	private string _currentRoute = string.Empty;






















































	public MainWindow()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		InitializeComponent();

		// Register toast notification service so ToastService.Error/Success/etc. work app-wide
		baranggaysystem1.Views.Controls.ToastService.Register(toastNotification);

		// Initialize theme system
		Services.ThemeService.Instance.Initialize();
		if (Services.ThemeService.Instance.IsDarkMode)
		{
			themeToggleIcon.Icon = FontAwesome.Sharp.IconChar.Sun;
		}

		NavigationService.Instance.Initialize(pageHost);
		string text = UserSession.Username ?? "User";
		string text2 = UserSession.Role ?? "Staff";
		string text3 = ((text.Length > 0) ? text[0].ToString().ToUpperInvariant() : "U");
		sidebarUsername.Text = text;
		sidebarRole.Text = text2;
		sidebarUserInitial.Text = text3;
		topBarInitial.Text = text3;
		RefreshBranding();
		ApplyRoleVisibility();
		_clockTimer.Interval = TimeSpan.FromSeconds(30.0);
		_clockTimer.Tick += delegate
		{
			UpdateClock();
		};
		_clockTimer.Start();
		UpdateClock();
		base.Loaded += delegate
		{
			base.Opacity = 0.0;

			// Ensure the window covers the entire screen including taskbar
			var screen = System.Windows.SystemParameters.PrimaryScreenWidth;
			var screenH = System.Windows.SystemParameters.PrimaryScreenHeight;
			base.Left = 0;
			base.Top = 0;
			base.Width = screen;
			base.Height = screenH;

			BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(150.0)));
			NavigatePage("Home");

			// Initialize UX enhancements (keyboard shortcuts, command palette, toast, nav history)
			UxEnhancementsIntegration.Initialize(this);

			// Initialize session security (timeout, lock, forced password change)
			SessionSecurityIntegration.OnLoginSuccess();

			// Keep one owned timer per shell and stop it when the shell closes.
			_notificationTimer.Interval = TimeSpan.FromMinutes(5);
			_notificationTimer.Tick += NotificationTimer_Tick;
			_notificationTimer.Start();

			// Show theme picker on first login
			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
			{
				try { Views.Dialogs.ThemePickerWindow.ShowIfNeeded(this); } catch { }
			}));
		};
		Closed += delegate
		{
			_clockTimer.Stop();
			_notificationTimer.Stop();
			_notificationTimer.Tick -= NotificationTimer_Tick;
			UxEnhancementsIntegration.Shutdown();
		};
	}

	private void ApplyRoleVisibility()
	{
		SetNav(navGovernanceRegistry, RouteAuthorization.CanNavigate("GovernanceRegistry"));
		SetNav(navHouseholds, RouteAuthorization.CanNavigate("Households"));
		SetNav(navBlotter, RouteAuthorization.CanNavigate("ResidentCases"));
		SetNav(navTanod, RouteAuthorization.CanNavigate("TanodPatrol"));
		SetNav(navEmergencyContacts, RouteAuthorization.CanNavigate("EmergencyContacts"));
		SetNav(navPayments, RouteAuthorization.CanNavigate("ResidentPayments"));
		SetNav(navAyuda, RouteAuthorization.CanNavigate("Ayuda"));
		SetNav(navCollections, RouteAuthorization.CanNavigate("Collections"));
		SetNav(navGroupFinance, navPayments.Visibility == Visibility.Visible || navAyuda.Visibility == Visibility.Visible || navCollections.Visibility == Visibility.Visible);
		SetNav(navMeetings, RouteAuthorization.CanNavigate("Meetings"));
		SetNav(navFacilityBooking, RouteAuthorization.CanNavigate("FacilityBooking"));
		SetNav(navOfficials, RouteAuthorization.CanNavigate("Officials"));
		SetNav(navStaff, RouteAuthorization.CanNavigate("StaffUsers"));
		SetNav(navRoles, RouteAuthorization.CanNavigate("RolePermissions"));
		SetNav(navLogs, RouteAuthorization.CanNavigate("SystemLogs"));
		SetNav(navNotificationOutbox, RouteAuthorization.CanNavigate("NotificationOutbox"));
		SetNav(navSettings, RouteAuthorization.CanNavigate("Settings"));
		SetNav(navGroupBlotter, navBlotter.Visibility == Visibility.Visible || navTanod.Visibility == Visibility.Visible);
		SetNav(navGroupAdmin, navOfficials.Visibility == Visibility.Visible || navStaff.Visibility == Visibility.Visible || navRoles.Visibility == Visibility.Visible || navLogs.Visibility == Visibility.Visible || navNotificationOutbox.Visibility == Visibility.Visible || navSettings.Visibility == Visibility.Visible || navMeetings.Visibility == Visibility.Visible || navFacilityBooking.Visibility == Visibility.Visible);
	}

	private static void SetNav(UIElement element, bool visible)
	{
		element.Visibility = ((!visible) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateClock()
	{
		statusClock.Text = DateTime.Now.ToString("ddd, MMM dd yyyy");
		UpdateSyncStatus();
	}

	private void UpdateSyncStatus()
	{
		try
		{
			bool isOffline = Database.OfflineDatabaseSupport.IsOffline;
			int pending = Database.OfflineSyncService.GetPendingCount();

			if (isOffline)
			{
				dbStatusDot.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
				dbStatusLabel.Text = "Offline (SQLite)";
			}
			else
			{
				dbStatusDot.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
				dbStatusLabel.Text = "Connected";
			}

			if (pending > 0)
			{
				syncStatusLabel.Text = $"{pending} pending sync";
				btnSyncNow.Visibility = Visibility.Visible;
			}
			else
			{
				syncStatusLabel.Text = "";
				btnSyncNow.Visibility = Visibility.Collapsed;
			}
		}
		catch { }
	}

	private async void BtnSyncNow_Click(object sender, RoutedEventArgs e)
	{
		btnSyncNow.IsEnabled = false;
		syncStatusLabel.Text = "Syncing...";
		try
		{
			Database.SyncReplayResult result = await Task.Run(
				() => Database.OfflineSyncService.TrySyncPendingChangesDetailed());
			if (result.FailedCount > 0)
				syncStatusLabel.Text = $"⚠ {result.FailedCount} failed, {result.RemainingCount} pending";
			else if (result.SyncedCount > 0)
				syncStatusLabel.Text = $"✓ {result.SyncedCount} synced";
			else if (result.RemainingCount > 0)
				syncStatusLabel.Text = $"{result.RemainingCount} pending";
			else
				syncStatusLabel.Text = "✓ Up to date";
		}
		catch (Exception ex)
		{
			syncStatusLabel.Text = "Sync error";
			AppLogger.LogWarning("Manual sync failed.", ex);
		}
		finally
		{
			btnSyncNow.IsEnabled = true;
		}
	}

	public void RefreshBranding()
	{
		string systemName = SystemConfigService.GetSystemName();
		string barangayName = SystemConfigService.GetBarangayName();
		base.Title = systemName;
		barangayLabel.Text = barangayName;
		sidebarBarangayName.Text = FormatSidebarBrandName(barangayName);
		BitmapImage logo = SystemConfigService.GetLogo();
		if (logo != null)
		{
			sidebarBrandLogo.Source = logo;
			sidebarBrandLogo.Visibility = Visibility.Visible;
			((UIElement)(object)sidebarBrandIcon).Visibility = Visibility.Collapsed;
		}
		else
		{
			sidebarBrandLogo.Source = null;
			sidebarBrandLogo.Visibility = Visibility.Collapsed;
			((UIElement)(object)sidebarBrandIcon).Visibility = Visibility.Visible;
		}
	}

	private void NavItem_Checked(object sender, RoutedEventArgs e)
	{
		if (!_isSynchronizingNavigation && sender is RadioButton radioButton)
		{
			string route = (radioButton.Tag as string) ?? "Home";
			NavigatePage(route);
		}
	}

	public void NavigatePage(string route, bool recordHistory = true, bool forceReload = false)
	{
		route = NormalizeRoute(route);
		if (!RouteAuthorization.CanNavigate(route))
		{
			DialogService.Instance.ShowWarning(
				"Your account is not allowed to open this area. Ask an administrator if you need access.",
				"Access denied");
			SyncNavigationSelection(_currentRoute);
			return;
		}

		// Guard: Check for unsaved changes in the current fullscreen view
		// before allowing navigation away (Requirements 3.1, 3.2)
		var nav = NavigationService.Instance;
		if (!nav.GuardUnsavedChanges())
		{
			// User chose to keep editing — block navigation and restore sidebar selection
			SyncNavigationSelection(_currentRoute);
			return;
		}

		if (forceReload)
		{
			nav.InvalidateCache(route);
		}
		else if (string.Equals(_currentRoute, route, StringComparison.Ordinal) && pageHost.Content != null)
		{
			UpdateShellForRoute(route);
			UpdateBreadcrumb(route);
			SyncNavigationSelection(route);
			return;
		}
		UIElement uIElement = route switch
		{
			"Home" => nav.GetOrCreate(route, () => new HomeLandingPage()),
			"DashboardNotifications" => new DashboardPage(showReminderEntry: true),
			"Dashboard" => nav.GetOrCreate(route, () => new DashboardPage()),
			"Statistics" => nav.GetOrCreate(route, () => new StatisticsPage()),
			"GovernanceRegistry" => nav.GetOrCreate(route, () => new GovernanceRegistryPage()),
			"ResidentWorkspace" => nav.GetOrCreate(route, () => new ResidentModulePage()),
			"ResidentSoloParents" => nav.GetOrCreate(route, () => new ResidentModulePage(route)),
			"ResidentYouth" => nav.GetOrCreate(route, () => new ResidentModulePage(route)),
			"ResidentIndigent" => nav.GetOrCreate(route, () => new ResidentModulePage(route)),
			"Households" => nav.GetOrCreate(route, () => new HouseholdsPage()),
			"ResidentCategories" => nav.GetOrCreate(route, () => new TagsCategoriesPage()),
			"DeceasedRegistry" => nav.GetOrCreate(route, () => new DeceasedRegistryPage()),
			"Clearances" => nav.GetOrCreate(route, () => new ClearancesPage()),
			"Permits" => nav.GetOrCreate(route, () => new PermitsPage()),
			"ResidentCases" => nav.GetOrCreate(route, () => new BlotterPage()),
			"TanodPatrol" => nav.GetOrCreate(route, () => new TanodPatrolPage()),
			"EmergencyContacts" => nav.GetOrCreate(route, () => new EmergencyContactsPage()),
			"Meetings" => nav.GetOrCreate(route, () => new MeetingsPage()),
			"FacilityBooking" => nav.GetOrCreate(route, () => new FacilityBookingPage()),
			"ResidentPayments" => nav.GetOrCreate(route, () => new PaymentsPage()),
			"Ayuda" => nav.GetOrCreate(route, () => new AyudaPage()),
			"Collections" => nav.GetOrCreate(route, () => new CollectionsPage()),
			"Reports" => nav.GetOrCreate(route, () => new ReportsPage()),
			"Officials" => nav.GetOrCreate(route, () => new OfficialsPage()),
			"StaffUsers" => nav.GetOrCreate(route, () => new StaffManagementPage()),
			"RolePermissions" => nav.GetOrCreate(route, () => new RolePermissionsPage()),
			"SystemLogs" => nav.GetOrCreate(route, () => new SystemLogsPage()),
			"NotificationOutbox" => nav.GetOrCreate(route, () => new NotificationOutboxPage()),
			"Settings" => nav.GetOrCreate(route, () => new SettingsPage()),
			_ => nav.GetOrCreate("Home", () => new HomeLandingPage()),
		};
		_currentRoute = route;
		UpdateShellForRoute(route);
		UpdateBreadcrumb(route);
		SyncNavigationSelection(route);

		// Record navigation for history (back/forward support)
		if (recordHistory)
		{
			UxEnhancementsIntegration.RecordNavigation(route, RouteToTitle(route));
		}
		if (uIElement is FrameworkElement frameworkElement)
		{
			frameworkElement.Opacity = 0.0;
			frameworkElement.RenderTransform = new TranslateTransform(0.0, 14.0);
		}

		// Use NavigationService.NavigateTo to enforce single active view constraint
		// (Requirement 11.1, 11.2, 11.3): ensures exactly one view in pageHost,
		// removes previous view, and completes/cancels in-progress transitions.
		nav.NavigateTo(uIElement);

		if (uIElement is FrameworkElement frameworkElement2)
		{
			// Signal transition start for debounce coordination
			nav.BeginTransition();

			DoubleAnimation animation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(120.0));
			DoubleAnimation animation2 = new DoubleAnimation(14.0, 0.0, TimeSpan.FromMilliseconds(120.0))
			{
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};

			// Signal transition end when animation completes
			animation.Completed += (s, args) => nav.EndTransition();

			frameworkElement2.BeginAnimation(UIElement.OpacityProperty, animation);
			((TranslateTransform)frameworkElement2.RenderTransform).BeginAnimation(TranslateTransform.YProperty, animation2);
		}
	}

	private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
	{
		SetSidebarState(!_isSidebarCollapsed);
	}

	private void SetSidebarState(bool collapse)
	{
		_isSidebarCollapsed = false; // Always keep sidebar expanded with labels visible
		if (!string.Equals(_currentRoute, "Home", StringComparison.OrdinalIgnoreCase))
		{
			SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
			SidebarColumn.Width = new GridLength(180.0);
		}
	}

	private void BtnSearch_Click(object sender, RoutedEventArgs e)
	{
		new GlobalSearchWindow().ShowDialog();
	}

	private void TopBarSearchBox_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		new GlobalSearchWindow().ShowDialog();
	}

	private void BtnEllie_Click(object sender, RoutedEventArgs e)
	{
		new EllieAssistantWindow().ShowDialog();
	}

	private void BtnNotify_Click(object sender, RoutedEventArgs e)
	{
		notificationPopup.IsOpen = !notificationPopup.IsOpen;
	}

	private void BtnLock_Click(object sender, RoutedEventArgs e)
	{
		SessionSecurityIntegration.LockSession();
	}

	private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
	{
		Services.ThemeService.Instance.ToggleTheme();
		themeToggleIcon.Icon = Services.ThemeService.Instance.IsDarkMode
			? FontAwesome.Sharp.IconChar.Sun
			: FontAwesome.Sharp.IconChar.Moon;
	}

	private void BtnNotifyViewAll_Click(object sender, RoutedEventArgs e)
	{
		notificationPopup.IsOpen = false;
		NavigatePage(Permissions.CanDispatchNotifications ? "NotificationOutbox" : "DashboardNotifications");
	}

	private static void NotificationTimer_Tick(object? sender, EventArgs e)
	{
		Task.Run(() => OutboundNotificationService.TryRunScheduledAutomation(includeReminderQueue: true));
	}

	private static string NormalizeRoute(string? route)
	{
		if (!string.IsNullOrWhiteSpace(route))
		{
			return route;
		}
		return "Home";
	}

	private static string FormatSidebarBrandName(string barangayName)
	{
		if (string.IsNullOrWhiteSpace(barangayName))
		{
			return "BARANGAY";
		}
		string text = barangayName.Trim();
		if (text.StartsWith("barangay ", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = text;
			int length = "barangay ".Length;
			text = text2.Substring(length, text2.Length - length);
		}
		return text.ToUpperInvariant();
	}

	private void UpdateShellForRoute(string route)
	{
		bool isHome = string.Equals(route, "Home", StringComparison.OrdinalIgnoreCase);
		bool isDashboard = string.Equals(route, "DashboardNotifications", StringComparison.OrdinalIgnoreCase);
		bool hideSidebar = isHome || isDashboard;
		SidebarRoot.Visibility = (hideSidebar ? Visibility.Collapsed : Visibility.Visible);
		btnToggleSidebar.Visibility = (hideSidebar ? Visibility.Collapsed : Visibility.Visible);
		TopBarContainer.Visibility = Visibility.Visible;
		BottomStatusBar.Visibility = Visibility.Visible;
		SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
		SidebarColumn.Width = new GridLength(hideSidebar ? 0.0 : 180.0);
		TopBarRow.Height = new GridLength(36.0);
		StatusBarRow.Height = new GridLength(22.0);
	}

	private void UpdateBreadcrumb(string route)
	{
		bool flag = string.Equals(route, "Home", StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals(route, "DashboardNotifications", StringComparison.OrdinalIgnoreCase);
		breadcrumbLabel.Text = RouteToTitle(route);
		breadcrumbLabel.ToolTip = null;
		breadcrumbRootLabel.Text = "Home";
		breadcrumbRootLabel.Visibility = ((flag || flag2) ? Visibility.Collapsed : Visibility.Visible);
		breadcrumbSeparator.Visibility = ((flag || flag2) ? Visibility.Collapsed : Visibility.Visible);
	}

	/// <summary>
	/// Updates the breadcrumb display for fullscreen view navigation.
	/// Sets breadcrumb to "OriginTitle › ViewTitle" format.
	/// Truncates ViewTitle with ellipsis if exceeding 50 characters and sets ToolTip with full title.
	/// Limits breadcrumb depth to two segments for nested fullscreen views.
	/// Requirements: 4.1, 4.2, 4.3, 4.4, 4.5
	/// </summary>
	/// <param name="originRoute">The route key of the originating page.</param>
	/// <param name="viewTitle">The title of the fullscreen view being navigated to.</param>
	public void UpdateBreadcrumbForFullscreen(string originRoute, string viewTitle)
	{
		// Mark the current route as a fullscreen route so that NavigatePage's
		// early-exit guard does not fire when navigating back to the origin route.
		_currentRoute = $"Fullscreen:{originRoute}:{viewTitle}";

		// Requirement 4.5: Limit breadcrumb depth to two segments for nested fullscreen views.
		// The root label shows the origin title, the current label shows the view title.
		// If the origin is itself a fullscreen route (nested navigation), extract the
		// immediate origin title to maintain a maximum depth of two segments.
		string originTitle;
		if (originRoute != null && originRoute.StartsWith("Fullscreen:", StringComparison.Ordinal))
		{
			// Nested fullscreen: extract the title from the fullscreen route format
			// Format: "Fullscreen:{OriginalRoute}:{Title}"
			var parts = originRoute.Split(new[] { ':' }, 3);
			originTitle = parts.Length >= 3 ? parts[2] : RouteToTitle(originRoute);
		}
		else
		{
			originTitle = RouteToTitle(originRoute ?? "Home");
		}

		// Requirement 4.1: Set root label to OriginTitle
		breadcrumbRootLabel.Text = originTitle;
		breadcrumbRootLabel.Visibility = Visibility.Visible;
		breadcrumbSeparator.Visibility = Visibility.Visible;

		// Requirement 4.4: Truncate ViewTitle with ellipsis if exceeding 50 characters
		const int MaxViewTitleLength = 50;
		if (!string.IsNullOrEmpty(viewTitle) && viewTitle.Length > MaxViewTitleLength)
		{
			breadcrumbLabel.Text = viewTitle.Substring(0, MaxViewTitleLength) + "…";
			breadcrumbLabel.ToolTip = viewTitle;
		}
		else
		{
			breadcrumbLabel.Text = viewTitle ?? string.Empty;
			breadcrumbLabel.ToolTip = null;
		}

		// Hide sidebar, top bar, and status bar for true fullscreen experience
		EnterFullscreenShell();
	}

	/// <summary>
	/// Hides the sidebar, top bar, and status bar to provide a true fullscreen view experience.
	/// Called when navigating to a fullscreen form/view.
	/// </summary>
	private void EnterFullscreenShell()
	{
		SidebarRoot.Visibility = Visibility.Collapsed;
		TopBarContainer.Visibility = Visibility.Collapsed;
		BottomStatusBar.Visibility = Visibility.Collapsed;
		SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
		SidebarColumn.Width = new GridLength(0);
		TopBarRow.Height = new GridLength(0);
		StatusBarRow.Height = new GridLength(0);
	}

	/// <summary>
	/// Restores the sidebar, top bar, and status bar after exiting a fullscreen view.
	/// Called automatically by UpdateShellForRoute when navigating back to a normal page.
	/// </summary>
	private void ExitFullscreenShell()
	{
		TopBarContainer.Visibility = Visibility.Visible;
		BottomStatusBar.Visibility = Visibility.Visible;
		TopBarRow.Height = new GridLength(36.0);
		StatusBarRow.Height = new GridLength(22.0);
	}

	private void SyncNavigationSelection(string route)
	{
		RadioButton radioButton = route switch
		{
			"Home" => navHome, 
			"DashboardNotifications" => navDashboard, 
			"Dashboard" => navDashboard, 
			"Statistics" => navStatistics,
			"GovernanceRegistry" => navGovernanceRegistry, 
			"ResidentWorkspace" => navResidents, 
			"ResidentSoloParents" => navSoloParents, 
			"ResidentYouth" => navYouth, 
			"ResidentIndigent" => navIndigent, 
			"Households" => navHouseholds, 
			"ResidentCategories" => navCategories, 
			"DeceasedRegistry" => navDeceased, 
			"Clearances" => navClearances, 
			"Permits" => navPermits, 
			"ResidentCases" => navBlotter, 
			"TanodPatrol" => navTanod,
			"EmergencyContacts" => navEmergencyContacts,
			"Meetings" => navMeetings,
			"FacilityBooking" => navFacilityBooking,
			"ResidentPayments" => navPayments, 
			"Ayuda" => navAyuda, 
			"Collections" => navCollections, 
			"Officials" => navOfficials, 
			"StaffUsers" => navStaff, 
			"RolePermissions" => navRoles, 
			"SystemLogs" => navLogs, 
			"NotificationOutbox" => navNotificationOutbox, 
			"Settings" => navSettings, 
			_ => null, 
		};
		if (radioButton == null || radioButton.IsChecked.GetValueOrDefault())
		{
			return;
		}
		_isSynchronizingNavigation = true;
		try
		{
			radioButton.IsChecked = true;
		}
		finally
		{
			_isSynchronizingNavigation = false;
		}
	}

	private static string RouteToTitle(string route)
	{
		return route switch
		{
			"Home" => "Home", 
			"DashboardNotifications" => "Dashboard Notifications", 
			"Dashboard" => "Dashboard", 
			"Statistics" => "Statistics",
			"GovernanceRegistry" => "Announcements & Projects", 
			"ResidentWorkspace" => "Resident Records", 
			"ResidentSoloParents" => "Solo Parent Registry", 
			"ResidentYouth" => "Youth Registry", 
			"ResidentIndigent" => "Indigent Registry", 
			"Households" => "Households", 
			"ResidentCategories" => "Tags & Categories", 
			"DeceasedRegistry" => "Deceased Registry", 
			"Clearances" => "Clearances Queue", 
			"Permits" => "Permits Queue", 
			"ResidentCases" => "Blotter Cases", 
			"TanodPatrol" => "Tanod Patrol",
			"EmergencyContacts" => "Emergency Contacts",
			"Meetings" => "Meetings & Resolutions",
			"FacilityBooking" => "Facility Booking",
			"ResidentPayments" => "Payments", 
			"Ayuda" => "Ayuda Assistance", 
			"Collections" => "Finance Operations", 
			"Reports" => "Module Reports", 
			"Officials" => "Barangay Officials", 
			"StaffUsers" => "Staff & Users", 
			"RolePermissions" => "Roles & Permissions", 
			"SystemLogs" => "System Logs", 
			"NotificationOutbox" => "Notification Outbox", 
			"Settings" => "System Settings", 
			_ => "Home", 
		};
	}}
