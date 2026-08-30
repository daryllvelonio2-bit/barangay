using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class AttachmentService
{
	private const int MaxAttachmentBytes = 20971520;

	public static IReadOnlyList<AttachmentListItem> LoadList(AttachmentEntityType entityType, int entityId)
	{
		if (entityId <= 0)
		{
			return Array.Empty<AttachmentListItem>();
		}
		List<AttachmentListItem> list = new List<AttachmentListItem>();
		DataTable table = DbHelper.LoadTable("SELECT a.attachment_id,\n                     a.file_name,\n                     a.mime_type,\n                     a.file_size_bytes,\n                     a.notes,\n                     a.uploaded_at,\n                     COALESCE(ua.username, CONCAT('User #', a.uploaded_by_user_id)) AS uploaded_by\n              FROM record_attachment a\n              LEFT JOIN user_account ua ON ua.user_id = a.uploaded_by_user_id\n              WHERE a.entity_type = @entityType\n                AND a.entity_id = @entityId\n              ORDER BY a.uploaded_at DESC, a.attachment_id DESC", cmd =>
		{
			cmd.Parameters.AddWithValue("@entityType", ToDbEntityType(entityType));
			cmd.Parameters.AddWithValue("@entityId", entityId);
		});
		foreach (DataRow row in table.Rows)
		{
			list.Add(new AttachmentListItem
			{
				AttachmentId = Convert.ToInt64(row["attachment_id"]),
				FileName = Convert.ToString(row["file_name"]) ?? string.Empty,
				MimeType = Convert.ToString(row["mime_type"]) ?? string.Empty,
				FileSizeBytes = row["file_size_bytes"] == DBNull.Value ? 0 : Convert.ToInt64(row["file_size_bytes"]),
				Notes = Convert.ToString(row["notes"]) ?? string.Empty,
				UploadedBy = Convert.ToString(row["uploaded_by"]) ?? string.Empty,
				UploadedAt = row["uploaded_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["uploaded_at"])
			});
		}
		return list;
	}

	public static long AddFromFile(AttachmentEntityType entityType, int entityId, string filePath, string? notes)
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		if (!Permissions.CanManageAttachments)
		{
			throw new UnauthorizedAccessException("You do not have permission to manage attachments.");
		}
		if (entityId <= 0)
		{
			throw new InvalidOperationException("A record must be selected before attaching a file.");
		}
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new InvalidOperationException("Attachment file path is required.");
		}
		FileInfo fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists)
		{
			throw new FileNotFoundException("Attachment file was not found.", filePath);
		}
		if (fileInfo.Length <= 0)
		{
			throw new InvalidOperationException("Attachment file is empty.");
		}
		if (fileInfo.Length > 20971520)
		{
			throw new InvalidOperationException("Attachment is too large. Maximum allowed size is 20 MB.");
		}
		byte[] array = File.ReadAllBytes(filePath);
		string name = fileInfo.Name;
		string text = fileInfo.Extension?.Trim().TrimStart('.').ToLowerInvariant() ?? string.Empty;
		string text2 = GuessMimeType(text);
		string text3 = ComputeSha256(array);
		DbHelper.ExecuteNonQuery("INSERT INTO record_attachment\n                (entity_type, entity_id, file_name, file_ext, mime_type, file_size_bytes, file_hash, file_blob, notes, uploaded_by_user_id, uploaded_at)\n              VALUES\n                (@entityType, @entityId, @fileName, @fileExt, @mimeType, @sizeBytes, @hash, @blob, @notes, @uploadedBy, NOW())", cmd =>
		{
			cmd.Parameters.AddWithValue("@entityType", ToDbEntityType(entityType));
			cmd.Parameters.AddWithValue("@entityId", entityId);
			cmd.Parameters.AddWithValue("@fileName", name);
			cmd.Parameters.AddWithValue("@fileExt", string.IsNullOrWhiteSpace(text) ? DBNull.Value : text);
			cmd.Parameters.AddWithValue("@mimeType", string.IsNullOrWhiteSpace(text2) ? DBNull.Value : text2);
			cmd.Parameters.AddWithValue("@sizeBytes", array.LongLength);
			cmd.Parameters.AddWithValue("@hash", text3);
			cmd.Parameters.AddWithValue("@blob", array);
			cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim());
			cmd.Parameters.AddWithValue("@uploadedBy", UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value);
		});
		long attachmentId = DbHelper.ExecuteScalar<long>(
			"SELECT attachment_id FROM record_attachment WHERE entity_type = @entityType AND entity_id = @entityId AND file_hash = @hash ORDER BY attachment_id DESC LIMIT 1",
			cmd =>
			{
				cmd.Parameters.AddWithValue("@entityType", ToDbEntityType(entityType));
				cmd.Parameters.AddWithValue("@entityId", entityId);
				cmd.Parameters.AddWithValue("@hash", text3);
			});
		AuditTrailService.Log("Attachments", "record_attachment", attachmentId, "CREATE", null,
			new { EntityType = ToDbEntityType(entityType), EntityId = entityId, FileName = name, Size = array.LongLength },
			"Attachment added.");
		return attachmentId;
	}

	public static AttachmentContent? LoadContent(long attachmentId)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		if (attachmentId <= 0)
		{
			return null;
		}
		DataTable table = DbHelper.LoadTable("SELECT attachment_id, file_name, mime_type, file_blob\n              FROM record_attachment\n              WHERE attachment_id = @id\n              LIMIT 1", cmd =>
		{
			cmd.Parameters.AddWithValue("@id", attachmentId);
		});
		if (table.Rows.Count == 0)
			return null;
		DataRow row = table.Rows[0];
		return new AttachmentContent
		{
			AttachmentId = Convert.ToInt64(row["attachment_id"]),
			FileName = Convert.ToString(row["file_name"]) ?? "attachment.bin",
			MimeType = Convert.ToString(row["mime_type"]) ?? string.Empty,
			Content = row["file_blob"] as byte[] ?? Array.Empty<byte>()
		};
	}

	public static void DeleteAttachment(long attachmentId)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		if (!Permissions.CanManageAttachments)
		{
			throw new UnauthorizedAccessException("You do not have permission to delete attachments.");
		}
		if (attachmentId <= 0)
		{
			return;
		}
		int affected = DbHelper.ExecuteNonQuery(
			"DELETE FROM record_attachment WHERE attachment_id = @id",
			cmd => cmd.Parameters.AddWithValue("@id", attachmentId));
		if (affected > 0)
		{
			AuditTrailService.Log("Attachments", "record_attachment", attachmentId, "DELETE",
				null, null, "Attachment deleted.");
		}
	}

	public static string GetEntityDisplayName(AttachmentEntityType entityType)
	{
		return entityType switch
		{
			AttachmentEntityType.Resident => "Resident", 
			AttachmentEntityType.Case => "Blotter Case", 
			AttachmentEntityType.Certificate => "Certificate", 
			_ => "Record", 
		};
	}

	private static string ToDbEntityType(AttachmentEntityType entityType)
	{
		return entityType switch
		{
			AttachmentEntityType.Resident => "RESIDENT", 
			AttachmentEntityType.Case => "CASE", 
			AttachmentEntityType.Certificate => "CERTIFICATE", 
			_ => "RESIDENT", 
		};
	}

	private static string ComputeSha256(byte[] bytes)
	{
		using SHA256 sHA = SHA256.Create();
		return Convert.ToHexString(sHA.ComputeHash(bytes)).ToLowerInvariant();
	}

	private static string GuessMimeType(string fileExt)
	{
		return fileExt switch
		{
			"pdf" => "application/pdf", 
			"jpg" => "image/jpeg", 
			"jpeg" => "image/jpeg", 
			"png" => "image/png", 
			"gif" => "image/gif", 
			"txt" => "text/plain", 
			"doc" => "application/msword", 
			"docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
			"xls" => "application/vnd.ms-excel", 
			"xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
			"ppt" => "application/vnd.ms-powerpoint", 
			"pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation", 
			_ => "application/octet-stream", 
		};
	}
}
