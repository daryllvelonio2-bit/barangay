using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class AyudaReleaseViewModel : ObservableObject
{
	private const int ResidentSearchLimit = 10;

	private readonly AyudaService _ayudaService = new AyudaService();

	private readonly BarangayOfficialService _barangayOfficialService = new BarangayOfficialService();

	private readonly int? _initialProgramId;

	private readonly int? _releaseId;

	private AyudaReleaseRecord? _existingRelease;

	private bool _isSynchronizingSelection;

	private CancellationTokenSource? _residentSearchCts;

	private CancellationTokenSource? _historyCheckCts;

	[ObservableProperty]
	private string _windowTitle = "Release Ayuda";

	[ObservableProperty]
	private string _windowEyebrowText = "RELEASE BARANGAY AYUDA";

	[ObservableProperty]
	private string _headerTitleText = "Create a verified assistance distribution";

	[ObservableProperty]
	private string _headerDescriptionText = "Select a funded program, verify residents, review the totals, and post one auditable distribution batch.";

	[ObservableProperty]
	private string _saveButtonText = "Post Distribution";

	[ObservableProperty]
	private string _stageButtonText = "Add Beneficiary";

	[ObservableProperty]
	private string _processingMessage = "Posting ayuda release...";

	[ObservableProperty]
	private string _beneficiarySummaryText = "No beneficiaries staged yet";

	[ObservableProperty]
	private string _workflowTitle = "1. Program and release details";

	[ObservableProperty]
	private string _workflowDescription = "Select the funded assistance program and set the distribution details.";

	[ObservableProperty]
	private bool _isProgramStep = true;

	[ObservableProperty]
	private bool _isBeneficiaryStep;

	[ObservableProperty]
	private bool _isReviewStep;

	[ObservableProperty]
	private bool _isReviewConfirmed;

	[ObservableProperty]
	private decimal _defaultAmount;

	[ObservableProperty]
	private string _beneficiaryHistoryText = "Select a resident to check previous assistance automatically.";

	[ObservableProperty]
	private bool _hasBeneficiaryHistoryWarning;

	[ObservableProperty]
	private string _reviewProgramText = "No program selected";

	[ObservableProperty]
	private string _reviewScheduleText = string.Empty;

	[ObservableProperty]
	private string _reviewBeneficiaryText = "0 beneficiaries";

	[ObservableProperty]
	private string _reviewTotalText = "PHP 0.00";

	[ObservableProperty]
	private string _reviewBalanceText = "PHP 0.00";

	[ObservableProperty]
	private string _validationMessage = string.Empty;

	[ObservableProperty]
	private bool _hasValidationMessage;

	[ObservableProperty]
	private int _programId;

	[ObservableProperty]
	private int _residentId;

	[ObservableProperty]
	private string _residentName = string.Empty;

	[ObservableProperty]
	private string _residentContactNo = string.Empty;

	[ObservableProperty]
	private decimal _amount;

	[ObservableProperty]
	private DateTime _releaseDate = DateTime.Today;

	[ObservableProperty]
	private string _referenceNo = "Reference assigned after posting";

	[ObservableProperty]
	private string _notes = "Barangay ayuda batch release";

	[ObservableProperty]
	private string _remainingBudgetText = "Select a budget program";

	[ObservableProperty]
	private string _residentSearchText = string.Empty;

	[ObservableProperty]
	private string _residentSearchStatusText = $"Showing up to {10} residents at a time to keep the list fast.";

	[ObservableProperty]
	private bool _isProcessing;

	[ObservableProperty]
	private bool _isExistingReleaseEdit;

	[ObservableProperty]
	private AyudaProgramOption? _selectedProgram;

	[ObservableProperty]
	private OfficialResidentOption? _selectedResident;

	[ObservableProperty]
	private AyudaBeneficiaryDraft? _selectedBatchItem;

	public ObservableCollection<AyudaProgramOption> ProgramOptions { get; } = new ObservableCollection<AyudaProgramOption>();

	public ObservableCollection<OfficialResidentOption> ResidentOptions { get; } = new ObservableCollection<OfficialResidentOption>();

	public ObservableCollection<AyudaBeneficiaryDraft> Beneficiaries { get; } = new ObservableCollection<AyudaBeneficiaryDraft>();

	public event Action<bool?>? CloseRequested;

	public AyudaReleaseViewModel(int? initialProgramId, int? releaseId = null)
	{
		_initialProgramId = initialProgramId;
		_releaseId = releaseId;
		ApplyModeText();
		UpdateBudgetSummary();
	}

	public AyudaReleaseViewModel()
		: this(null)
	{
	}

	public async Task InitializeAsync()
	{
		_ = 2;
		try
		{
			IsProcessing = true;
			AyudaReleaseRecord existingRelease = null;
			if (_releaseId.HasValue && _releaseId.Value > 0)
			{
				var getTask = _ayudaService.GetReleaseAsync(_releaseId.Value);
				var timeout = Task.Delay(TimeSpan.FromSeconds(8));
				if (await Task.WhenAny(getTask, timeout) == timeout || getTask.IsFaulted)
				{
					DialogService.Instance.ShowWarning("Could not load release details. The database may be unavailable.");
					this.CloseRequested?.Invoke(false);
					return;
				}
				existingRelease = getTask.Result;
				if (existingRelease == null)
				{
					DialogService.Instance.ShowWarning("The selected ayuda release could not be found.");
					this.CloseRequested?.Invoke(false);
					return;
				}
			}
			ProgramOptions.Clear();
			try
			{
				AyudaService ayudaService = _ayudaService;
				AyudaReleaseRecord ayudaReleaseRecord = existingRelease;
				var programTask = ayudaService.GetProgramOptionsAsync((ayudaReleaseRecord != null) ? new int?(ayudaReleaseRecord.ProgramId) : _initialProgramId);
				var programTimeout = Task.Delay(TimeSpan.FromSeconds(8));
				if (await Task.WhenAny(programTask, programTimeout) != programTimeout && !programTask.IsFaulted)
				{
					foreach (AyudaProgramOption item in programTask.Result.OrderBy<AyudaProgramOption, string>((AyudaProgramOption option) => option.ProgramName, StringComparer.OrdinalIgnoreCase))
					{
						ProgramOptions.Add(item);
					}
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("Failed to load ayuda program options.", ex);
			}
			try
			{
				await ReloadResidentOptionsAsync(existingRelease?.ResidentId).ConfigureAwait(continueOnCapturedContext: true);
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("Failed to load resident options for ayuda release.", ex);
			}
			if (existingRelease != null)
			{
				LoadExistingRelease(existingRelease);
			}
			else if (_initialProgramId.HasValue)
			{
				SelectedProgram = ProgramOptions.FirstOrDefault((AyudaProgramOption option) => option.ProgramId == _initialProgramId.Value);
			}
			UpdateBudgetSummary();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda release dialog failed to initialize.", ex);
		}
		finally
		{
			IsProcessing = false;
		}
	}

	[RelayCommand]
	private void ContinueToBeneficiaries()
	{
		if (!ValidateProgramDetails())
		{
			return;
		}
		ClearValidation();
		SetWorkflowStep(2);
	}

	[RelayCommand]
	private void ContinueToReview()
	{
		if (HasDraftBeneficiaryInput() && !TryStageCurrentBeneficiary(showWarnings: true))
		{
			return;
		}
		if (Beneficiaries.Count == 0)
		{
			ShowValidation("Add at least one eligible resident before reviewing this distribution.");
			return;
		}
		if (!ValidateStagedTotal())
		{
			return;
		}
		ClearValidation();
		IsReviewConfirmed = false;
		UpdateReviewSummary();
		SetWorkflowStep(3);
	}

	[RelayCommand]
	private void BackToProgram()
	{
		ClearValidation();
		SetWorkflowStep(1);
	}

	[RelayCommand]
	private void BackToBeneficiaries()
	{
		ClearValidation();
		SetWorkflowStep(2);
	}

	[RelayCommand]
	private void Cancel()
	{
		this.CloseRequested?.Invoke(false);
	}

	[RelayCommand]
	private void ApplyDefaultAmount()
	{
		if (DefaultAmount <= 0m)
		{
			ShowValidation("Enter a default amount greater than zero first.");
			return;
		}
		decimal normalizedAmount = decimal.Round(DefaultAmount, 2, MidpointRounding.AwayFromZero);
		foreach (AyudaBeneficiaryDraft beneficiary in Beneficiaries)
		{
			beneficiary.Amount = normalizedAmount;
		}
		if (SelectedResident != null)
		{
			Amount = normalizedAmount;
		}
		ClearValidation();
		UpdateBudgetSummary();
	}

	[RelayCommand]
	private void StageBeneficiary()
	{
		if (TryStageCurrentBeneficiary(showWarnings: true))
		{
			ClearValidation();
		}
	}

	[RelayCommand]
	private void ClearBeneficiaryEntry()
	{
		SelectedBatchItem = null;
		ClearCurrentEntry(resetSelection: true);
		RefreshStageButtonText();
		UpdateBudgetSummary();
	}

	[RelayCommand]
	private void RemoveSelectedBeneficiary()
	{
		if (IsExistingReleaseEdit)
		{
			DialogService.Instance.ShowWarning("Use Cancel Release from the Ayuda page if this saved release should no longer count against the budget.");
			return;
		}
		if (SelectedBatchItem == null)
		{
			DialogService.Instance.ShowWarning("Select a staged beneficiary first.");
			return;
		}
		Beneficiaries.Remove(SelectedBatchItem);
		SelectedBatchItem = null;
		ClearCurrentEntry(resetSelection: true);
		RefreshStageButtonText();
		UpdateBudgetSummary();
		ClearValidation();
	}

	[RelayCommand]
	private async Task SaveRelease()
	{
		if (!ValidateProgramDetails())
		{
			return;
		}
		if (HasDraftBeneficiaryInput() && !TryStageCurrentBeneficiary(showWarnings: true))
		{
			return;
		}
		if (Beneficiaries.Count == 0)
		{
			ShowValidation("Add at least one beneficiary before posting ayuda.");
			return;
		}
		if (!ValidateStagedTotal())
		{
			return;
		}
		if (!IsReviewStep)
		{
			UpdateReviewSummary();
			SetWorkflowStep(3);
			ShowValidation("Review the distribution summary before posting it.");
			return;
		}
		if (!IsReviewConfirmed)
		{
			ShowValidation("Confirm that the beneficiary list and amounts have been verified.");
			return;
		}
		UpdateReviewSummary();
		string confirmationMessage = IsExistingReleaseEdit
			? $"Save the revised release for {Beneficiaries[0].ResidentName} in the amount of {ReviewTotalText}?"
			: $"Post {ReviewTotalText} to {Beneficiaries.Count:N0} beneficiary(ies) under {SelectedProgram?.ProgramName}?\n\nThis action will deduct the amount from the program budget and create an audit record.";
		if (!DialogService.Instance.Confirm(confirmationMessage, IsExistingReleaseEdit ? "Confirm Release Changes" : "Confirm Ayuda Distribution"))
		{
			return;
		}
		try
		{
			ClearValidation();
			IsProcessing = true;
			if (IsExistingReleaseEdit)
			{
				AyudaBeneficiaryDraft ayudaBeneficiaryDraft = Beneficiaries[0];
				await _ayudaService.UpdateReleaseAsync(new AyudaReleaseRecord
				{
					ReleaseId = _releaseId.GetValueOrDefault(),
					ProgramId = ProgramId,
					ResidentId = ayudaBeneficiaryDraft.ResidentId,
					ResidentName = ayudaBeneficiaryDraft.ResidentName,
					Amount = ayudaBeneficiaryDraft.Amount,
					ReleasedAt = ReleaseDate,
					Notes = Notes
				});
				DialogService.Instance.ShowInfo("Ayuda release updated successfully.");
				this.CloseRequested?.Invoke(true);
			}
			else
			{
				AyudaBatchReleaseResult result = await _ayudaService.SaveBatchReleaseAsync(ProgramId, ReleaseDate, Notes, Beneficiaries.ToList());
				ReferenceNo = result.BatchReference;
				if (!string.IsNullOrWhiteSpace(result.ReportFilePath))
				{
					AyudaReleaseReportService.TryOpenGeneratedFile(result.ReportFilePath);
				}
				string message = $"Ayuda distribution posted successfully.\n\nBeneficiaries: {result.BeneficiaryCount:N0}\nTotal: PHP {result.TotalAmount:N2}\nBatch Reference: {result.BatchReference}";
				if (!string.IsNullOrWhiteSpace(result.ReportFilePath))
				{
					message += "\nThe signed distribution report has been generated.";
				}
				DialogService.Instance.ShowInfo(message);
				this.CloseRequested?.Invoke(true);
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda release save failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Ayuda Release");
		}
		finally
		{
			IsProcessing = false;
		}
	}

	private void LoadExistingRelease(AyudaReleaseRecord existingRelease)
	{
		_existingRelease = existingRelease;
		IsExistingReleaseEdit = true;
		ReleaseDate = existingRelease.ReleasedAt;
		ReferenceNo = existingRelease.ReferenceNo;
		Notes = existingRelease.Notes;
		SelectedProgram = ProgramOptions.FirstOrDefault((AyudaProgramOption option) => option.ProgramId == existingRelease.ProgramId);
		Beneficiaries.Clear();
		AyudaBeneficiaryDraft ayudaBeneficiaryDraft = new AyudaBeneficiaryDraft
		{
			PersistedReleaseId = existingRelease.ReleaseId,
			ResidentId = existingRelease.ResidentId,
			ResidentName = existingRelease.ResidentName,
			ContactNo = existingRelease.ResidentContactNo,
			Amount = existingRelease.Amount
		};
		Beneficiaries.Add(ayudaBeneficiaryDraft);
		SelectedBatchItem = ayudaBeneficiaryDraft;
	}

	private bool TryStageCurrentBeneficiary(bool showWarnings)
	{
		if (SelectedResident == null || ResidentId <= 0)
		{
			if (showWarnings)
			{
				DialogService.Instance.ShowWarning("Select a resident beneficiary first.");
			}
			return false;
		}
		if (Amount <= 0m)
		{
			if (showWarnings)
			{
				DialogService.Instance.ShowWarning("Release amount must be greater than zero.");
			}
			return false;
		}
		if (Beneficiaries.FirstOrDefault((AyudaBeneficiaryDraft item) => item != SelectedBatchItem && item.ResidentId == ResidentId) != null)
		{
			if (showWarnings)
			{
				DialogService.Instance.ShowWarning("This resident is already in the staged beneficiary list.");
			}
			return false;
		}
		decimal proposedTotal = Beneficiaries.Where(item => item != SelectedBatchItem).Sum(item => item.Amount) + Amount;
		decimal availableBudget = GetEffectiveAvailableBudget();
		if (proposedTotal > availableBudget)
		{
			if (showWarnings)
			{
				DialogService.Instance.ShowWarning($"This entry would exceed the available budget by PHP {proposedTotal - availableBudget:N2}.");
			}
			return false;
		}
		AyudaBeneficiaryDraft ayudaBeneficiaryDraft = SelectedBatchItem;
		if (IsExistingReleaseEdit && ayudaBeneficiaryDraft == null)
		{
			ayudaBeneficiaryDraft = Beneficiaries.FirstOrDefault();
		}
		if (ayudaBeneficiaryDraft == null)
		{
			ayudaBeneficiaryDraft = new AyudaBeneficiaryDraft();
			Beneficiaries.Add(ayudaBeneficiaryDraft);
		}
		ayudaBeneficiaryDraft.ResidentId = ResidentId;
		ayudaBeneficiaryDraft.ResidentName = ResidentName;
		ayudaBeneficiaryDraft.ContactNo = ResidentContactNo;
		ayudaBeneficiaryDraft.Amount = decimal.Round(Amount, 2, MidpointRounding.AwayFromZero);
		SelectedBatchItem = ayudaBeneficiaryDraft;
		if (!IsExistingReleaseEdit)
		{
			ClearCurrentEntry(resetSelection: true);
			SelectedBatchItem = null;
		}
		RefreshStageButtonText();
		UpdateBudgetSummary();
		return true;
	}

	private bool HasDraftBeneficiaryInput()
	{
		if (ResidentId <= 0 && SelectedResident == null)
		{
			return Amount > 0m;
		}
		return true;
	}

	private void ClearCurrentEntry(bool resetSelection)
	{
		if (resetSelection)
		{
			_isSynchronizingSelection = true;
			try
			{
				SelectedResident = null;
			}
			finally
			{
				_isSynchronizingSelection = false;
			}
		}
		ResidentId = 0;
		ResidentName = string.Empty;
		ResidentContactNo = string.Empty;
		Amount = 0m;
		_historyCheckCts?.Cancel();
		BeneficiaryHistoryText = "Select a resident to check previous assistance automatically.";
		HasBeneficiaryHistoryWarning = false;
	}

	partial void OnResidentSearchTextChanged(string value)
	{
		ScheduleResidentSearch();
	}

	partial void OnDefaultAmountChanged(decimal value)
	{
		if (SelectedResident != null && Amount <= 0m && value > 0m)
		{
			Amount = value;
		}
	}

	partial void OnReleaseDateChanged(DateTime value)
	{
		UpdateReviewSummary();
	}

	partial void OnSelectedResidentChanged(OfficialResidentOption? value)
	{
		if (_isSynchronizingSelection) return;
		if (value != null && value.ResidentId > 0)
		{
			ResidentId = value.ResidentId;
			ResidentName = value.FullName ?? string.Empty;
			ResidentContactNo = value.ContactNo ?? string.Empty;
			if (Amount <= 0m && DefaultAmount > 0m)
			{
				Amount = DefaultAmount;
			}
			ScheduleResidentHistoryCheck();
		}
		else
		{
			BeneficiaryHistoryText = "Select a resident to check previous assistance automatically.";
			HasBeneficiaryHistoryWarning = false;
		}
	}

	partial void OnSelectedProgramChanged(AyudaProgramOption? value)
	{
		if (value != null)
		{
			ProgramId = value.ProgramId;
		}
		else
		{
			ProgramId = 0;
		}
		if (ResidentId > 0)
		{
			ScheduleResidentHistoryCheck();
		}
		UpdateBudgetSummary();
	}

	private void UpdateBudgetSummary()
	{
		decimal num = GetEffectiveAvailableBudget();
		decimal num2 = Beneficiaries.Sum((AyudaBeneficiaryDraft item) => item.Amount);
		decimal value = Math.Max(num - num2, 0m);
		RemainingBudgetText = ((SelectedProgram == null) ? "Select a budget program" : $"PHP {num:N2} available | Staged PHP {num2:N2} | After save PHP {value:N2}");
		BeneficiarySummaryText = ((Beneficiaries.Count == 0) ? "No beneficiaries staged yet" : $"{Beneficiaries.Count:N0} beneficiary(ies) staged | Total PHP {num2:N2}");
		UpdateReviewSummary();
	}

	private decimal GetEffectiveAvailableBudget()
	{
		decimal available = SelectedProgram?.RemainingBudget ?? 0m;
		if (_existingRelease != null && _existingRelease.ProgramId == ProgramId && !string.Equals(_existingRelease.ReleaseStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
		{
			available += _existingRelease.Amount;
		}
		return available;
	}

	private bool ValidateProgramDetails()
	{
		if (SelectedProgram == null || ProgramId <= 0)
		{
			ShowValidation("Select an active ayuda program with an available budget.");
			return false;
		}
		if (!string.Equals(SelectedProgram.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
		{
			ShowValidation("The selected program is not active and cannot release assistance.");
			return false;
		}
		if (ReleaseDate.Date > DateTime.Today)
		{
			ShowValidation("Release date cannot be in the future.");
			return false;
		}
		if (SelectedProgram.StartDate.HasValue && ReleaseDate.Date < SelectedProgram.StartDate.Value.Date)
		{
			ShowValidation($"Release date must be on or after {SelectedProgram.StartDate:MMM d, yyyy}.");
			return false;
		}
		if (SelectedProgram.EndDate.HasValue && ReleaseDate.Date > SelectedProgram.EndDate.Value.Date)
		{
			ShowValidation($"This program ended on {SelectedProgram.EndDate:MMM d, yyyy}. Update the program schedule before posting.");
			return false;
		}
		return true;
	}

	private bool ValidateStagedTotal()
	{
		if (Beneficiaries.Any(item => item.Amount <= 0m))
		{
			ShowValidation("Every staged beneficiary must have an amount greater than zero.");
			return false;
		}
		decimal stagedTotal = Beneficiaries.Sum(item => item.Amount);
		decimal available = GetEffectiveAvailableBudget();
		if (stagedTotal > available)
		{
			ShowValidation($"The staged total exceeds the available budget by PHP {stagedTotal - available:N2}.");
			return false;
		}
		return true;
	}

	private void SetWorkflowStep(int step)
	{
		IsProgramStep = step == 1;
		IsBeneficiaryStep = step == 2;
		IsReviewStep = step == 3;
		(WorkflowTitle, WorkflowDescription) = step switch
		{
			1 => ("1. Program and release details", "Select the funded assistance program and set the distribution details."),
			2 => ("2. Verify and stage beneficiaries", "Search registered residents, review previous assistance, and add verified amounts."),
			_ => ("3. Review and post distribution", "Check the final totals and beneficiary list before committing the batch.")
		};
	}

	private void UpdateReviewSummary()
	{
		decimal total = Beneficiaries.Sum(item => item.Amount);
		decimal afterSave = Math.Max(GetEffectiveAvailableBudget() - total, 0m);
		ReviewProgramText = SelectedProgram == null ? "No program selected" : $"{SelectedProgram.ProgramName} · {SelectedProgram.Category}";
		ReviewScheduleText = SelectedProgram == null ? string.Empty : $"Release {ReleaseDate:MMM d, yyyy} · {SelectedProgram.ScheduleDisplay}";
		ReviewBeneficiaryText = $"{Beneficiaries.Count:N0} beneficiary(ies)";
		ReviewTotalText = $"PHP {total:N2}";
		ReviewBalanceText = $"PHP {afterSave:N2}";
	}

	private void ShowValidation(string message)
	{
		ValidationMessage = message;
		HasValidationMessage = true;
	}

	private void ClearValidation()
	{
		ValidationMessage = string.Empty;
		HasValidationMessage = false;
	}

	private void ApplyModeText()
	{
		if (_releaseId.HasValue && _releaseId.Value > 0)
		{
			WindowTitle = "Edit Ayuda Release";
			WindowEyebrowText = "UPDATE AYUDA RELEASE";
			HeaderTitleText = "Revise the selected ayuda release";
			HeaderDescriptionText = "Update the beneficiary, amount, release date, notes, or target program for the selected ayuda release.";
			SaveButtonText = "Save Release Changes";
			StageButtonText = "Update Beneficiary";
			ProcessingMessage = "Saving ayuda release...";
			Notes = string.Empty;
		}
	}

	private void RefreshStageButtonText()
	{
		if (IsExistingReleaseEdit)
		{
			StageButtonText = "Update Beneficiary";
		}
		else
		{
			StageButtonText = ((SelectedBatchItem == null) ? "Add Beneficiary" : "Update Beneficiary");
		}
	}

	private void ScheduleResidentHistoryCheck()
	{
		_historyCheckCts?.Cancel();
		_historyCheckCts?.Dispose();
		if (ResidentId <= 0)
		{
			BeneficiaryHistoryText = "Select a resident to check previous assistance automatically.";
			HasBeneficiaryHistoryWarning = false;
			return;
		}
		CancellationTokenSource cancellationTokenSource = (_historyCheckCts = new CancellationTokenSource());
		_ = LoadResidentHistoryAsync(ResidentId, ProgramId, cancellationTokenSource.Token);
	}

	private async Task LoadResidentHistoryAsync(int residentId, int programId, CancellationToken cancellationToken)
	{
		try
		{
			BeneficiaryHistoryText = "Checking previous assistance records...";
			AyudaResidentAssistanceSummary summary = await _ayudaService.GetResidentAssistanceSummaryAsync(residentId, programId).ConfigureAwait(continueOnCapturedContext: true);
			if (cancellationToken.IsCancellationRequested || residentId != ResidentId || programId != ProgramId)
			{
				return;
			}
			HasBeneficiaryHistoryWarning = summary.ProgramReleaseCount > 0 || summary.RecentHouseholdReleaseCount > 0;
			if (summary.ReleaseCount == 0 && summary.RecentHouseholdReleaseCount == 0)
			{
				BeneficiaryHistoryText = "No previous released ayuda found for this resident.";
				return;
			}
			List<string> details = new List<string>();
			if (summary.ProgramReleaseCount > 0)
			{
				details.Add($"{summary.ProgramReleaseCount:N0} prior release(s) from this program totaling PHP {summary.ProgramTotalAmount:N2}; last {summary.LastProgramReleaseAt:MMM d, yyyy}");
			}
			else if (summary.ReleaseCount > 0)
			{
				details.Add($"{summary.ReleaseCount:N0} prior ayuda release(s) totaling PHP {summary.TotalAmount:N2}; last {summary.LastReleaseAt:MMM d, yyyy}");
			}
			if (summary.RecentHouseholdReleaseCount > 0)
			{
				details.Add($"{summary.RecentHouseholdReleaseCount:N0} release(s) to other household members within 90 days");
			}
			BeneficiaryHistoryText = string.Join(". ", details) + ". Verify eligibility before staging.";
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Ayuda beneficiary history check failed.", ex);
			if (!cancellationToken.IsCancellationRequested)
			{
				BeneficiaryHistoryText = "Previous assistance could not be checked. Verify manually before staging.";
				HasBeneficiaryHistoryWarning = true;
			}
		}
	}

	private void ScheduleResidentSearch()
	{
		_residentSearchCts?.Cancel();
		_residentSearchCts?.Dispose();
		CancellationTokenSource cancellationTokenSource = (_residentSearchCts = new CancellationTokenSource());
		string residentSearchText = ResidentSearchText;
		int? preferredResidentId = ((ResidentId > 0) ? new int?(ResidentId) : SelectedResident?.ResidentId);
		RunResidentSearchAsync(residentSearchText, preferredResidentId, cancellationTokenSource.Token);
	}

	private async Task RunResidentSearchAsync(string searchText, int? preferredResidentId, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			await Task.Delay(250, cancellationToken).ConfigureAwait(continueOnCapturedContext: true);
			IReadOnlyList<OfficialResidentOption> residents = await _barangayOfficialService.SearchResidentOptionsAsync(searchText, 10, preferredResidentId).ConfigureAwait(continueOnCapturedContext: true);
			if (!cancellationToken.IsCancellationRequested && string.Equals(searchText, ResidentSearchText, StringComparison.Ordinal))
			{
				ApplyResidentOptions(residents);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			AppLogger.LogError("Ayuda resident search failed.", ex2);
			ResidentSearchStatusText = "Could not load residents. Try searching again.";
		}
	}

	private async Task ReloadResidentOptionsAsync(int? preferredResidentId = null)
	{
		ApplyResidentOptions(await _barangayOfficialService.SearchResidentOptionsAsync(ResidentSearchText, 10, preferredResidentId).ConfigureAwait(continueOnCapturedContext: true));
	}

	private void ApplyResidentOptions(IReadOnlyList<OfficialResidentOption> residents)
	{
		ResidentOptions.Clear();
		foreach (OfficialResidentOption resident in residents)
		{
			ResidentOptions.Add(resident);
		}
		EnsureResidentOptionAvailable(ResidentId, ResidentName, ResidentContactNo);
		RebindSelectedResident();
		UpdateResidentSearchStatus(residents.Count);
	}

	private void EnsureResidentOptionAvailable(int residentId, string residentName, string contactNo)
	{
		if (residentId <= 0 || string.IsNullOrWhiteSpace(residentName))
		{
			return;
		}
		OfficialResidentOption officialResidentOption = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == residentId);
		if (officialResidentOption != null)
		{
			officialResidentOption.FullName = residentName;
			officialResidentOption.ContactNo = contactNo;
			return;
		}
		ResidentOptions.Insert(0, new OfficialResidentOption
		{
			ResidentId = residentId,
			FullName = residentName,
			ContactNo = contactNo
		});
		while (ResidentOptions.Count > 10)
		{
			ResidentOptions.RemoveAt(ResidentOptions.Count - 1);
		}
	}

	private void RebindSelectedResident()
	{
		if (ResidentId <= 0)
		{
			return;
		}
		OfficialResidentOption officialResidentOption = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == ResidentId);
		if (officialResidentOption == null)
		{
			return;
		}
		_isSynchronizingSelection = true;
		try
		{
			SelectedResident = officialResidentOption;
		}
		finally
		{
			_isSynchronizingSelection = false;
		}
	}

	private void UpdateResidentSearchStatus(int resultCount)
	{
		if (resultCount <= 0)
		{
			ResidentSearchStatusText = (string.IsNullOrWhiteSpace(ResidentSearchText) ? "No active residents are available for this barangay yet." : "No matching residents found. Try a different name or contact number.");
		}
		else if (string.IsNullOrWhiteSpace(ResidentSearchText))
		{
			ResidentSearchStatusText = ((resultCount >= 10) ? $"Showing the first {10} active residents. Search by name or contact number to narrow the list." : $"Showing {resultCount} active resident(s).");
		}
		else
		{
			ResidentSearchStatusText = ((resultCount >= 10) ? $"Showing the first {10} matching residents." : $"Showing {resultCount} matching resident(s).");
		}
	}

	partial void OnSelectedBatchItemChanged(AyudaBeneficiaryDraft? value)
	{
		if (value == null)
		{
			return;
		}
		_isSynchronizingSelection = true;
		try
		{
			EnsureResidentOptionAvailable(value.ResidentId, value.ResidentName, value.ContactNo);
			SelectedResident = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == value.ResidentId);
			ResidentId = value.ResidentId;
			ResidentName = value.ResidentName;
			ResidentContactNo = value.ContactNo;
			Amount = value.Amount;
		}
		finally
		{
			_isSynchronizingSelection = false;
		}
		ScheduleResidentHistoryCheck();
	}
}
