namespace finance_tracker_backend.Contracts.Responses;

public sealed class SubscriptionPaymentResponse
{
    public Guid Id { get; init; }
    public Guid? ProfileId { get; init; }
    public decimal Amount { get; init; }
    public DateTimeOffset ChargedAt { get; init; }
    public string PlanId { get; init; } = string.Empty;
    public string PaymentType { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
