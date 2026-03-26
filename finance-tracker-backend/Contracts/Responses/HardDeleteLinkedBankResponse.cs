namespace finance_tracker_backend.Contracts.Responses;

public sealed class HardDeleteLinkedBankResponse
{
    public Guid LinkedBankId { get; init; }
    public bool Deleted { get; init; }
}
