using System;

namespace baranggaysystem1.helper;

/// <summary>
/// Service-layer authorization. UI visibility is only presentation and must
/// never be trusted to protect a mutation.
/// </summary>
internal static class AuthorizationGuard
{
	public static void RequirePermission(string permissionKey, string action)
	{
		if (!Permissions.Has(permissionKey))
		{
			Deny(action);
		}
	}

	public static void RequireAdmin(string action)
	{
		if (!Permissions.IsAdmin)
		{
			Deny(action);
		}
	}

	public static void RequireSuperAdmin(string action)
	{
		if (!string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase))
		{
			Deny(action);
		}
	}

	private static void Deny(string action)
	{
		string safeAction = string.IsNullOrWhiteSpace(action) ? "perform this action" : action.Trim();
		AppLogger.LogWarning(
			$"Authorization denied: user {UserSession.UserId} ({UserSession.Role}) attempted to {safeAction}.");
		throw new UnauthorizedAccessException($"You do not have permission to {safeAction}.");
	}
}
