namespace finance_tracker_backend.Contracts.Responses;

public sealed class StackedExpensesByPfcPrimaryResponse
{
    public string TimeGranularity { get; set; } = string.Empty;
    public IReadOnlyList<StackedExpensesByPfcPrimaryBucket> Buckets { get; set; } = [];
}

public sealed class StackedExpensesByPfcPrimaryBucket
{
    public string Period { get; set; } = string.Empty;

    public IReadOnlyList<StackedExpensesByPfcPrimaryAmount> Stacks { get; set; } = [];
}

public sealed class StackedExpensesByPfcPrimaryAmount
{
    public string? PfcPrimary { get; set; }
    public decimal Amount { get; set; }
}
