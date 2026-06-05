namespace finance_tracker_backend.Contracts.Responses;

public sealed class StackedExpensesByCategoryResponse
{
    public string TimeGranularity { get; set; } = string.Empty;
    public IReadOnlyList<StackedExpensesByCategoryBucket> Buckets { get; set; } = [];
}

public sealed class StackedExpensesByCategoryBucket
{
    public string Period { get; set; } = string.Empty;

    public IReadOnlyList<StackedExpensesByCategoryAmount> Stacks { get; set; } = [];
}

public sealed class StackedExpensesByCategoryAmount
{
    public string? PfcPrimary { get; set; }
    public ProfileCustomCategoryResponse? CustomCategory { get; set; }
    public decimal Amount { get; set; }
}
