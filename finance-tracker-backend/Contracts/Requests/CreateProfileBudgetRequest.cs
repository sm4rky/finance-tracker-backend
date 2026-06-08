namespace finance_tracker_backend.Contracts.Requests;

public sealed class CreateProfileBudgetRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal AmountLimit { get; set; }
    public bool IsRecurring { get; set; }
    public string? PeriodType { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public Guid? ProfileCustomCategorySetId { get; set; }
    public bool IncludeIncome { get; set; }
    public bool IncludeUnlinkedTransactions { get; set; } = true;
    public IReadOnlyList<CreateProfileBudgetCategoryRequest> Categories { get; set; } = [];
    public IReadOnlyList<Guid> LinkedBankAccountIds { get; set; } = [];
}

public sealed class CreateProfileBudgetCategoryRequest
{
    public string? PfcPrimaryCode { get; set; }
    public string? PfcVersion { get; set; }
    public Guid? CustomCategoryId { get; set; }
}
