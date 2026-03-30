namespace finance_tracker_backend.Contracts.Requests;

public sealed class QueryTransactionsRequest
{
    public int? Page { get; set; }
    public int? Limit { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}
