using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Provides bulk import functionality for residents from CSV/Excel files.
/// Supports validation, duplicate detection, and batch insert with rollback.
/// </summary>
public static class BulkImportService
{
    /// <summary>
    /// Expected CSV column headers (case-insensitive matching).
    /// </summary>
    private static readonly string[] RequiredColumns = new[]
    {
        "last_name", "first_name"
    };

    private static readonly string[] OptionalColumns = new[]
    {
        "middle_name", "suffix", "gender", "birthdate", "civil_status",
        "contact_no", "email", "address", "purok", "occupation",
        "nationality", "religion", "blood_type", "voter_status",
        "is_pwd", "is_senior", "is_4ps", "is_solo_parent", "is_youth", "is_indigent",
        "household_no", "house_no", "street"
    };

    /// <summary>
    /// Parse a CSV file and return validated import rows.
    /// </summary>
    public static BulkImportParseResult ParseCsv(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return BulkImportParseResult.Error("File not found: " + filePath);

        try
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length < 2)
                return BulkImportParseResult.Error("CSV file must have a header row and at least one data row.");

            string[] headers = ParseCsvLine(lines[0])
                .Select(h => h.Trim().ToLowerInvariant().Replace(" ", "_"))
                .ToArray();

            // Validate required columns exist
            var missingRequired = RequiredColumns
                .Where(r => !headers.Contains(r))
                .ToList();

            if (missingRequired.Count > 0)
                return BulkImportParseResult.Error(
                    $"Missing required columns: {string.Join(", ", missingRequired)}. " +
                    $"Required: {string.Join(", ", RequiredColumns)}");

            var rows = new List<BulkImportRow>();
            var errors = new List<BulkImportRowError>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] values = ParseCsvLine(lines[i]);
                var row = MapRow(headers, values, i + 1);

                var validationErrors = ValidateRow(row);
                if (validationErrors.Count > 0)
                {
                    errors.Add(new BulkImportRowError
                    {
                        LineNumber = i + 1,
                        RawData = lines[i],
                        Errors = validationErrors
                    });
                }
                else
                {
                    rows.Add(row);
                }
            }

            return new BulkImportParseResult
            {
                IsSuccess = true,
                Rows = rows,
                Errors = errors,
                TotalLinesRead = lines.Length - 1,
                Message = $"Parsed {rows.Count} valid rows, {errors.Count} rows with errors."
            };
        }
        catch (Exception ex)
        {
            return BulkImportParseResult.Error($"Failed to parse CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute the bulk import into the database.
    /// Uses a transaction so all-or-nothing on failure.
    /// </summary>
    public static BulkImportResult Import(IReadOnlyList<BulkImportRow> rows, bool skipDuplicates = true)
    {
        AuthorizationGuard.RequirePermission(PermissionKeys.CreateResidents, "bulk import residents");
        if (rows == null || rows.Count == 0)
            return new BulkImportResult { IsSuccess = false, Message = "No rows to import." };

        int inserted = 0;
        int skipped = 0;
        var duplicates = new List<string>();

        bool sqlite = OfflineDatabaseSupport.IsOffline || DBConnection.IsSqliteSelected();
        DbConnection connection = sqlite
            ? OfflineDatabaseSupport.GetConnection()
            : DBConnection.GetConnection();
        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            DbTransaction transaction = connection.BeginTransaction();
            try
            {
                // Load purok lookup
                var purokLookup = LoadPurokLookup(connection, transaction);

                foreach (var row in rows)
                {
                    // Check for duplicates
                    if (skipDuplicates && IsDuplicate(connection, transaction, row))
                    {
                        skipped++;
                        duplicates.Add($"{row.FirstName} {row.LastName}");
                        continue;
                    }

                    int? purokId = ResolvePurok(row.Purok, purokLookup);
                    InsertResident(connection, transaction, row, purokId);
                    inserted++;
                }

                transaction.Commit();

                AuditTrailService.Log("Residents", "resident", 0, "BULK_IMPORT", null,
                    new { Inserted = inserted, Skipped = skipped, Total = rows.Count },
                    $"Bulk import completed: {inserted} inserted, {skipped} skipped.");

                return new BulkImportResult
                {
                    IsSuccess = true,
                    InsertedCount = inserted,
                    SkippedCount = skipped,
                    DuplicateNames = duplicates,
                    Message = $"Import complete. {inserted} residents added, {skipped} duplicates skipped."
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                AppLogger.LogError("Bulk import failed, transaction rolled back.", ex);
                return new BulkImportResult
                {
                    IsSuccess = false,
                    InsertedCount = 0,
                    Message = $"Import failed and was rolled back: {ex.Message}"
                };
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    /// <summary>
    /// Generate a CSV template file with all supported columns.
    /// </summary>
    public static string GenerateTemplate(string outputPath)
    {
        var allColumns = RequiredColumns.Concat(OptionalColumns).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", allColumns));
        sb.AppendLine("\"Dela Cruz\",\"Juan\",\"Santos\",\"\",\"Male\",\"1990-05-15\",\"Single\",\"09171234567\",\"juan@email.com\",\"123 Main St\",\"Purok 1\",\"Farmer\",\"Filipino\",\"Catholic\",\"O+\",\"Registered\",\"0\",\"0\",\"0\",\"0\",\"0\",\"0\",\"\",\"123\",\"Main Street\"");

        string path = Path.Combine(outputPath, "resident_import_template.csv");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static BulkImportRow MapRow(string[] headers, string[] values, int lineNumber)
    {
        string GetValue(string header)
        {
            int idx = Array.IndexOf(headers, header);
            if (idx < 0 || idx >= values.Length) return string.Empty;
            return values[idx]?.Trim() ?? string.Empty;
        }

        return new BulkImportRow
        {
            LineNumber = lineNumber,
            LastName = GetValue("last_name"),
            FirstName = GetValue("first_name"),
            MiddleName = GetValue("middle_name"),
            Suffix = GetValue("suffix"),
            Gender = GetValue("gender"),
            Birthdate = ParseDate(GetValue("birthdate")),
            CivilStatus = GetValue("civil_status"),
            ContactNo = GetValue("contact_no"),
            Email = GetValue("email"),
            Address = GetValue("address"),
            Purok = GetValue("purok"),
            Occupation = GetValue("occupation"),
            Nationality = GetValue("nationality"),
            Religion = GetValue("religion"),
            BloodType = GetValue("blood_type"),
            VoterStatus = GetValue("voter_status"),
            IsPwd = ParseBool(GetValue("is_pwd")),
            IsSenior = ParseBool(GetValue("is_senior")),
            Is4Ps = ParseBool(GetValue("is_4ps")),
            IsSoloParent = ParseBool(GetValue("is_solo_parent")),
            IsYouth = ParseBool(GetValue("is_youth")),
            IsIndigent = ParseBool(GetValue("is_indigent")),
            HouseholdNo = GetValue("household_no"),
            HouseNo = GetValue("house_no"),
            Street = GetValue("street")
        };
    }

    private static List<string> ValidateRow(BulkImportRow row)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.LastName))
            errors.Add("Last name is required.");
        if (string.IsNullOrWhiteSpace(row.FirstName))
            errors.Add("First name is required.");
        if (!string.IsNullOrWhiteSpace(row.Gender) &&
            !new[] { "male", "female", "other" }.Contains(row.Gender.ToLowerInvariant()))
            errors.Add("Gender must be Male, Female, or Other.");
        if (!string.IsNullOrWhiteSpace(row.Email) && !row.Email.Contains('@'))
            errors.Add("Invalid email format.");

        return errors;
    }

    private static bool IsDuplicate(DbConnection conn, DbTransaction tx, BulkImportRow row)
    {
        using var cmd = CreateCommand(conn, tx,
            @"SELECT COUNT(*) FROM resident 
              WHERE IFNULL(is_deleted,0)=0 
              AND LOWER(first_name) = LOWER(@fn) 
              AND LOWER(last_name) = LOWER(@ln)
              AND (
                  @mn = '' OR LOWER(IFNULL(middle_name,'')) = LOWER(@mn)
              )");
        AddParameter(cmd, "@fn", row.FirstName);
        AddParameter(cmd, "@ln", row.LastName);
        AddParameter(cmd, "@mn", row.MiddleName ?? "");
        object? result = cmd.ExecuteScalar();
        return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
    }

    private static Dictionary<string, int> LoadPurokLookup(DbConnection conn, DbTransaction tx)
    {
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var cmd = CreateCommand(
            conn, tx, "SELECT purok_id, name FROM purok_sitio WHERE barangay_id = @bid");
        AddParameter(cmd, "@bid", UserSession.BarangayId);
        using DbDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string name = Convert.ToString(reader["name"]) ?? "";
            int id = Convert.ToInt32(reader["purok_id"]);
            if (!string.IsNullOrWhiteSpace(name))
                lookup[name.Trim()] = id;
        }
        return lookup;
    }

    private static int? ResolvePurok(string? purokName, Dictionary<string, int> lookup)
    {
        if (string.IsNullOrWhiteSpace(purokName)) return null;
        return lookup.TryGetValue(purokName.Trim(), out int id) ? id : null;
    }

    private static void InsertResident(DbConnection conn, DbTransaction tx, BulkImportRow row, int? purokId)
    {
        using var cmd = CreateCommand(conn, tx,
            @"INSERT INTO resident (
                barangay_id, purok_id, first_name, middle_name, last_name, suffix,
                gender, birthdate, civil_status, contact_no, email, address,
                occupation, nationality, religion, blood_type, voter_status,
                is_pwd, is_senior_citizen, is_4ps_beneficiary, is_solo_parent, is_youth, is_indigent,
                status, date_registered, created_at
              ) VALUES (
                @barangayId, @purokId, @firstName, @middleName, @lastName, @suffix,
                @gender, @birthdate, @civilStatus, @contactNo, @email, @address,
                @occupation, @nationality, @religion, @bloodType, @voterStatus,
                @isPwd, @isSenior, @is4Ps, @isSoloParent, @isYouth, @isIndigent,
                'ACTIVE', NOW(), NOW()
              )");
        AddParameter(cmd, "@barangayId", UserSession.BarangayId);
        AddParameter(cmd, "@purokId", purokId.HasValue ? purokId.Value : DBNull.Value);
        AddParameter(cmd, "@firstName", row.FirstName);
        AddParameter(cmd, "@middleName", ToDbValue(row.MiddleName));
        AddParameter(cmd, "@lastName", row.LastName);
        AddParameter(cmd, "@suffix", ToDbValue(row.Suffix));
        AddParameter(cmd, "@gender", ToDbValue(row.Gender));
        AddParameter(cmd, "@birthdate", row.Birthdate.HasValue ? row.Birthdate.Value : DBNull.Value);
        AddParameter(cmd, "@civilStatus", ToDbValue(row.CivilStatus));
        AddParameter(cmd, "@contactNo", ToDbValue(row.ContactNo));
        AddParameter(cmd, "@email", ToDbValue(row.Email));
        AddParameter(cmd, "@address", ToDbValue(row.Address));
        AddParameter(cmd, "@occupation", ToDbValue(row.Occupation));
        AddParameter(cmd, "@nationality", ToDbValue(row.Nationality));
        AddParameter(cmd, "@religion", ToDbValue(row.Religion));
        AddParameter(cmd, "@bloodType", ToDbValue(row.BloodType));
        AddParameter(cmd, "@voterStatus", ToDbValue(row.VoterStatus));
        AddParameter(cmd, "@isPwd", row.IsPwd ? 1 : 0);
        AddParameter(cmd, "@isSenior", row.IsSenior ? 1 : 0);
        AddParameter(cmd, "@is4Ps", row.Is4Ps ? 1 : 0);
        AddParameter(cmd, "@isSoloParent", row.IsSoloParent ? 1 : 0);
        AddParameter(cmd, "@isYouth", row.IsYouth ? 1 : 0);
        AddParameter(cmd, "@isIndigent", row.IsIndigent ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    private static DbCommand CreateCommand(DbConnection connection, DbTransaction transaction, string sql)
    {
        DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = connection is Microsoft.Data.Sqlite.SqliteConnection
            ? OfflineSqlCompat.NormalizeSql(sql)
            : sql;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)value.Trim();
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string[] formats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "M/d/yyyy", "yyyy/MM/dd" };
        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime result))
            return result;
        if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result))
            return result;
        return null;
    }

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.Trim() == "1" || value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }
}

/// <summary>
/// A single row parsed from the import file.
/// </summary>
public sealed class BulkImportRow
{
    public int LineNumber { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? Birthdate { get; set; }
    public string CivilStatus { get; set; } = string.Empty;
    public string ContactNo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Purok { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Religion { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string VoterStatus { get; set; } = string.Empty;
    public bool IsPwd { get; set; }
    public bool IsSenior { get; set; }
    public bool Is4Ps { get; set; }
    public bool IsSoloParent { get; set; }
    public bool IsYouth { get; set; }
    public bool IsIndigent { get; set; }
    public string HouseholdNo { get; set; } = string.Empty;
    public string HouseNo { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
}

/// <summary>
/// Result of parsing a CSV file for import.
/// </summary>
public sealed class BulkImportParseResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BulkImportRow> Rows { get; set; } = new();
    public List<BulkImportRowError> Errors { get; set; } = new();
    public int TotalLinesRead { get; set; }

    public static BulkImportParseResult Error(string message) =>
        new() { IsSuccess = false, Message = message };
}

/// <summary>
/// A row that failed validation during import parsing.
/// </summary>
public sealed class BulkImportRowError
{
    public int LineNumber { get; set; }
    public string RawData { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result of executing the bulk import.
/// </summary>
public sealed class BulkImportResult
{
    public bool IsSuccess { get; set; }
    public int InsertedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> DuplicateNames { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
