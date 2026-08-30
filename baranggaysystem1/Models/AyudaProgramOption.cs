using System;

namespace baranggaysystem1.Models;

public sealed class AyudaProgramOption
{
	public int ProgramId { get; set; }

	public string ProgramName { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public decimal AllocatedBudget { get; set; }

	public decimal SpentBudget { get; set; }

	public decimal RemainingBudget { get; set; }

	public string Status { get; set; } = string.Empty;

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public string ScheduleDisplay
	{
		get
		{
			if (StartDate.HasValue && EndDate.HasValue)
			{
				return $"{StartDate:MMM d, yyyy} - {EndDate:MMM d, yyyy}";
			}
			if (StartDate.HasValue)
			{
				return $"Starts {StartDate:MMM d, yyyy}";
			}
			if (EndDate.HasValue)
			{
				return $"Until {EndDate:MMM d, yyyy}";
			}
			return "No schedule restriction";
		}
	}

	public string DisplayName
	{
		get
		{
			if (!(RemainingBudget <= 0m))
			{
				return $"{ProgramName} | PHP {RemainingBudget:N2} available";
			}
			return ProgramName + " | Budget depleted";
		}
	}

	public override string ToString()
	{
		return DisplayName;
	}
}
