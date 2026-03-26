namespace finance_tracker_backend.Contracts.Responses;

public sealed class CreatePlaidLinkTokenResponse
{
    public string LinkToken { get; init; } = string.Empty;
    public Guid LinkSessionId { get; init; }
}
