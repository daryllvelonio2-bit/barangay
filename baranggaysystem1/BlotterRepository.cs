using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class BlotterRepository
{
	public Task<DataTable> LoadCaseListAsync(int barangayId, CancellationToken cancellationToken = default(CancellationToken))
	{
		int targetBarangayId = HouseholdRepository.ResolveBarangayId(barangayId);
		return DatabaseManagerAsync.LoadTableAsync("\nSELECT cr.case_id,\n       COALESCE(\n           NULLIF(TRIM(cr.case_no), ''),\n           CONCAT('BLT-', DATE_FORMAT(COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()), '%Y'), '-', LPAD(cr.case_id, 5, '0'))\n       ) AS case_no,\n       COALESCE(\n           NULLIF(TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name)), ''),\n           CASE\n               WHEN cr.complainant_id IS NOT NULL THEN CONCAT('Resident #', cr.complainant_id)\n               ELSE 'Unassigned resident'\n           END\n       ) AS complainant_name,\n       COALESCE(NULLIF(TRIM(cr.respondent_name), ''), 'Unspecified respondent') AS respondent_name,\n       COALESCE(NULLIF(TRIM(cr.incident_type), ''), 'General') AS incident_type,\n       DATE_FORMAT(COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)), '%Y-%m-%d') AS incident_date,\n       UPPER(COALESCE(cr.status, 'ONGOING')) AS status\nFROM case_record cr\nLEFT JOIN resident r ON r.resident_id = cr.complainant_id\nWHERE cr.barangay_id = @barangayId\nORDER BY COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)) DESC,\n         cr.case_id DESC\nLIMIT 300;", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)targetBarangayId);
		}, cancellationToken);
	}

	public async Task<BlotterDto?> LoadCaseAsync(int caseId, CancellationToken cancellationToken = default(CancellationToken))
	{
		DataTable dt = await DatabaseManagerAsync.LoadTableAsync("\nSELECT cr.case_id,\n       COALESCE(cr.case_no, '') AS case_no,\n       cr.complainant_id,\n       COALESCE(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name), '') AS complainant_name,\n       COALESCE(h.street, '') AS complainant_address,\n       cr.respondent_resident_id,\n       COALESCE(cr.respondent_name, '') AS respondent_name,\n       COALESCE(cr.incident_type, '') AS incident_type,\n       cr.incident_date,\n       cr.incident_time,\n       COALESCE(cr.incident_location, '') AS incident_location,\n       COALESCE(cr.witness_names, '') AS witness_names,\n       COALESCE(cr.action_taken, '') AS action_taken,\n       COALESCE(cr.resolution_details, '') AS resolution_details,\n       COALESCE(cr.incident_details, '') AS incident_details,\n       UPPER(COALESCE(cr.status, 'ONGOING')) AS status,\n       COALESCE(cr.referral_destination, '') AS referral_destination,\n       COALESCE(cr.closure_notes, '') AS closure_notes,\n       COALESCE(cr.ai_summary, '') AS ai_summary,\n       COALESCE(cr.ai_category, '') AS ai_category,\n       COALESCE(cr.ai_risk_level, '') AS ai_risk_level\nFROM case_record cr\nLEFT JOIN resident r ON r.resident_id = cr.complainant_id\nLEFT JOIN household h ON h.household_id = r.household_id\nWHERE cr.case_id = @caseId\nLIMIT 1;", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@caseId", (object)caseId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

		if (dt.Rows.Count == 0) return null;
		DataRow row = dt.Rows[0];

		DateTime incidentDate = DateTime.Today;
		if (row["incident_date"] != DBNull.Value)
		{
			DateTime.TryParse(Convert.ToString(row["incident_date"]), out incidentDate);
		}

		return new BlotterDto
		{
			CaseId = Convert.ToInt32(row["case_id"]),
			CaseNo = (Convert.ToString(row["case_no"]) ?? string.Empty),
			ComplainantId = ((row["complainant_id"] != DBNull.Value) ? Convert.ToInt32(row["complainant_id"]) : 0),
			ComplainantName = (Convert.ToString(row["complainant_name"]) ?? string.Empty),
			ComplainantAddress = (Convert.ToString(row["complainant_address"]) ?? string.Empty),
			RespondentResidentId = ((row["respondent_resident_id"] == DBNull.Value) ? (int?)null : Convert.ToInt32(row["respondent_resident_id"])),
			RespondentName = (Convert.ToString(row["respondent_name"]) ?? string.Empty),
			IncidentType = (Convert.ToString(row["incident_type"]) ?? string.Empty),
			IncidentDate = incidentDate,
			IncidentTime = null,
			IncidentLocation = (Convert.ToString(row["incident_location"]) ?? string.Empty),
			Witnesses = (Convert.ToString(row["witness_names"]) ?? string.Empty),
			ActionTaken = (Convert.ToString(row["action_taken"]) ?? string.Empty),
			ResolutionDetails = (Convert.ToString(row["resolution_details"]) ?? string.Empty),
			IncidentDetails = (Convert.ToString(row["incident_details"]) ?? string.Empty),
			Status = WorkflowRules.NormalizeBlotterStatus(Convert.ToString(row["status"])),
			ReferralDestination = (Convert.ToString(row["referral_destination"]) ?? string.Empty),
			ClosureNotes = (Convert.ToString(row["closure_notes"]) ?? string.Empty),
			AiSummary = (Convert.ToString(row["ai_summary"]) ?? string.Empty),
			AiCategory = (Convert.ToString(row["ai_category"]) ?? string.Empty),
			AiRiskLevel = (Convert.ToString(row["ai_risk_level"]) ?? string.Empty)
		};
	}

	public async Task<BlotterResidentLookupItem?> GetResidentAsync(int residentId, CancellationToken cancellationToken = default(CancellationToken))
	{
		DataTable dt = await DatabaseManagerAsync.LoadTableAsync("\nSELECT r.resident_id,\n       CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,\n       COALESCE(r.contact_no, '') AS contact_no,\n       COALESCE(h.house_no, '') AS house_no,\n       COALESCE(h.street, '') AS street,\n       COALESCE(h.subdivision, '') AS subdivision,\n       COALESCE(p.name, '') AS purok_name,\n       COALESCE(h.address_note, '') AS address_note\nFROM resident r\nLEFT JOIN household h ON h.household_id = r.household_id\nLEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\nWHERE r.resident_id = @residentId\nLIMIT 1;", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (dt.Rows.Count == 0) return null;
		DataRow row = dt.Rows[0];
		return new BlotterResidentLookupItem
		{
			ResidentId = Convert.ToInt32(row["resident_id"]),
			FullName = (Convert.ToString(row["full_name"]) ?? string.Empty),
			ContactNo = (Convert.ToString(row["contact_no"]) ?? string.Empty),
			Address = BuildAddress(Convert.ToString(row["house_no"]), Convert.ToString(row["street"]), Convert.ToString(row["subdivision"]), Convert.ToString(row["purok_name"]), Convert.ToString(row["address_note"]))
		};
	}

	public async Task<IReadOnlyList<BlotterResidentLookupItem>> SearchResidentsAsync(int barangayId, string? searchText, CancellationToken cancellationToken = default(CancellationToken))
	{
		int targetBarangayId = HouseholdRepository.ResolveBarangayId(barangayId);
		string search = (searchText ?? string.Empty).Trim();
		string like = "%" + search + "%";
		string exactId = search;
		List<BlotterResidentLookupItem> residents = new List<BlotterResidentLookupItem>();
		DataTable dt = await DatabaseManagerAsync.LoadTableAsync("\nSELECT r.resident_id,\n       CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,\n       COALESCE(r.contact_no, '') AS contact_no,\n       COALESCE(h.house_no, '') AS house_no,\n       COALESCE(h.street, '') AS street,\n       COALESCE(h.subdivision, '') AS subdivision,\n       COALESCE(p.name, '') AS purok_name,\n       COALESCE(h.address_note, '') AS address_note\nFROM resident r\nLEFT JOIN household h ON h.household_id = r.household_id\nLEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\nWHERE r.barangay_id = @barangayId\n  AND IFNULL(r.is_deleted, 0) = 0\n  AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n  AND (\n      @searchText = ''\n      OR CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @searchLike\n      OR COALESCE(r.contact_no, '') LIKE @searchLike\n      OR CAST(r.resident_id AS CHAR) = @searchId\n  )\nORDER BY r.last_name, r.first_name, r.middle_name\nLIMIT 60;", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)targetBarangayId);
			cmd.Parameters.AddWithValue("@searchText", (object)search);
			cmd.Parameters.AddWithValue("@searchLike", (object)like);
			cmd.Parameters.AddWithValue("@searchId", (object)exactId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		foreach (DataRow row in dt.Rows)
		{
			residents.Add(new BlotterResidentLookupItem
			{
				ResidentId = Convert.ToInt32(row["resident_id"]),
				FullName = (Convert.ToString(row["full_name"]) ?? string.Empty),
				ContactNo = (Convert.ToString(row["contact_no"]) ?? string.Empty),
				Address = BuildAddress(Convert.ToString(row["house_no"]), Convert.ToString(row["street"]), Convert.ToString(row["subdivision"]), Convert.ToString(row["purok_name"]), Convert.ToString(row["address_note"]))
			});
		}
		return residents;
	}

	public async Task<BlotterSaveResult> SaveCaseAsync(BlotterDto dto, CancellationToken cancellationToken = default(CancellationToken))
	{
		AuthorizationGuard.RequirePermission(
			dto.CaseId <= 0 ? PermissionKeys.CreateBlotter : PermissionKeys.UpdateBlotterStatus,
			dto.CaseId <= 0 ? "create blotter cases" : "update blotter case details");
		int barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		string normalizedStatus = WorkflowRules.NormalizeBlotterStatus(dto.Status);
		if (dto.CaseId <= 0 && !WorkflowRules.TryValidateNewBlotterStatus(normalizedStatus, out string newStatusError))
			throw new InvalidOperationException(newStatusError);
		if (dto.CaseId > 0)
		{
			BlotterDto? existing = await LoadCaseAsync(dto.CaseId, cancellationToken).ConfigureAwait(false);
			if (existing == null)
				throw new InvalidOperationException("The selected blotter case no longer exists.");
			normalizedStatus = existing.Status;
		}
		object userId = (UserSession.UserId > 0) ? (object)UserSession.UserId : DBNull.Value;

		if (dto.CaseId <= 0)
		{
			// New case - INSERT
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\nINSERT INTO case_record\n    (barangay_id, case_type_id, case_no, date_filed, incident_date, incident_location, summary, status,\n     handled_by_user_id, complainant_id, respondent_resident_id, respondent_name, incident_type, incident_time,\n     witness_names, action_taken, resolution_details, incident_details, recorded_by)\nVALUES\n    (@barangayId, (SELECT case_type_id FROM case_type ORDER BY CASE WHEN UPPER(name) = 'GENERAL' THEN 0 ELSE 1 END LIMIT 1), NULL, @dateFiled, @incidentDate, @incidentLocation, @summary, @status,\n     @handledBy, @complainantId, @respondentResidentId, @respondentName, @incidentType, @incidentTime,\n     @witnessNames, @actionTaken, @resolutionDetails, @incidentDetails, @recordedBy);", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)barangayId);
				cmd.Parameters.AddWithValue("@dateFiled", (object)DateTime.Today);
				cmd.Parameters.AddWithValue("@incidentDate", (object)dto.IncidentDate.Date);
				cmd.Parameters.AddWithValue("@incidentLocation", ToDbValue(dto.IncidentLocation));
				cmd.Parameters.AddWithValue("@summary", ToDbValue(BuildSummary(dto)));
				cmd.Parameters.AddWithValue("@status", (object)normalizedStatus);
				cmd.Parameters.AddWithValue("@handledBy", userId);
				cmd.Parameters.AddWithValue("@complainantId", (dto.ComplainantId > 0) ? (object)dto.ComplainantId : DBNull.Value);
				cmd.Parameters.AddWithValue("@respondentResidentId", dto.RespondentResidentId.HasValue ? (object)dto.RespondentResidentId.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@respondentName", ToDbValue(dto.RespondentName));
				cmd.Parameters.AddWithValue("@incidentType", ToDbValue(dto.IncidentType));
				cmd.Parameters.AddWithValue("@incidentTime", dto.IncidentTime.HasValue ? (object)dto.IncidentTime.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@witnessNames", ToDbValue(dto.Witnesses));
				cmd.Parameters.AddWithValue("@actionTaken", ToDbValue(dto.ActionTaken));
				cmd.Parameters.AddWithValue("@resolutionDetails", ToDbValue(dto.ResolutionDetails));
				cmd.Parameters.AddWithValue("@incidentDetails", ToDbValue(dto.IncidentDetails));
				cmd.Parameters.AddWithValue("@recordedBy", userId);
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

			// Get the last inserted ID
			int caseId = await DatabaseManagerAsync.ExecuteScalarAsync<int>(
				"SELECT MAX(case_id) FROM case_record WHERE barangay_id = @barangayId",
				delegate(MySqlCommand cmd) { cmd.Parameters.AddWithValue("@barangayId", (object)barangayId); },
				cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

			string caseNo = ComposeCaseNumber(null, dto.IncidentDate, caseId);
			await DatabaseManagerAsync.ExecuteNonQueryAsync(
				"UPDATE case_record SET case_no = @caseNo WHERE case_id = @caseId",
				delegate(MySqlCommand cmd)
				{
					cmd.Parameters.AddWithValue("@caseNo", (object)caseNo);
					cmd.Parameters.AddWithValue("@caseId", (object)caseId);
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

			return new BlotterSaveResult { CaseId = caseId, CaseNo = caseNo, Status = normalizedStatus };
		}
		else
		{
			// Update existing case
			string existingCaseNo = (string.IsNullOrWhiteSpace(dto.CaseNo) ? ComposeCaseNumber(null, dto.IncidentDate, dto.CaseId) : dto.CaseNo.Trim());
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\nUPDATE case_record\nSET complainant_id = @complainantId,\n    respondent_resident_id = @respondentResidentId,\n    respondent_name = @respondentName,\n    incident_type = @incidentType,\n    incident_date = @incidentDate,\n    incident_time = @incidentTime,\n    incident_location = @incidentLocation,\n    summary = @summary,\n    witness_names = @witnessNames,\n    action_taken = @actionTaken,\n    resolution_details = @resolutionDetails,\n    incident_details = @incidentDetails,\n    case_no = @caseNo\nWHERE case_id = @caseId\n  AND barangay_id = @barangayId;", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)barangayId);
				cmd.Parameters.AddWithValue("@caseId", (object)dto.CaseId);
				cmd.Parameters.AddWithValue("@caseNo", (object)existingCaseNo);
				cmd.Parameters.AddWithValue("@complainantId", (dto.ComplainantId > 0) ? (object)dto.ComplainantId : DBNull.Value);
				cmd.Parameters.AddWithValue("@respondentResidentId", dto.RespondentResidentId.HasValue ? (object)dto.RespondentResidentId.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@respondentName", ToDbValue(dto.RespondentName));
				cmd.Parameters.AddWithValue("@incidentType", ToDbValue(dto.IncidentType));
				cmd.Parameters.AddWithValue("@incidentDate", (object)dto.IncidentDate.Date);
				cmd.Parameters.AddWithValue("@incidentTime", dto.IncidentTime.HasValue ? (object)dto.IncidentTime.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@incidentLocation", ToDbValue(dto.IncidentLocation));
				cmd.Parameters.AddWithValue("@summary", ToDbValue(BuildSummary(dto)));
				cmd.Parameters.AddWithValue("@witnessNames", ToDbValue(dto.Witnesses));
				cmd.Parameters.AddWithValue("@actionTaken", ToDbValue(dto.ActionTaken));
				cmd.Parameters.AddWithValue("@resolutionDetails", ToDbValue(dto.ResolutionDetails));
				cmd.Parameters.AddWithValue("@incidentDetails", ToDbValue(dto.IncidentDetails));
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

			return new BlotterSaveResult { CaseId = dto.CaseId, CaseNo = existingCaseNo, Status = normalizedStatus };
		}
	}

	public async Task<BlotterSaveResult> UpdateStatusAsync(int caseId, string originalStatus, string currentStatus, string? resolutionDetails, string? referralDestination, string? closureNotes, CancellationToken cancellationToken = default(CancellationToken))
	{
		AuthorizationGuard.RequirePermission(PermissionKeys.UpdateBlotterStatus, "change blotter case status");
		string normalizedStatus = WorkflowRules.NormalizeBlotterStatus(currentStatus);
		string normalizedOriginal = WorkflowRules.NormalizeBlotterStatus(originalStatus);
		if (!WorkflowRules.TryValidateBlotterTransition(normalizedOriginal, normalizedStatus, out string error))
			throw new InvalidOperationException(error);
		await using DatabaseTransactionScope transaction =
			await DatabaseTransactionScope.BeginAsync(cancellationToken).ConfigureAwait(false);
		int affected = await transaction.ExecuteNonQueryAsync(
			"\nUPDATE case_record\nSET status = @status,\n    resolution_details = @resolutionDetails,\n    referral_destination = @referralDestination,\n    closure_notes = @closureNotes,\n    closed_at = @closedAt,\n    closed_by_user_id = @closedBy\nWHERE case_id = @caseId;",
			new Dictionary<string, object?>
			{
				["@status"] = normalizedStatus,
				["@resolutionDetails"] = ToDbValue(resolutionDetails),
				["@referralDestination"] = ToDbValue(referralDestination),
				["@closureNotes"] = ToDbValue(closureNotes),
				["@closedAt"] = normalizedStatus == "CLOSED" ? DateTime.Now : DBNull.Value,
				["@closedBy"] = normalizedStatus == "CLOSED" && UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value,
				["@caseId"] = caseId
			}, cancellationToken).ConfigureAwait(false);
		if (affected <= 0)
			throw new InvalidOperationException("The blotter status update could not be saved.");
		await transaction.ExecuteNonQueryAsync(
			"INSERT INTO case_timeline (case_id, event_type, event_title, event_details, from_status, to_status, created_by_user_id) VALUES (@caseId, 'STATUS', @title, @details, @fromStatus, @toStatus, @userId)",
			new Dictionary<string, object?>
			{
				["@caseId"] = caseId,
				["@title"] = "Status updated to " + normalizedStatus,
				["@details"] = BuildStatusDetails(normalizedStatus, resolutionDetails, referralDestination, closureNotes),
				["@fromStatus"] = normalizedOriginal,
				["@toStatus"] = normalizedStatus,
				["@userId"] = UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value
			}, cancellationToken).ConfigureAwait(false);
		await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
		AuditTrailService.Log("Blotter", "case_record", caseId, "STATUS_CHANGE",
			new { Status = normalizedOriginal }, new { Status = normalizedStatus },
			"Blotter status updated.");
		return new BlotterSaveResult { CaseId = caseId, Status = normalizedStatus };
	}

	public async Task ScheduleMediationAsync(int caseId, DateTime scheduleAt, string venue, CancellationToken cancellationToken = default(CancellationToken))
	{
		AuthorizationGuard.RequirePermission(PermissionKeys.UpdateBlotterStatus, "schedule blotter mediation");
		if (scheduleAt <= DateTime.Now)
			throw new InvalidOperationException("Mediation must be scheduled for a future date and time.");
		if (string.IsNullOrWhiteSpace(venue))
			throw new InvalidOperationException("A mediation venue is required.");
		await using DatabaseTransactionScope transaction =
			await DatabaseTransactionScope.BeginAsync(cancellationToken).ConfigureAwait(false);
		await transaction.ExecuteNonQueryAsync(
			"\nINSERT INTO case_hearing\n    (case_id, schedule_at, venue, status, created_by_user_id)\nVALUES\n    (@caseId, @scheduleAt, @venue, 'SCHEDULED', @createdBy);",
			new Dictionary<string, object?>
			{
				["@caseId"] = caseId,
				["@scheduleAt"] = scheduleAt,
				["@venue"] = venue.Trim(),
				["@createdBy"] = UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value
			}, cancellationToken).ConfigureAwait(false);
		await transaction.ExecuteNonQueryAsync(
			"INSERT INTO case_timeline (case_id, event_type, event_title, event_details, created_by_user_id) VALUES (@caseId, 'MEDIATION', 'Mediation scheduled', @details, @userId)",
			new Dictionary<string, object?>
			{
				["@caseId"] = caseId,
				["@details"] = $"Schedule: {scheduleAt:MMM dd, yyyy hh:mm tt}\nVenue: {venue.Trim()}",
				["@userId"] = UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value
			}, cancellationToken).ConfigureAwait(false);
		await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
		AuditTrailService.Log("Blotter", "case_hearing", caseId, "CREATE", null,
			new { scheduleAt, Venue = venue.Trim() }, "Mediation scheduled.");
	}

	private static async Task<int> ResolveDefaultCaseTypeIdAsync(MySqlConnection conn, MySqlTransaction tx, CancellationToken cancellationToken)
	{
		MySqlCommand cmd = new MySqlCommand("\nSELECT case_type_id\nFROM case_type\nORDER BY CASE WHEN UPPER(name) = 'GENERAL' THEN 0 ELSE 1 END,\n         case_type_id\nLIMIT 1;", conn);
		try
		{
			cmd.Transaction = tx;
			object obj = await ((DbCommand)(object)cmd).ExecuteScalarAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (obj == null || obj == DBNull.Value)
			{
				throw new InvalidOperationException("No case type is configured for blotter records.");
			}
			return Convert.ToInt32(obj);
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}

	private static async Task EnsureCaseNumberAsync(MySqlConnection conn, MySqlTransaction tx, int caseId, string caseNo, CancellationToken cancellationToken)
	{
		MySqlCommand cmd = new MySqlCommand("UPDATE case_record SET case_no = @caseNo WHERE case_id = @caseId;", conn);
		try
		{
			cmd.Transaction = tx;
			cmd.Parameters.AddWithValue("@caseNo", (object)caseNo);
			cmd.Parameters.AddWithValue("@caseId", (object)caseId);
			await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}

	private static MySqlCommand BuildSaveCommand(string sql, MySqlConnection conn, MySqlTransaction tx, BlotterDto dto, string normalizedStatus, bool isNewCase)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand(sql, conn)
		{
			Transaction = tx
		};
		int num = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		object obj = ((UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
		object obj2 = ((dto.RecordedBy > 0) ? ((object)dto.RecordedBy) : ((UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value));
		val.Parameters.AddWithValue("@barangayId", (object)num);
		if (isNewCase)
		{
			val.Parameters.AddWithValue("@caseTypeId", (object)DBNull.Value);
		}
		val.Parameters.AddWithValue("@dateFiled", (object)DateTime.Today);
		val.Parameters.AddWithValue("@incidentDate", (object)dto.IncidentDate.Date);
		val.Parameters.AddWithValue("@incidentLocation", ToDbValue(dto.IncidentLocation));
		val.Parameters.AddWithValue("@summary", ToDbValue(BuildSummary(dto)));
		val.Parameters.AddWithValue("@status", (object)normalizedStatus);
		val.Parameters.AddWithValue("@handledBy", obj);
		val.Parameters.AddWithValue("@complainantId", (dto.ComplainantId > 0) ? ((object)dto.ComplainantId) : DBNull.Value);
		val.Parameters.AddWithValue("@respondentResidentId", dto.RespondentResidentId.HasValue ? ((object)dto.RespondentResidentId.Value) : DBNull.Value);
		val.Parameters.AddWithValue("@respondentName", ToDbValue(dto.RespondentName));
		val.Parameters.AddWithValue("@incidentType", ToDbValue(dto.IncidentType));
		val.Parameters.AddWithValue("@incidentTime", dto.IncidentTime.HasValue ? ((object)dto.IncidentTime.Value) : DBNull.Value);
		val.Parameters.AddWithValue("@witnessNames", ToDbValue(dto.Witnesses));
		val.Parameters.AddWithValue("@actionTaken", ToDbValue(dto.ActionTaken));
		val.Parameters.AddWithValue("@resolutionDetails", ToDbValue(dto.ResolutionDetails));
		val.Parameters.AddWithValue("@incidentDetails", ToDbValue(dto.IncidentDetails));
		val.Parameters.AddWithValue("@recordedBy", obj2);
		return val;
	}

	private static string BuildSummary(BlotterDto dto)
	{
		string text = (dto.IncidentDetails ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return (dto.IncidentType ?? "Blotter case").Trim();
		}
		if (text.Length > 300)
		{
			return text.Substring(0, 300);
		}
		return text;
	}

	private static string BuildFiledDetails(BlotterDto dto)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(dto.ComplainantName))
		{
			list.Add("Complainant: " + dto.ComplainantName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(dto.RespondentName))
		{
			list.Add("Respondent: " + dto.RespondentName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(dto.IncidentType))
		{
			list.Add("Type: " + dto.IncidentType.Trim());
		}
		list.Add("Incident date: " + dto.IncidentDate.ToString("MMM dd, yyyy"));
		if (!string.IsNullOrWhiteSpace(dto.IncidentLocation))
		{
			list.Add("Location: " + dto.IncidentLocation.Trim());
		}
		return string.Join("\n", list);
	}

	private static string BuildUpdateDetails(BlotterDto dto)
	{
		List<string> list = new List<string>
		{
			"Respondent: " + (string.IsNullOrWhiteSpace(dto.RespondentName) ? "Not specified" : dto.RespondentName.Trim()),
			"Type: " + (string.IsNullOrWhiteSpace(dto.IncidentType) ? "Not specified" : dto.IncidentType.Trim()),
			"Incident date: " + dto.IncidentDate.ToString("MMM dd, yyyy")
		};
		if (!string.IsNullOrWhiteSpace(dto.IncidentLocation))
		{
			list.Add("Location: " + dto.IncidentLocation.Trim());
		}
		return string.Join("\n", list);
	}

	private static string BuildStatusDetails(string status, string? resolutionDetails, string? referralDestination, string? closureNotes)
	{
		List<string> list = new List<string> { "New status: " + status };
		if (!string.IsNullOrWhiteSpace(resolutionDetails))
		{
			list.Add("Resolution: " + resolutionDetails.Trim());
		}
		if (!string.IsNullOrWhiteSpace(referralDestination))
		{
			list.Add("Referral: " + referralDestination.Trim());
		}
		if (!string.IsNullOrWhiteSpace(closureNotes))
		{
			list.Add("Closure notes: " + closureNotes.Trim());
		}
		return string.Join("\n", list);
	}

	private static object ToDbValue(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static string ComposeCaseNumber(string? existingCaseNo, DateTime incidentDate, int caseId)
	{
		if (!string.IsNullOrWhiteSpace(existingCaseNo))
		{
			return existingCaseNo.Trim();
		}
		DateTime value = ((incidentDate == default(DateTime)) ? DateTime.Today : incidentDate);
		return $"BLT-{value:yyyy}-{caseId:D5}";
	}

	private static string BuildAddress(string? houseNo, string? street, string? subdivision, string? purokName, string? addressNote)
	{
		return string.Join(", ", from value in new string[5] { houseNo, street, subdivision, purokName, addressNote }
			where !string.IsNullOrWhiteSpace(value)
			select value.Trim());
	}

	public Task<DataTable> LoadCasesForResidentAsync(int residentId, CancellationToken cancellationToken = default(CancellationToken))
	{
		return DatabaseManagerAsync.LoadTableAsync("\nSELECT cr.case_id,\n       COALESCE(\n           NULLIF(TRIM(cr.case_no), ''),\n           CONCAT('BLT-', DATE_FORMAT(COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()), '%Y'), '-', LPAD(cr.case_id, 5, '0'))\n       ) AS case_no,\n       COALESCE(\n           NULLIF(TRIM(CONCAT_WS(' ', rc.first_name, rc.middle_name, rc.last_name)), ''),\n           CASE\n               WHEN cr.complainant_id IS NOT NULL THEN CONCAT('Resident #', cr.complainant_id)\n               ELSE 'Unassigned'\n           END\n       ) AS complainant_name,\n       COALESCE(NULLIF(TRIM(cr.respondent_name), ''), 'Unspecified') AS respondent_name,\n       COALESCE(NULLIF(TRIM(cr.incident_type), ''), 'General') AS incident_type,\n       DATE_FORMAT(COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)), '%Y-%m-%d') AS incident_date,\n       UPPER(COALESCE(cr.status, 'ONGOING')) AS status,\n       CASE\n           WHEN cr.complainant_id = @residentId AND cr.respondent_resident_id = @residentId THEN 'Both'\n           WHEN cr.complainant_id = @residentId THEN 'Complainant'\n           ELSE 'Respondent'\n       END AS involvement\nFROM case_record cr\nLEFT JOIN resident rc ON rc.resident_id = cr.complainant_id\nWHERE cr.complainant_id = @residentId\n   OR cr.respondent_resident_id = @residentId\nORDER BY COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)) DESC,\n         cr.case_id DESC\nLIMIT 200;", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
		}, cancellationToken);
	}

	private static async Task<(DateTime? scheduleAt, string venue)> LoadLatestHearingAsync(MySqlConnection conn, int caseId, CancellationToken cancellationToken)
	{
		MySqlCommand cmd = new MySqlCommand("\nSELECT schedule_at, COALESCE(venue, '') AS venue\nFROM case_hearing\nWHERE case_id = @caseId\nORDER BY schedule_at DESC, hearing_id DESC\nLIMIT 1;", conn);
		try
		{
			cmd.Parameters.AddWithValue("@caseId", (object)caseId);
			MySqlDataReader reader = (MySqlDataReader)(await ((DbCommand)(object)cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
			try
			{
				if (!(await ((DbDataReader)(object)reader).ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
				{
					return (scheduleAt: null, venue: string.Empty);
				}
				DateTime? item = ((((DbDataReader)(object)reader)["schedule_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(((DbDataReader)(object)reader)["schedule_at"])));
				string item2 = Convert.ToString(((DbDataReader)(object)reader)["venue"]) ?? string.Empty;
				return (scheduleAt: item, venue: item2);
			}
			finally
			{
				((IDisposable)reader)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}
}
