using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using baranggaysystem1.Database;

namespace baranggaysystem1;

internal sealed class EllieAssistantService
{
	private sealed class DashboardSnapshot
	{
		public int TotalResidents { get; set; }

		public int ActiveResidents { get; set; }

		public int Households { get; set; }

		public int PendingCertificates { get; set; }

		public int OngoingBlotter { get; set; }

		public int ActiveUsers { get; set; }
	}

	private readonly OllamaClient _ollamaClient;

	public EllieAssistantService(OllamaClient? ollamaClient = null)
	{
		_ollamaClient = ollamaClient ?? new OllamaClient();
	}

	public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(question))
		{
			return "Please type your question first.";
		}
		string prompt = BuildPrompt(question, await BuildSystemContextAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		try
		{
			string text = JsonUtils.TrimCodeFences(await _ollamaClient.GenerateAsync(prompt, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).Trim();
			return string.IsNullOrWhiteSpace(text) ? "I could not generate an answer right now." : text;
		}
		catch (Exception ex)
		{
			return BuildFallbackAnswer(question, ex.Message);
		}
	}

	private async Task<string> BuildSystemContextAsync(CancellationToken cancellationToken)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Available modules:");
		sb.AppendLine("- Dashboard: KPIs, officials cards, trends, announcements, projects, action center.");
		sb.AppendLine("- Residents: profile details, photo, resident list, edit/add/delete.");
		sb.AppendLine("- Blotter: file cases, case status (Ongoing/Settled/Referred), respondent and incident tracking.");
		sb.AppendLine("- Certificates: requests, approval/issuance workflow, certificate types and records.");
		sb.AppendLine("- History: activity timeline and filtering by module/date.");
		sb.AppendLine("- Reports: summary and printable reports.");
		sb.AppendLine("- Settings: sidebar behavior options.");
		DashboardSnapshot dashboardSnapshot = await LoadDashboardSnapshotAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		sb.AppendLine();
		sb.AppendLine("Current live snapshot:");
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder);
		handler.AppendLiteral("- Total residents: ");
		handler.AppendFormatted(dashboardSnapshot.TotalResidents);
		stringBuilder2.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder3 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder);
		handler.AppendLiteral("- Active residents: ");
		handler.AppendFormatted(dashboardSnapshot.ActiveResidents);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder4 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder);
		handler.AppendLiteral("- Households: ");
		handler.AppendFormatted(dashboardSnapshot.Households);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder5 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder);
		handler.AppendLiteral("- Pending certificates: ");
		handler.AppendFormatted(dashboardSnapshot.PendingCertificates);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder6 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder);
		handler.AppendLiteral("- Ongoing blotter: ");
		handler.AppendFormatted(dashboardSnapshot.OngoingBlotter);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder7 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(31, 1, stringBuilder);
		handler.AppendLiteral("- Active staff/admin accounts: ");
		handler.AppendFormatted(dashboardSnapshot.ActiveUsers);
		stringBuilder7.AppendLine(ref handler);
		string text = TryGetProjectRoot();
		if (!string.IsNullOrWhiteSpace(text))
		{
			AppendFileSnippet(sb, Path.Combine(text, "Database", "migrations", "20260211_new_schema.sql"), "Database schema snapshot");
			AppendFileSnippet(sb, Path.Combine(text, "Database", "rule", "ruletext.txt"), "System rule notes");
		}
		return sb.ToString();
	}

	private static void AppendFileSnippet(StringBuilder sb, string filePath, string title)
	{
		if (File.Exists(filePath))
		{
			string text = File.ReadAllText(filePath);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string value = ((text.Length > 4500) ? text.Substring(0, 4500) : text);
				sb.AppendLine();
				sb.AppendLine(title + ":");
				sb.AppendLine(value);
			}
		}
	}

	private async Task<DashboardSnapshot> LoadDashboardSnapshotAsync(CancellationToken cancellationToken)
	{
		DashboardSnapshot snapshot = new DashboardSnapshot
		{
			TotalResidents = await DatabaseManagerAsync.SafeScalarAsync(
				"SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0", cancellationToken).ConfigureAwait(false),
			ActiveResidents = await DatabaseManagerAsync.SafeScalarAsync(
				"SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND status = 'ACTIVE'", cancellationToken).ConfigureAwait(false),
			Households = await DatabaseManagerAsync.SafeScalarAsync(
				"SELECT COUNT(*) FROM household", cancellationToken).ConfigureAwait(false),
			PendingCertificates = await DatabaseManagerAsync.SafeScalarAsync(
				"SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED'", cancellationToken).ConfigureAwait(false),
			OngoingBlotter = await DatabaseManagerAsync.SafeScalarAsync(
				"SELECT COUNT(*) FROM case_record WHERE status = 'ONGOING'", cancellationToken).ConfigureAwait(false),
			ActiveUsers = await DatabaseManagerAsync.SafeScalarAsync(
				"SELECT COUNT(*) FROM user_account WHERE is_active = 1", cancellationToken).ConfigureAwait(false)
		};
		return snapshot;
	}

	private static string BuildPrompt(string question, string systemContext)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("You are Ellie, a local assistant inside a Barangay Management System (WinForms desktop app).");
		stringBuilder.AppendLine("Answer clearly and practically, based on the provided system context.");
		stringBuilder.AppendLine("Keep responses concise, actionable, and use numbered steps when user asks how-to.");
		stringBuilder.AppendLine("Do not invent database fields or app screens outside the context.");
		stringBuilder.AppendLine("If information is missing, say what is missing and suggest where to check in the app.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("System context:");
		stringBuilder.AppendLine(systemContext);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("User question:");
		stringBuilder.AppendLine(question.Trim());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Response:");
		return stringBuilder.ToString();
	}

	private static string BuildFallbackAnswer(string question, string reason)
	{
		return "I cannot reach the local AI right now (" + reason + "). You can still use these modules directly: Dashboard, Residents, Blotter, Certificates, History, Reports, and Settings. Question received: \"" + question.Trim() + "\".";
	}

	private static string? TryGetProjectRoot()
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(AppContext.BaseDirectory);
		for (int i = 0; i < 8; i++)
		{
			if (directoryInfo == null)
			{
				break;
			}
			if (File.Exists(Path.Combine(directoryInfo.FullName, "baranggaysystem1.csproj")))
			{
				return directoryInfo.FullName;
			}
			directoryInfo = directoryInfo.Parent;
		}
		return null;
	}
}
