namespace finance_tracker_backend.Contracts.Responses;

public sealed class SoftDisconnectLinkedBankResponse
{
    public Guid LinkedBankId { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool TokenRemoved { get; init; }
}
