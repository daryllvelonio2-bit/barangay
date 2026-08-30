using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Runs the reports dashboard through the shared provider abstraction when SQLite is
/// selected. It mirrors the MySQL report metrics without opening a remote connection.
/// </summary>
internal static class ReportsCompatibilityService
{
	public static ReportsDashboardData LoadDashboard(
		DateTime from,
		DateTime to,
		DateTime toExclusive,
		ReportsFilters filters)
	{
		return new ReportsDashboardData
		{
			Trends = LoadTrends(from, to, toExclusive, filters),
			Summary = LoadSummary(from, to, toExclusive, filters.PurokId),
			ServiceTimes = LoadServiceTimes(from, toExclusive, filters.PurokId),
			StaffPerformance = LoadStaffPerformance(from, toExclusive, filters.PurokId),
			Hotspots = LoadHotspots(from, toExclusive, filters.PurokId, filters.BlotterStatus)
		};
	}

	private static IReadOnlyList<MonthlyTrendRow> LoadTrends(
		DateTime from,
		DateTime to,
		DateTime toExclusive,
		ReportsFilters filters)
	{
		Dictionary<string, int> residents = LoadMonthlyCounts(
			@"SELECT DATE_FORMAT(date_registered, '%Y-%m') AS ym, COUNT(*) AS cnt
			  FROM resident
			  WHERE IFNULL(is_deleted, 0) = 0
			    AND date_registered BETWEEN @from AND @to
			    AND (@purokId IS NULL OR purok_id = @purokId)
			  GROUP BY ym",
			command => AddRangeParameters(command, from, to, toExclusive, filters.PurokId));
		Dictionary<string, int> certificates = LoadMonthlyCounts(
			@"SELECT DATE_FORMAT(dr.requested_at, '%Y-%m') AS ym, COUNT(*) AS cnt
			  FROM document_request dr
			  INNER JOIN resident r ON r.resident_id = dr.resident_id
			  WHERE " + CertificateStatusClause(filters.CertificateStatus) + @"
			    AND dr.requested_at >= @from
			    AND dr.requested_at < @toExcl
			    AND (@purokId IS NULL OR r.purok_id = @purokId)
			  GROUP BY ym",
			command => AddRangeParameters(command, from, to, toExclusive, filters.PurokId));
		Dictionary<string, int> blotters = LoadMonthlyCounts(
			@"SELECT DATE_FORMAT(cr.date_filed, '%Y-%m') AS ym, COUNT(*) AS cnt
			  FROM case_record cr
			  LEFT JOIN resident r ON r.resident_id = cr.complainant_id
			  WHERE cr.date_filed BETWEEN @from AND @to
			    " + BlotterStatusClause(filters.BlotterStatus) + @"
			    AND (@purokId IS NULL OR r.purok_id = @purokId)
			  GROUP BY ym",
			command => AddRangeParameters(command, from, to, toExclusive, filters.PurokId));
		List<MonthlyTrendRow> result = new();
		for (DateTime month = new(from.Year, from.Month, 1);
			month <= new DateTime(to.Year, to.Month, 1);
			month = month.AddMonths(1))
		{
			string key = month.ToString("yyyy-MM");
			result.Add(new MonthlyTrendRow
			{
				MonthKey = key,
				MonthLabel = month.ToString("MMM yyyy"),
				Residents = residents.TryGetValue(key, out int residentCount) ? residentCount : 0,
				Certificates = certificates.TryGetValue(key, out int certificateCount) ? certificateCount : 0,
				Blotters = blotters.TryGetValue(key, out int blotterCount) ? blotterCount : 0
			});
		}
		return result;
	}

	private static ReportsSummary LoadSummary(
		DateTime from,
		DateTime to,
		DateTime toExclusive,
		int? purokId)
	{
		return new ReportsSummary
		{
			NewResidents = Count(
				@"SELECT COUNT(*) FROM resident
				  WHERE IFNULL(is_deleted,0)=0
				    AND date_registered BETWEEN @from AND @to
				    AND (@purokId IS NULL OR purok_id = @purokId)",
				command => AddRangeParameters(command, from, to, toExclusive, purokId)),
			CertificateRequests = Count(
				@"SELECT COUNT(*) FROM document_request dr
				  INNER JOIN resident r ON r.resident_id = dr.resident_id
				  WHERE UPPER(dr.status) <> 'DRAFT'
				    AND dr.requested_at >= @from AND dr.requested_at < @toExcl
				    AND (@purokId IS NULL OR r.purok_id = @purokId)",
				command => AddRangeParameters(command, from, to, toExclusive, purokId)),
			CertificatesReleased = Count(
				@"SELECT COUNT(*) FROM document_request dr
				  INNER JOIN resident r ON r.resident_id = dr.resident_id
				  WHERE dr.released_at IS NOT NULL
				    AND dr.released_at >= @from AND dr.released_at < @toExcl
				    AND (@purokId IS NULL OR r.purok_id = @purokId)",
				command => AddRangeParameters(command, from, to, toExclusive, purokId)),
			BlottersFiled = Count(
				@"SELECT COUNT(*) FROM case_record cr
				  LEFT JOIN resident r ON r.resident_id = cr.complainant_id
				  WHERE cr.date_filed BETWEEN @from AND @to
				    AND (@purokId IS NULL OR r.purok_id = @purokId)",
				command => AddRangeParameters(command, from, to, toExclusive, purokId)),
			TotalResidents = Count(
				@"SELECT COUNT(*) FROM resident
				  WHERE IFNULL(is_deleted,0)=0
				    AND (@purokId IS NULL OR purok_id = @purokId)",
				command => command.Parameters.AddWithValue("@purokId", DbNullable(purokId))),
			PendingCertificates = Count(
				@"SELECT COUNT(*) FROM document_request dr
				  INNER JOIN resident r ON r.resident_id = dr.resident_id
				  WHERE UPPER(dr.status) IN ('SUBMITTED','APPROVED','REQUESTED')
				    AND (@purokId IS NULL OR r.purok_id = @purokId)",
				command => command.Parameters.AddWithValue("@purokId", DbNullable(purokId))),
			ActiveBlotters = Count(
				@"SELECT COUNT(*) FROM case_record cr
				  LEFT JOIN resident r ON r.resident_id = cr.complainant_id
				  WHERE UPPER(cr.status) IN ('OPEN','ONGOING')
				    AND (@purokId IS NULL OR r.purok_id = @purokId)",
				command => command.Parameters.AddWithValue("@purokId", DbNullable(purokId)))
		};
	}

	private static ServiceTimeMetrics LoadServiceTimes(
		DateTime from,
		DateTime toExclusive,
		int? purokId)
	{
		DataTable table = DbHelper.LoadTable(
			@"SELECT
			    SUM(CASE WHEN dr.approved_at IS NOT NULL
			              AND dr.approved_at >= @from AND dr.approved_at < @toExcl
			              AND dr.approved_at >= dr.requested_at THEN 1 ELSE 0 END) AS approval_samples,
			    AVG(CASE WHEN dr.approved_at IS NOT NULL
			              AND dr.approved_at >= @from AND dr.approved_at < @toExcl
			              AND dr.approved_at >= dr.requested_at
			             THEN TIMESTAMPDIFF(SECOND, dr.requested_at, dr.approved_at) END) AS approval_seconds,
			    SUM(CASE WHEN dr.released_at IS NOT NULL
			              AND dr.released_at >= @from AND dr.released_at < @toExcl
			              AND dr.released_at >= dr.approved_at THEN 1 ELSE 0 END) AS release_samples,
			    AVG(CASE WHEN dr.released_at IS NOT NULL
			              AND dr.released_at >= @from AND dr.released_at < @toExcl
			              AND dr.released_at >= dr.approved_at
			             THEN TIMESTAMPDIFF(SECOND, dr.approved_at, dr.released_at) END) AS release_seconds
			  FROM document_request dr
			  INNER JOIN resident r ON r.resident_id = dr.resident_id
			  WHERE (@purokId IS NULL OR r.purok_id = @purokId)",
			command =>
			{
				command.Parameters.AddWithValue("@from", from);
				command.Parameters.AddWithValue("@toExcl", toExclusive);
				command.Parameters.AddWithValue("@purokId", DbNullable(purokId));
			});
		DataRow row = table.Rows[0];
		return new ServiceTimeMetrics
		{
			ApprovalSamples = ReadInt(row, "approval_samples"),
			AvgRequestToApprovalSeconds = ReadDouble(row, "approval_seconds"),
			ReleaseSamples = ReadInt(row, "release_samples"),
			AvgApprovalToReleaseSeconds = ReadDouble(row, "release_seconds")
		};
	}

	private static IReadOnlyList<StaffPerformanceRow> LoadStaffPerformance(
		DateTime from,
		DateTime toExclusive,
		int? purokId)
	{
		DataTable table = DbHelper.LoadTable(
			@"SELECT ua.user_id, ua.username,
			         COALESCE(NULLIF(ua.full_name,''), NULLIF(CONCAT_WS(' ', ua.first_name, ua.last_name), ''), ua.username) AS display_name,
			         IFNULL(ua.is_active,1) AS is_active,
			         (SELECT COUNT(*) FROM document_request dr
			          INNER JOIN resident r ON r.resident_id = dr.resident_id
			          WHERE dr.approved_by_user_id = ua.user_id
			            AND dr.approved_at >= @from AND dr.approved_at < @toExcl
			            AND (@purokId IS NULL OR r.purok_id = @purokId)) AS approvals,
			         (SELECT COUNT(*) FROM document_request dr
			          INNER JOIN resident r ON r.resident_id = dr.resident_id
			          WHERE dr.released_by_user_id = ua.user_id
			            AND dr.released_at >= @from AND dr.released_at < @toExcl
			            AND (@purokId IS NULL OR r.purok_id = @purokId)) AS releases,
			         (SELECT COUNT(*) FROM case_timeline ct
			          INNER JOIN case_record cr ON cr.case_id = ct.case_id
			          LEFT JOIN resident r ON r.resident_id = cr.complainant_id
			          WHERE ct.created_by_user_id = ua.user_id
			            AND ct.event_type = 'STATUS_CHANGE'
			            AND ct.created_at >= @from AND ct.created_at < @toExcl
			            AND (@purokId IS NULL OR r.purok_id = @purokId)) AS status_changes,
			         (SELECT COUNT(*) FROM case_timeline ct
			          INNER JOIN case_record cr ON cr.case_id = ct.case_id
			          LEFT JOIN resident r ON r.resident_id = cr.complainant_id
			          WHERE ct.created_by_user_id = ua.user_id
			            AND ct.to_status IN ('SETTLED','REFERRED','CLOSED')
			            AND ct.created_at >= @from AND ct.created_at < @toExcl
			            AND (@purokId IS NULL OR r.purok_id = @purokId)) AS resolutions
			  FROM user_account ua
			  ORDER BY IFNULL(ua.is_active,1) DESC, ua.username",
			command =>
			{
				command.Parameters.AddWithValue("@from", from);
				command.Parameters.AddWithValue("@toExcl", toExclusive);
				command.Parameters.AddWithValue("@purokId", DbNullable(purokId));
			});
		List<StaffPerformanceRow> rows = new();
		foreach (DataRow row in table.Rows)
		{
			rows.Add(new StaffPerformanceRow
			{
				UserId = ReadInt(row, "user_id"),
				Username = Convert.ToString(row["username"]) ?? string.Empty,
				DisplayName = Convert.ToString(row["display_name"]) ?? string.Empty,
				IsActive = ReadInt(row, "is_active") != 0,
				ApprovalsCompleted = ReadInt(row, "approvals"),
				ReleasesCompleted = ReadInt(row, "releases"),
				BlotterStatusChanges = ReadInt(row, "status_changes"),
				BlotterResolutions = ReadInt(row, "resolutions")
			});
		}
		return rows;
	}

	private static IReadOnlyList<HotspotPoint> LoadHotspots(
		DateTime from,
		DateTime toExclusive,
		int? purokId,
		BlotterStatusFilter blotterStatus)
	{
		DataTable table = DbHelper.LoadTable(
			@"SELECT p.purok_id, p.name AS purok_name, p.latitude, p.longitude,
			         COUNT(cr.case_id) AS incident_count
			  FROM purok_sitio p
			  LEFT JOIN resident r ON r.purok_id = p.purok_id AND IFNULL(r.is_deleted,0) = 0
			  LEFT JOIN case_record cr ON cr.complainant_id = r.resident_id
			    AND cr.date_filed >= @from AND cr.date_filed < @toExcl
			    " + BlotterStatusClause(blotterStatus) + @"
			  WHERE p.barangay_id = @barangayId
			    AND (@purokId IS NULL OR p.purok_id = @purokId)
			  GROUP BY p.purok_id, p.name, p.latitude, p.longitude
			  ORDER BY incident_count DESC, p.name",
			command =>
			{
				command.Parameters.AddWithValue("@from", from);
				command.Parameters.AddWithValue("@toExcl", toExclusive);
				command.Parameters.AddWithValue("@barangayId", UserSession.BarangayId > 0 ? UserSession.BarangayId : 1);
				command.Parameters.AddWithValue("@purokId", DbNullable(purokId));
			});
		List<HotspotPoint> rows = new();
		foreach (DataRow row in table.Rows)
		{
			rows.Add(new HotspotPoint
			{
				PurokId = ReadInt(row, "purok_id"),
				PurokName = Convert.ToString(row["purok_name"]) ?? string.Empty,
				Latitude = row["latitude"] == DBNull.Value ? null : Convert.ToDouble(row["latitude"]),
				Longitude = row["longitude"] == DBNull.Value ? null : Convert.ToDouble(row["longitude"]),
				IncidentCount = ReadInt(row, "incident_count")
			});
		}
		return rows;
	}

	private static Dictionary<string, int> LoadMonthlyCounts(string sql, Action<MySqlCommand> configure)
	{
		Dictionary<string, int> result = new(StringComparer.Ordinal);
		foreach (DataRow row in DbHelper.LoadTable(sql, configure).Rows)
		{
			string key = Convert.ToString(row["ym"]) ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(key))
			{
				result[key] = ReadInt(row, "cnt");
			}
		}
		return result;
	}

	private static int Count(string sql, Action<MySqlCommand> configure) =>
		DbHelper.ExecuteScalar<int>(sql, configure);

	private static void AddRangeParameters(
		MySqlCommand command,
		DateTime from,
		DateTime to,
		DateTime toExclusive,
		int? purokId)
	{
		command.Parameters.AddWithValue("@from", from);
		command.Parameters.AddWithValue("@to", to);
		command.Parameters.AddWithValue("@toExcl", toExclusive);
		command.Parameters.AddWithValue("@purokId", DbNullable(purokId));
	}

	private static int ReadInt(DataRow row, string column) =>
		row[column] == DBNull.Value ? 0 : Convert.ToInt32(row[column]);

	private static double ReadDouble(DataRow row, string column) =>
		row[column] == DBNull.Value ? 0d : Convert.ToDouble(row[column]);

	private static object DbNullable(int? value) =>
		value.HasValue ? value.Value : DBNull.Value;

	private static string CertificateStatusClause(CertificateStatusFilter filter) => filter switch
	{
		CertificateStatusFilter.Pending => "UPPER(dr.status) IN ('SUBMITTED','APPROVED','REQUESTED')",
		CertificateStatusFilter.Submitted => "UPPER(dr.status) IN ('SUBMITTED','REQUESTED')",
		CertificateStatusFilter.Approved => "UPPER(dr.status) = 'APPROVED'",
		CertificateStatusFilter.Released => "UPPER(dr.status) IN ('RELEASED','ISSUED')",
		CertificateStatusFilter.Cancelled => "UPPER(dr.status) = 'CANCELLED'",
		CertificateStatusFilter.Rejected => "UPPER(dr.status) = 'REJECTED'",
		_ => "UPPER(dr.status) <> 'DRAFT'"
	};

	private static string BlotterStatusClause(BlotterStatusFilter filter) => filter switch
	{
		BlotterStatusFilter.Active => "AND UPPER(cr.status) IN ('OPEN','ONGOING')",
		BlotterStatusFilter.Settled => "AND UPPER(cr.status) = 'SETTLED'",
		BlotterStatusFilter.Referred => "AND UPPER(cr.status) = 'REFERRED'",
		BlotterStatusFilter.Closed => "AND UPPER(cr.status) = 'CLOSED'",
		_ => string.Empty
	};
}
