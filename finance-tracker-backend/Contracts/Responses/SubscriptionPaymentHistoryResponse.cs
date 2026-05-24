namespace finance_tracker_backend.Contracts.Responses;

public sealed class SubscriptionPaymentHistoryResponse
{
    public Guid Id { get; init; }
    public Guid? ProfileId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? FromPlanId { get; init; }
    public string? ToPlanId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset EffectiveAt { get; init; }
}
