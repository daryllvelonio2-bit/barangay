using System;

namespace baranggaysystem1.Models;

public sealed class AyudaResidentAssistanceSummary
{
	public int ReleaseCount { get; set; }

	public decimal TotalAmount { get; set; }

	public DateTime? LastReleaseAt { get; set; }

	public int ProgramReleaseCount { get; set; }

	public decimal ProgramTotalAmount { get; set; }

	public DateTime? LastProgramReleaseAt { get; set; }

	public int RecentHouseholdReleaseCount { get; set; }
}
