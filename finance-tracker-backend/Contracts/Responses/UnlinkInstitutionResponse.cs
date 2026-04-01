namespace finance_tracker_backend.Contracts.Responses;

public sealed class UnlinkInstitutionResponse
{
    public Guid LinkedBankId { get; init; }
    public bool Deleted { get; init; }
    public int TransactionsRemoved { get; init; }
}
