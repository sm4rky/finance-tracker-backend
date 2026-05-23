namespace finance_tracker_backend.Contracts.Responses;

public sealed class TransactionLinkedBankAccountResponse
{
    public Guid Id { get; init; }
    public string? AccountName { get; init; }
    public string? OfficialName { get; init; }
    public string? Mask { get; init; }
    public string? Type { get; init; }
    public string? Subtype { get; init; }
}
