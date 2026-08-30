using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class PaymentLedgerService
{
	private const string GeneralPaymentTypeName = "General Payment";

	private const string GeneralPaymentTypeCode = "PAY";

	public async Task<DataTable> GetLedgerAsync()
	{
		await EnsurePaymentVoidSchemaAsync();
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT 'GENERAL' AS payment_source,\n                       p.payment_id,\n                       NULL AS doc_request_id,\n                       COALESCE(p.or_number, '') AS or_no,\n                       p.resident_name,\n                       'General Payment' AS item_type,\n                       p.amount,\n                       COALESCE(p.payment_method, 'Cash') AS payment_method,\n                       CASE WHEN pv.void_id IS NULL THEN 'PAID' ELSE 'VOID' END AS payment_status,\n                       DATE_FORMAT(p.payment_date, '%Y-%m-%d %h:%i %p') AS paid_at,\n                       '' AS document_no,\n                       COALESCE(p.remarks, '') AS remarks,\n                       COALESCE(pv.void_reason, '') AS void_reason,\n                       COALESCE(DATE_FORMAT(pv.voided_at, '%Y-%m-%d %h:%i %p'), '') AS voided_at\n                FROM payment_ledger p\n                LEFT JOIN payment_void pv\n                  ON pv.payment_source = 'GENERAL'\n                 AND pv.payment_id = p.payment_id\n                 AND pv.barangay_id = p.barangay_id\n                WHERE p.barangay_id = @barangayId\n                UNION ALL\n                SELECT 'DOCUMENT' AS payment_source,\n                       dp.payment_id,\n                       dp.doc_request_id,\n                       COALESCE(dp.or_no, dr.or_number, '') AS or_no,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       COALESCE(dt.name, 'General Payment') AS item_type,\n                       IFNULL(dp.amount, IFNULL(dr.fee, 0.00)) AS amount,\n                       COALESCE(dp.payment_method, 'Cash') AS payment_method,\n                       CASE WHEN pv.void_id IS NULL THEN 'PAID' ELSE 'VOID' END AS payment_status,\n                       DATE_FORMAT(dp.paid_at, '%Y-%m-%d %h:%i %p') AS paid_at,\n                       COALESCE(dr.document_no, '') AS document_no,\n                       COALESCE(dr.remarks, '') AS remarks,\n                       COALESCE(pv.void_reason, '') AS void_reason,\n                       COALESCE(DATE_FORMAT(pv.voided_at, '%Y-%m-%d %h:%i %p'), '') AS voided_at\n                FROM document_payment dp\n                LEFT JOIN document_request dr ON dr.doc_request_id = dp.doc_request_id\n                LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n                LEFT JOIN resident r ON r.resident_id = dr.resident_id\n                LEFT JOIN payment_void pv\n                  ON pv.payment_source = 'DOCUMENT'\n                 AND pv.payment_id = dp.payment_id\n                 AND pv.barangay_id = dr.barangay_id\n                WHERE dr.barangay_id = @barangayId\n                ORDER BY paid_at DESC\n                LIMIT 250", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", ResolveBarangayId());
		});
	}

	public async Task<string> RecordPaymentAsync(int residentId, string residentName, decimal amount, string orNumber, string paymentMethod, string remarks)
	{
		AuthorizationGuard.RequireAdmin("record payments");
		await EnsurePaymentVoidSchemaAsync();
		if (residentId <= 0)
		{
			throw new InvalidOperationException("A resident must be selected before recording payment.");
		}
		string finalRemarks = (string.IsNullOrWhiteSpace(remarks) ? ("General payment recorded for " + residentName + ".") : remarks.Trim());
		string safeOrNumber = (string.IsNullOrWhiteSpace(orNumber) ? BuildDocumentNumber() : orNumber.Trim());
		if (amount <= 0m)
		{
			throw new InvalidOperationException("Payment amount must be greater than zero.");
		}
		if (await OfficialReceiptExistsAsync(safeOrNumber))
		{
			throw new InvalidOperationException("This official receipt number is already in use.");
		}

		await EnsurePaymentLedgerSchemaAsync();

		// Record directly to payment_ledger table
		await DatabaseManagerAsync.ExecuteNonQueryAsync(
			"INSERT INTO payment_ledger (barangay_id, resident_id, resident_name, amount, or_number, payment_method, remarks, payment_date, created_by_user_id) VALUES (@barangayId, @residentId, @residentName, @amount, @orNumber, @paymentMethod, @remarks, CURRENT_TIMESTAMP, @userId)",
			delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
				cmd.Parameters.AddWithValue("@residentId", (object)residentId);
				cmd.Parameters.AddWithValue("@residentName", (object)residentName);
				cmd.Parameters.AddWithValue("@amount", (object)amount);
				cmd.Parameters.AddWithValue("@orNumber", (object)safeOrNumber);
				cmd.Parameters.AddWithValue("@paymentMethod", (object)paymentMethod.Trim());
				cmd.Parameters.AddWithValue("@remarks", (object)finalRemarks);
				cmd.Parameters.AddWithValue("@userId", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
			});

		AuditTrailService.Log(
			"Payments", "payment_ledger", safeOrNumber, "CREATE", null,
			new { residentId, residentName, amount, OrNumber = safeOrNumber, paymentMethod },
			"General payment recorded.");
		return safeOrNumber;
	}

	public async Task VoidPaymentAsync(string paymentSource, int paymentId, string reason)
	{
		AuthorizationGuard.RequireAdmin("void payments");
		await EnsurePaymentVoidSchemaAsync();
		string source = (paymentSource ?? string.Empty).Trim().ToUpperInvariant();
		if (source != "GENERAL" && source != "DOCUMENT")
		{
			throw new InvalidOperationException("The selected payment source is not valid.");
		}
		if (paymentId <= 0)
		{
			throw new InvalidOperationException("Select a valid payment to void.");
		}
		string safeReason = (reason ?? string.Empty).Trim();
		if (safeReason.Length < 5)
		{
			throw new InvalidOperationException("A clear void reason of at least five characters is required.");
		}
		int exists = source == "GENERAL"
			? await DatabaseManagerAsync.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM payment_ledger WHERE payment_id = @paymentId AND barangay_id = @barangayId",
				cmd =>
				{
					cmd.Parameters.AddWithValue("@paymentId", paymentId);
					cmd.Parameters.AddWithValue("@barangayId", ResolveBarangayId());
				})
			: await DatabaseManagerAsync.ExecuteScalarAsync<int>(
				"SELECT COUNT(*)\n                  FROM document_payment dp\n                  INNER JOIN document_request dr ON dr.doc_request_id = dp.doc_request_id\n                  WHERE dp.payment_id = @paymentId\n                    AND dr.barangay_id = @barangayId",
				cmd =>
				{
					cmd.Parameters.AddWithValue("@paymentId", paymentId);
					cmd.Parameters.AddWithValue("@barangayId", ResolveBarangayId());
				});
		if (exists <= 0)
		{
			throw new InvalidOperationException("The selected payment could not be found.");
		}
		if (await DatabaseManagerAsync.ExecuteScalarAsync<int>(
			"SELECT COUNT(*)\n              FROM payment_void\n              WHERE barangay_id = @barangayId\n                AND payment_source = @paymentSource\n                AND payment_id = @paymentId",
			cmd =>
			{
				cmd.Parameters.AddWithValue("@barangayId", ResolveBarangayId());
				cmd.Parameters.AddWithValue("@paymentSource", source);
				cmd.Parameters.AddWithValue("@paymentId", paymentId);
			}) > 0)
		{
			throw new InvalidOperationException("This payment is already void.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync(
			"INSERT INTO payment_void\n                (barangay_id, payment_source, payment_id, void_reason, voided_by_user_id, voided_at)\n              VALUES\n                (@barangayId, @paymentSource, @paymentId, @voidReason, @userId, CURRENT_TIMESTAMP)",
			cmd =>
			{
				cmd.Parameters.AddWithValue("@barangayId", ResolveBarangayId());
				cmd.Parameters.AddWithValue("@paymentSource", source);
				cmd.Parameters.AddWithValue("@paymentId", paymentId);
				cmd.Parameters.AddWithValue("@voidReason", safeReason);
				cmd.Parameters.AddWithValue("@userId", UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value);
			});
		AuditTrailService.Log(
			"Payments",
			"payment_void",
			$"{source}:{paymentId}",
			"VOID",
			null,
			new { PaymentSource = source, PaymentId = paymentId, Reason = safeReason },
			"Payment voided; original ledger entry retained.");
	}

	private async Task<bool> OfficialReceiptExistsAsync(string orNumber)
	{
		int count = await DatabaseManagerAsync.ExecuteScalarAsync<int>(
			"SELECT\n                  (SELECT COUNT(*) FROM payment_ledger WHERE UPPER(or_number) = UPPER(@orNumber) AND barangay_id = @barangayId)\n                + (SELECT COUNT(*)\n                     FROM document_payment dp\n                     INNER JOIN document_request dr ON dr.doc_request_id = dp.doc_request_id\n                    WHERE UPPER(COALESCE(dp.or_no, dr.or_number, '')) = UPPER(@orNumber)\n                      AND dr.barangay_id = @barangayId)",
			cmd =>
			{
				cmd.Parameters.AddWithValue("@orNumber", orNumber);
				cmd.Parameters.AddWithValue("@barangayId", ResolveBarangayId());
			});
		return count > 0;
	}

	private static async Task EnsurePaymentVoidSchemaAsync()
	{
		string ddl = OfflineDatabaseSupport.IsOffline
			? "CREATE TABLE IF NOT EXISTS payment_void (\n                    void_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL DEFAULT 1,\n                    payment_source TEXT NOT NULL,\n                    payment_id INTEGER NOT NULL,\n                    void_reason TEXT NOT NULL,\n                    voided_by_user_id INTEGER,\n                    voided_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    UNIQUE (barangay_id, payment_source, payment_id)\n                )"
			: "CREATE TABLE IF NOT EXISTS payment_void (\n                    void_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL DEFAULT 1,\n                    payment_source VARCHAR(20) NOT NULL,\n                    payment_id INT NOT NULL,\n                    void_reason TEXT NOT NULL,\n                    voided_by_user_id INT NULL,\n                    voided_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    UNIQUE KEY ux_payment_void_source (barangay_id, payment_source, payment_id),\n                    KEY idx_payment_void_date (voided_at)\n                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";
		await DatabaseManagerAsync.ExecuteNonQueryAsync(ddl);
	}

	private static async Task EnsurePaymentLedgerSchemaAsync()
	{
		string ddl = OfflineDatabaseSupport.IsOffline
			? "CREATE TABLE IF NOT EXISTS payment_ledger (\n                    payment_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL DEFAULT 1,\n                    resident_id INTEGER NOT NULL,\n                    resident_name TEXT NOT NULL,\n                    amount REAL NOT NULL DEFAULT 0.00,\n                    or_number TEXT NOT NULL,\n                    payment_method TEXT NOT NULL DEFAULT 'Cash',\n                    remarks TEXT,\n                    payment_date TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    created_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                )"
			: "CREATE TABLE IF NOT EXISTS payment_ledger (\n                    payment_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL DEFAULT 1,\n                    resident_id INT NOT NULL,\n                    resident_name VARCHAR(255) NOT NULL,\n                    amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,\n                    or_number VARCHAR(50) NOT NULL,\n                    payment_method VARCHAR(50) NOT NULL DEFAULT 'Cash',\n                    remarks TEXT NULL,\n                    payment_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    created_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',\n                    KEY idx_payment_ledger_barangay_date (barangay_id, payment_date),\n                    KEY idx_payment_ledger_or (barangay_id, or_number)\n                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";
		await DatabaseManagerAsync.ExecuteNonQueryAsync(ddl);
	}

	private static int ResolveBarangayId()
	{
		return UserSession.BarangayId > 0 ? UserSession.BarangayId : 1;
	}

	private async Task<int> EnsureGeneralPaymentDocumentTypeAsync()
	{
		int num = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT document_type_id\n                  FROM document_type\n                  WHERE UPPER(code) = @code\n                     OR UPPER(name) = @name\n                  ORDER BY document_type_id ASC\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@code", (object)"PAY");
			cmd.Parameters.AddWithValue("@name", (object)"General Payment".ToUpperInvariant());
		});
		if (num > 0)
		{
			return num;
		}
		try
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO document_type\n                        (name, code, requires_approval)\n                      VALUES\n                        (@name, @code, 0)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@name", (object)"General Payment");
				cmd.Parameters.AddWithValue("@code", (object)"PAY");
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to create General Payment document type on first attempt.", ex);
		}
		int num2 = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT document_type_id\n                  FROM document_type\n                  WHERE UPPER(code) = @code\n                     OR UPPER(name) = @name\n                  ORDER BY document_type_id ASC\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@code", (object)"PAY");
			cmd.Parameters.AddWithValue("@name", (object)"General Payment".ToUpperInvariant());
		});
		if (num2 <= 0)
		{
			throw new InvalidOperationException("The General Payment document type could not be prepared.");
		}
		return num2;
	}

	private static string BuildDocumentNumber()
	{
		string text = $"{"PAY"}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToUpperInvariant();
		if (text.Length > 30)
		{
			return text.Substring(0, 30);
		}
		return text;
	}

	private static object NormalizeNullable(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}
}
