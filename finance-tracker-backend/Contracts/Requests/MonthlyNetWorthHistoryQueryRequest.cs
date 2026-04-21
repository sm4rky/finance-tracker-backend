namespace finance_tracker_backend.Contracts.Requests;

public sealed class MonthlyNetWorthHistoryQueryRequest
{
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}
