namespace finance_tracker_backend.Contracts.Responses;

public sealed class MonthlyNetWorthHistoryResponse
{
    public IReadOnlyList<MonthlyNetWorthHistoryItemResponse> Items { get; set; } = [];
}

public sealed class MonthlyNetWorthHistoryItemResponse
{
    public DateOnly PeriodStartDate { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
