namespace finance_tracker_backend.Contracts.Responses;

public sealed class SubscriptionPlanResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? MaxAccounts { get; init; }
    public int? HistoryMonths { get; init; }
    public bool HasAi { get; init; }
    public decimal? MonthlyPrice { get; init; }
}
