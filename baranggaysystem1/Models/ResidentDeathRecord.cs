using System;

namespace baranggaysystem1.Models;

public sealed class ResidentDeathRecord
{
	public int DeathRecordId { get; set; }
	public int ResidentId { get; set; }
	public string ResidentName { get; set; } = string.Empty;
	public DateTime DateOfDeath { get; set; } = DateTime.Today;
	public string PlaceOfDeath { get; set; } = string.Empty;
	public string CauseOfDeath { get; set; } = string.Empty;
	public string CertificateReference { get; set; } = string.Empty;
	public string ReportedBy { get; set; } = string.Empty;
	public string Notes { get; set; } = string.Empty;
}
