namespace finance_tracker_backend.Contracts.Requests;

public sealed class StackedExpensesByCategoryQueryRequest : TransactionAnalyticsQueryRequest
{
    public string? TimeGranularity { get; set; }
}
