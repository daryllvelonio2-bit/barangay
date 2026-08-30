using System;
using System.Data;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class CaseTimelineService
{
	private const int MaxEventTypeLength = 50;

	private const int MaxTitleLength = 150;

	private const int MaxStatusLength = 30;

	private const int MaxDetailsLength = 8000;

	public static void Log(int caseId, string eventType, string title, string? details = null, string? fromStatus = null, string? toStatus = null, int? userId = null)
	{
		if (caseId <= 0)
		{
			return;
		}
		try
		{
			string normalizedEventType = Normalize(eventType, MaxEventTypeLength, "EVENT");
			string normalizedTitle = Normalize(title, MaxTitleLength, "Update");
			string normalizedDetails = Normalize(details, MaxDetailsLength, string.Empty);
			string normalizedFromStatus = Normalize(fromStatus, MaxStatusLength, string.Empty);
			string normalizedToStatus = Normalize(toStatus, MaxStatusLength, string.Empty);
			DbHelper.ExecuteNonQuery(
				@"INSERT INTO case_timeline
					(case_id, event_type, event_title, event_details, from_status, to_status, created_by_user_id)
				  VALUES
					(@case_id, @event_type, @event_title, @event_details, @from_status, @to_status, @created_by)",
				command =>
			{
				command.Parameters.AddWithValue("@case_id", caseId);
				command.Parameters.AddWithValue("@event_type", normalizedEventType);
				command.Parameters.AddWithValue("@event_title", normalizedTitle);
				command.Parameters.AddWithValue("@event_details", ToDbNullable(normalizedDetails));
				command.Parameters.AddWithValue("@from_status", ToDbNullable(normalizedFromStatus));
				command.Parameters.AddWithValue("@to_status", ToDbNullable(normalizedToStatus));
				command.Parameters.AddWithValue("@created_by", userId.HasValue ? userId.Value : DBNull.Value);
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to write blotter timeline entry.", ex);
		}
	}

	public static DataTable LoadTimeline(int caseId, int limit = 80)
	{
		if (caseId <= 0)
		{
			return new DataTable();
		}
		int value = Math.Clamp(limit, 5, 200);
		return DbHelper.LoadTable($"SELECT ct.timeline_id,\n                               ct.created_at,\n                               ct.event_type,\n                               ct.event_title,\n                               ct.event_details,\n                               ct.from_status,\n                               ct.to_status,\n                               u.username AS created_by\n                        FROM case_timeline ct\n                        LEFT JOIN user_account u ON u.user_id = ct.created_by_user_id\n                        WHERE ct.case_id = @id\n                        ORDER BY ct.created_at DESC, ct.timeline_id DESC\n                        LIMIT {value}", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@id", (object)caseId);
		});
	}

	private static object ToDbNullable(string value) =>
		string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

	private static string Normalize(string? value, int maxLen, string fallback)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return fallback;
		}
		if (text.Length > maxLen)
		{
			return text.Substring(0, maxLen);
		}
		return text;
	}
}
