namespace finance_tracker_backend.Contracts.Requests;

public sealed class StackedExpensesByPfcPrimaryQueryRequest : TransactionAnalyticsQueryRequest
{
    public string? TimeGranularity { get; set; }
}
