using System;
using System.Collections.Generic;

namespace baranggaysystem1.helper;

/// <summary>
/// Single authorization source for shell navigation, shortcuts, search results,
/// and the command palette. Page visibility must never be the access boundary.
/// </summary>
internal static class RouteAuthorization
{
	private static readonly HashSet<string> PublicStaffRoutes = new(StringComparer.OrdinalIgnoreCase)
	{
		"Home",
		"Dashboard",
		"DashboardNotifications",
		"Statistics",
		"EmergencyContacts"
	};

	public static bool CanNavigate(string? route)
	{
		string normalized = string.IsNullOrWhiteSpace(route) ? "Home" : route.Trim();
		if (UserSession.UserId <= 0 || UserSession.IsSessionLocked)
		{
			return false;
		}
		if (Permissions.IsAdmin)
		{
			return true;
		}
		if (PublicStaffRoutes.Contains(normalized))
		{
			return true;
		}

		return normalized switch
		{
			"GovernanceRegistry" => Permissions.CanManageAnnouncements || Permissions.CanManageProjects,
			"ResidentWorkspace" or "ResidentSoloParents" or "ResidentYouth" or "ResidentIndigent"
				=> HasAny(PermissionKeys.CreateResidents, PermissionKeys.UpdateResidents, PermissionKeys.DeleteResidents),
			"Households" => Permissions.CanViewHouseholds,
			"ResidentCategories" or "DeceasedRegistry"
				=> HasAny(PermissionKeys.CreateResidents, PermissionKeys.UpdateResidents, PermissionKeys.DeleteResidents),
			"Clearances" or "Permits"
				=> HasAny(
					PermissionKeys.RequestCertificates,
					PermissionKeys.EditCertificateRequests,
					PermissionKeys.ApproveCertificates,
					PermissionKeys.IssueCertificates,
					PermissionKeys.CancelCertificates,
					PermissionKeys.ExportCertificates),
			"ResidentCases" => Permissions.CanCreateBlotter || Permissions.CanUpdateBlotterStatus,
			"Reports" => Permissions.CanViewHotspotReports,
			"Settings" => Permissions.CanOpenSettings,
			_ => false
		};
	}

	public static void RequireRoute(string route)
	{
		if (!CanNavigate(route))
		{
			throw new UnauthorizedAccessException("Your account is not allowed to open this area.");
		}
	}

	private static bool HasAny(params string[] keys)
	{
		foreach (string key in keys)
		{
			if (Permissions.Has(key))
			{
				return true;
			}
		}
		return false;
	}
}
