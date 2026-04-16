namespace finance_tracker_backend.Contracts.Responses;

public sealed class GroupedExpensesByAccountResponse
{
    public string TimeGranularity { get; set; } = string.Empty;
    public IReadOnlyList<GroupedExpensesByAccountBucket> Buckets { get; set; } = [];
}

public sealed class GroupedExpensesByAccountBucket
{
    public string Period { get; set; } = string.Empty;
    public IReadOnlyList<GroupedExpensesByAccountBar> Bars { get; set; } = [];
}

public sealed class GroupedExpensesByAccountBar
{
    public Guid? LinkedBankAccountId { get; set; }
    public string? OfficialName { get; set; }
    public decimal Amount { get; set; }
}
