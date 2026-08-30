using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class RolePermissionService
{
	public async Task<IReadOnlyList<RolePermissionSummary>> GetRoleSummariesAsync(string? search = null)
	{
		string trimmed = search?.Trim() ?? string.Empty;
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync(@"
			SELECT r.role_id, r.name, COALESCE(r.description, '') AS description,
			       COUNT(DISTINCT ur.user_id) AS user_count,
			       SUM(CASE WHEN IFNULL(ua.is_active, 1) = 1 THEN 1 ELSE 0 END) AS active_user_count
			FROM role r
			LEFT JOIN user_role ur ON ur.role_id = r.role_id
			LEFT JOIN user_account ua ON ua.user_id = ur.user_id
			WHERE COALESCE(r.is_active, 1) = 1
			  AND (@q = '' OR r.name LIKE @search OR COALESCE(r.description, '') LIKE @search)
			GROUP BY r.role_id, r.name, r.description", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@q", (object)trimmed);
			cmd.Parameters.AddWithValue("@search", (object)("%" + trimmed + "%"));
		}).ConfigureAwait(continueOnCapturedContext: false);
		List<RolePermissionSummary> list = new List<RolePermissionSummary>();
		foreach (DataRow row in obj.Rows)
		{
			string text = Convert.ToString(row["name"])?.Trim() ?? string.Empty;
			list.Add(new RolePermissionSummary
			{
				RoleId = Convert.ToInt32(row["role_id"]),
				Name = text,
				Description = (Convert.ToString(row["description"])?.Trim() ?? string.Empty),
				UserCount = ((row["user_count"] != DBNull.Value) ? Convert.ToInt32(row["user_count"]) : 0),
				ActiveUserCount = ((row["active_user_count"] != DBNull.Value) ? Convert.ToInt32(row["active_user_count"]) : 0),
				IsCoreRole = PermissionCatalog.IsCoreRole(text),
				IsSuperAdmin = string.Equals(text, "Super Admin", StringComparison.OrdinalIgnoreCase)
			});
		}
		list.Sort((RolePermissionSummary left, RolePermissionSummary right) => PermissionCatalog.CompareRoles(left.Name, right.Name));
		return list;
	}

	public async Task<RolePermissionEditorModel?> GetRoleEditorAsync(int roleId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n            SELECT r.role_id,\n                   r.name,\n                   COALESCE(r.description, '') AS description,\n                   COUNT(DISTINCT ur.user_id) AS user_count,\n                   SUM(CASE WHEN IFNULL(ua.is_active, 1) = 1 THEN 1 ELSE 0 END) AS active_user_count\n            FROM role r\n            LEFT JOIN user_role ur ON ur.role_id = r.role_id\n            LEFT JOIN user_account ua ON ua.user_id = ur.user_id\n            WHERE r.role_id = @roleId\n            GROUP BY r.role_id, r.name, r.description\n            LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@roleId", (object)roleId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		DataRow dataRow = dataTable.Rows[0];
		string text = Convert.ToString(dataRow["name"])?.Trim() ?? string.Empty;
		RolePermissionEditorModel editor = new RolePermissionEditorModel
		{
			RoleId = Convert.ToInt32(dataRow["role_id"]),
			Name = text,
			Description = (Convert.ToString(dataRow["description"])?.Trim() ?? string.Empty),
			UserCount = ((dataRow["user_count"] != DBNull.Value) ? Convert.ToInt32(dataRow["user_count"]) : 0),
			ActiveUserCount = ((dataRow["active_user_count"] != DBNull.Value) ? Convert.ToInt32(dataRow["active_user_count"]) : 0),
			IsCoreRole = PermissionCatalog.IsCoreRole(text),
			IsSuperAdmin = string.Equals(text, "Super Admin", StringComparison.OrdinalIgnoreCase)
		};
		Dictionary<string, bool> dictionary = (from rowValue in (await DatabaseManagerAsync.LoadTableAsync("SELECT permission_key, is_allowed\n              FROM role_permission\n              WHERE role_id = @roleId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@roleId", (object)roleId);
			}).ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable()
			where rowValue.Table.Columns.Contains("permission_key")
			select rowValue).ToDictionary<DataRow, string, bool>((DataRow rowValue) => Convert.ToString(rowValue["permission_key"]) ?? string.Empty, (DataRow rowValue) => rowValue["is_allowed"] != DBNull.Value && Convert.ToInt32(rowValue["is_allowed"]) == 1, StringComparer.OrdinalIgnoreCase);
		foreach (PermissionCatalogItem item in from item in PermissionCatalog.All
			orderby item.GroupOrder, item.ItemOrder
			select item)
		{
			editor.Permissions.Add(new RolePermissionGrantItem
			{
				PermissionKey = item.Key,
				GroupName = item.GroupName,
				Label = item.Label,
				Description = item.Description,
				GroupOrder = item.GroupOrder,
				ItemOrder = item.ItemOrder,
				IsAllowed = (dictionary.TryGetValue(item.Key, out var value) && value)
			});
		}
		return editor;
	}

	public RolePermissionEditorModel CreateNewRoleDraft()
	{
		RolePermissionEditorModel rolePermissionEditorModel = new RolePermissionEditorModel();
		foreach (PermissionCatalogItem item in from item in PermissionCatalog.All
			orderby item.GroupOrder, item.ItemOrder
			select item)
		{
			rolePermissionEditorModel.Permissions.Add(new RolePermissionGrantItem
			{
				PermissionKey = item.Key,
				GroupName = item.GroupName,
				Label = item.Label,
				Description = item.Description,
				GroupOrder = item.GroupOrder,
				ItemOrder = item.ItemOrder,
				IsAllowed = false
			});
		}
		return rolePermissionEditorModel;
	}

	public async Task<IReadOnlyList<string>> GetRoleNameOptionsAsync()
	{
		List<string> list = (from row in (await DatabaseManagerAsync.LoadTableAsync("SELECT name FROM role WHERE COALESCE(is_active, 1) = 1 AND name IS NOT NULL AND TRIM(name) <> '' ORDER BY name ASC").ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable()
			select Convert.ToString(row["name"])?.Trim() into name
			where !string.IsNullOrWhiteSpace(name)
			select name).Distinct<string>(StringComparer.OrdinalIgnoreCase).Cast<string>().ToList();
		list.Sort(PermissionCatalog.CompareRoles);
		return list;
	}

	public async Task<int> SaveRoleAsync(RolePermissionEditorModel editor)
	{
		AuthorizationGuard.RequireSuperAdmin("manage roles and permissions");
		if (editor == null)
		{
			throw new ArgumentNullException("editor");
		}
		string trimmedName = (editor.Name ?? string.Empty).Trim();
		string trimmedDescription = (editor.Description ?? string.Empty).Trim();
		bool isNew = editor.RoleId <= 0;
		RolePermissionSummary rolePermissionSummary = null;
		if (!isNew)
		{
			rolePermissionSummary = (await GetRoleSummariesAsync().ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((RolePermissionSummary role) => role.RoleId == editor.RoleId);
			if (rolePermissionSummary == null)
			{
				throw new InvalidOperationException("The selected role could not be found anymore.");
			}
		}
		if (isNew && string.IsNullOrWhiteSpace(trimmedName))
		{
			throw new InvalidOperationException("Role name is required.");
		}
		string nameToPersist = (isNew ? trimmedName : rolePermissionSummary.Name);
		if (string.IsNullOrWhiteSpace(nameToPersist))
		{
			throw new InvalidOperationException("Role name is required.");
		}
		if (await RoleNameExistsAsync(nameToPersist, editor.RoleId).ConfigureAwait(continueOnCapturedContext: false))
		{
			throw new InvalidOperationException("A role with the same name already exists.");
		}
		Dictionary<string, bool> grants = editor.Permissions.Where((RolePermissionGrantItem permission) => !string.IsNullOrWhiteSpace(permission.PermissionKey)).GroupBy<RolePermissionGrantItem, string>((RolePermissionGrantItem permission) => permission.PermissionKey, StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, RolePermissionGrantItem>, string, bool>((IGrouping<string, RolePermissionGrantItem> group) => group.Key, (IGrouping<string, RolePermissionGrantItem> group) => group.Last().IsAllowed, StringComparer.OrdinalIgnoreCase);
		await using (DatabaseTransactionScope transaction = await DatabaseTransactionScope.BeginAsync().ConfigureAwait(false))
		{
			if (!isNew)
			{
				int affected = await transaction.ExecuteNonQueryAsync(
					"UPDATE role SET description = @description WHERE role_id = @roleId",
					new Dictionary<string, object?>
					{
						["@description"] = string.IsNullOrWhiteSpace(trimmedDescription) ? DBNull.Value : trimmedDescription,
						["@roleId"] = editor.RoleId
					}).ConfigureAwait(false);
				if (affected != 1)
				{
					throw new DBConcurrencyException("The role changed or was removed. Reload the role list.");
				}
			}
			else
			{
				long roleId = await transaction.ExecuteInsertAsync(
					"INSERT INTO role (name, description) VALUES (@name, @description)",
					new Dictionary<string, object?>
					{
						["@name"] = nameToPersist,
						["@description"] = string.IsNullOrWhiteSpace(trimmedDescription) ? DBNull.Value : trimmedDescription
					}).ConfigureAwait(false);
				editor.RoleId = checked((int)roleId);
				if (editor.RoleId <= 0)
				{
					throw new InvalidOperationException("The new role ID could not be resolved.");
				}
			}
			await transaction.ExecuteNonQueryAsync(
				"DELETE FROM role_permission WHERE role_id = @roleId",
				new Dictionary<string, object?> { ["@roleId"] = editor.RoleId }).ConfigureAwait(false);
			foreach (PermissionCatalogItem catalogItem in PermissionCatalog.All.OrderBy(item => item.GroupOrder).ThenBy(item => item.ItemOrder))
			{
				bool allowed = grants.TryGetValue(catalogItem.Key, out bool value) && value;
				await transaction.ExecuteNonQueryAsync(
					"INSERT INTO role_permission (role_id, permission_key, is_allowed) VALUES (@roleId, @permissionKey, @allowed)",
					new Dictionary<string, object?>
					{
						["@roleId"] = editor.RoleId,
						["@permissionKey"] = catalogItem.Key,
						["@allowed"] = allowed ? 1 : 0
					}).ConfigureAwait(false);
			}
			await transaction.CommitAsync().ConfigureAwait(false);
		}
		AuditTrailService.Log("Roles", "role", editor.RoleId, isNew ? "CREATE" : "UPDATE", rolePermissionSummary,
			new { editor.RoleId, Name = nameToPersist, Description = trimmedDescription, Grants = grants },
			"Role and permissions saved atomically.");
		Permissions.Refresh();
		return editor.RoleId;
	}

	public async Task DeleteRoleAsync(int roleId)
	{
		await ArchiveRoleAsync(roleId, "Archived through the role management screen.").ConfigureAwait(false);
	}

	public async Task ArchiveRoleAsync(int roleId, string reason)
	{
		AuthorizationGuard.RequireSuperAdmin("archive roles");
		string cleanReason = (reason ?? string.Empty).Trim();
		if (cleanReason.Length < 5)
		{
			throw new InvalidOperationException("Provide an archive reason with at least five characters.");
		}
		RolePermissionSummary? obj = (await GetRoleSummariesAsync().ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((RolePermissionSummary role) => role.RoleId == roleId) ?? throw new InvalidOperationException("The selected role could not be found anymore.");
		if (obj.IsCoreRole)
		{
			throw new InvalidOperationException("Core roles cannot be archived.");
		}
		if (obj.UserCount > 0)
		{
			throw new InvalidOperationException("This role is still assigned to one or more user accounts.");
		}
		int affected = await DatabaseManagerAsync.ExecuteNonQueryAsync(@"
			UPDATE role
			SET is_active = 0, archived_at = CURRENT_TIMESTAMP,
			    archived_by_user_id = @userId, archive_reason = @reason
			WHERE role_id = @roleId AND COALESCE(is_active, 1) = 1",
			cmd =>
			{
				cmd.Parameters.AddWithValue("@userId", UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value);
				cmd.Parameters.AddWithValue("@reason", cleanReason);
				cmd.Parameters.AddWithValue("@roleId", roleId);
			}).ConfigureAwait(false);
		if (affected != 1)
		{
			throw new DBConcurrencyException("The role changed or was archived. Reload the role list.");
		}
		AuditTrailService.Log("Roles", "role", roleId, "ARCHIVE", obj,
			new { obj.RoleId, obj.Name, IsActive = false, Reason = cleanReason },
			"Unassigned non-core role archived.");
		Permissions.Refresh();
	}

	private static async Task<bool> RoleNameExistsAsync(string roleName, int excludeRoleId)
	{
		return await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\n              FROM role\n              WHERE LOWER(name) = LOWER(@name)\n                AND (@excludeRoleId = 0 OR role_id <> @excludeRoleId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@name", (object)roleName);
			cmd.Parameters.AddWithValue("@excludeRoleId", (object)excludeRoleId);
		}).ConfigureAwait(continueOnCapturedContext: false) > 0;
	}
}
