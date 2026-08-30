using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using baranggaysystem1.Models;

namespace baranggaysystem1.Services;

internal sealed class ResidentDeathRecordService
{
	public Task<DataTable> GetRegistryAsync()
	{
		return DatabaseManagerAsync.LoadTableAsync(@"
			SELECT d.death_record_id, d.resident_id,
			       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,
			       r.sex, r.birth_date, COALESCE(p.name, '') AS purok,
			       d.date_of_death, COALESCE(d.place_of_death, '') AS place_of_death,
			       COALESCE(d.cause_of_death, '') AS cause_of_death,
			       COALESCE(d.certificate_reference, '') AS certificate_reference,
			       COALESCE(d.reported_by, '') AS reported_by,
			       UPPER(COALESCE(d.record_status, 'CONFIRMED')) AS record_status,
			       d.confirmed_at
			FROM resident_death_record d
			INNER JOIN resident r ON r.resident_id = d.resident_id
			LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id
			WHERE d.barangay_id = @barangayId
			ORDER BY d.date_of_death DESC, d.death_record_id DESC",
			cmd => cmd.Parameters.AddWithValue("@barangayId", UserSession.BarangayId));
	}

	public async Task<int> ConfirmAsync(ResidentDeathRecord record)
	{
		AuthorizationGuard.RequirePermission(PermissionKeys.UpdateResidents, "record a resident death");
		if (record.ResidentId <= 0) throw new InvalidOperationException("Select a resident.");
		if (record.DateOfDeath > DateTime.Today) throw new InvalidOperationException("Date of death cannot be in the future.");
		if (record.CertificateReference.Trim().Length < 3) throw new InvalidOperationException("Enter the death certificate or supporting reference.");
		if (record.ReportedBy.Trim().Length < 3) throw new InvalidOperationException("Enter who reported or verified the record.");

		await using DatabaseTransactionScope tx = await DatabaseTransactionScope.BeginAsync().ConfigureAwait(false);
		int duplicate = await tx.ExecuteScalarAsync<int>(
			"SELECT COUNT(*) FROM resident_death_record WHERE resident_id = @residentId AND UPPER(COALESCE(record_status, 'CONFIRMED')) = 'CONFIRMED'",
			new Dictionary<string, object?> { ["@residentId"] = record.ResidentId }).ConfigureAwait(false);
		if (duplicate > 0) throw new InvalidOperationException("This resident already has a confirmed death record.");
		long id = await tx.ExecuteInsertAsync(@"
			INSERT INTO resident_death_record
				(barangay_id, resident_id, date_of_death, place_of_death, cause_of_death,
				 certificate_reference, reported_by, notes, record_status,
				 confirmed_by_user_id, confirmed_at, created_at, updated_at)
			VALUES
				(@barangayId, @residentId, @dateOfDeath, @place, @cause,
				 @reference, @reportedBy, @notes, 'CONFIRMED',
				 @userId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
			new Dictionary<string, object?>
			{
				["@barangayId"] = UserSession.BarangayId,
				["@residentId"] = record.ResidentId,
				["@dateOfDeath"] = record.DateOfDeath.Date,
				["@place"] = NullIfEmpty(record.PlaceOfDeath),
				["@cause"] = NullIfEmpty(record.CauseOfDeath),
				["@reference"] = record.CertificateReference.Trim(),
				["@reportedBy"] = record.ReportedBy.Trim(),
				["@notes"] = NullIfEmpty(record.Notes),
				["@userId"] = UserSession.UserId > 0 ? UserSession.UserId : null
			}).ConfigureAwait(false);
		int affected = await tx.ExecuteNonQueryAsync(
			"UPDATE resident SET status = 'DECEASED', updated_at = CURRENT_TIMESTAMP WHERE resident_id = @residentId AND barangay_id = @barangayId AND UPPER(COALESCE(status, 'ACTIVE')) <> 'DECEASED'",
			new Dictionary<string, object?> { ["@residentId"] = record.ResidentId, ["@barangayId"] = UserSession.BarangayId }).ConfigureAwait(false);
		if (affected != 1) throw new InvalidOperationException("The selected resident is unavailable or already marked deceased.");
		await tx.CommitAsync().ConfigureAwait(false);
		AuditTrailService.Log("Residents", "resident_death_record", id, "CONFIRM_DEATH", null,
			new { record.ResidentId, record.DateOfDeath, record.CertificateReference },
			"Resident death verified and confirmed.");
		return checked((int)id);
	}

	public async Task ReverseAsync(int deathRecordId, int residentId, string reason)
	{
		AuthorizationGuard.RequirePermission(PermissionKeys.DeleteResidents, "correct a confirmed death record");
		string cleanReason = (reason ?? string.Empty).Trim();
		if (cleanReason.Length < 5) throw new InvalidOperationException("Provide a correction reason with at least five characters.");
		await using DatabaseTransactionScope tx = await DatabaseTransactionScope.BeginAsync().ConfigureAwait(false);
		int affected = await tx.ExecuteNonQueryAsync(@"
			UPDATE resident_death_record
			SET record_status = 'REVERSED', reversal_reason = @reason,
			    reversed_by_user_id = @userId, reversed_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP
			WHERE death_record_id = @id AND resident_id = @residentId
			  AND barangay_id = @barangayId AND UPPER(COALESCE(record_status, 'CONFIRMED')) = 'CONFIRMED'",
			new Dictionary<string, object?>
			{
				["@reason"] = cleanReason,
				["@userId"] = UserSession.UserId > 0 ? UserSession.UserId : null,
				["@id"] = deathRecordId,
				["@residentId"] = residentId,
				["@barangayId"] = UserSession.BarangayId
			}).ConfigureAwait(false);
		if (affected != 1) throw new InvalidOperationException("Only a confirmed death record can be corrected.");
		await tx.ExecuteNonQueryAsync(
			"UPDATE resident SET status = 'ACTIVE', updated_at = CURRENT_TIMESTAMP WHERE resident_id = @residentId AND barangay_id = @barangayId",
			new Dictionary<string, object?> { ["@residentId"] = residentId, ["@barangayId"] = UserSession.BarangayId }).ConfigureAwait(false);
		await tx.CommitAsync().ConfigureAwait(false);
		AuditTrailService.Log("Residents", "resident_death_record", deathRecordId, "REVERSE",
			new { Status = "CONFIRMED" }, new { Status = "REVERSED", Reason = cleanReason },
			"Incorrect death record reversed; resident restored to active.");
	}

	private static object? NullIfEmpty(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
