namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileBudgetResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal AmountLimit { get; init; }
    public bool IsRecurring { get; init; }
    public string? PeriodType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public Guid? ProfileCustomCategorySetId { get; init; }
    public bool IncludeIncome { get; init; }
    public bool IncludeUnlinkedTransactions { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyList<ProfileBudgetCategoryResponse> Categories { get; init; } = [];
    public IReadOnlyList<Guid> LinkedBankAccountIds { get; init; } = [];
    public ProfileBudgetPeriodResponse? CurrentPeriod { get; init; }
}

public sealed class ProfileBudgetCategoryResponse
{
    public Guid Id { get; init; }
    public string? PfcPrimaryCode { get; init; }
    public string? PfcVersion { get; init; }
    public ProfileCustomCategoryResponse? CustomCategory { get; init; }
}
