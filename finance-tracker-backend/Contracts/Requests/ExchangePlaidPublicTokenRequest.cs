namespace finance_tracker_backend.Contracts.Requests;

public sealed class ExchangePlaidPublicTokenRequest
{
    public string PublicToken { get; set; } = string.Empty;
    public Guid LinkSessionId { get; set; }
}
