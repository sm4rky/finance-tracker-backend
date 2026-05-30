namespace finance_tracker_backend.Contracts.Responses;

public sealed class PushSubscriptionResponse
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
