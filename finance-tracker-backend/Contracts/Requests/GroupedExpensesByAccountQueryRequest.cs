namespace finance_tracker_backend.Contracts.Requests;

public sealed class GroupedExpensesByAccountQueryRequest : TransactionAnalyticsQueryRequest
{
    public string? TimeGranularity { get; set; }
}
