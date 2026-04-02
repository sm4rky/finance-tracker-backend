namespace finance_tracker_backend.Contracts.Responses;

public sealed class ExchangePlaidPublicTokenResponse
{
    public Guid LinkedBankId { get; init; }
    public string PlaidItemId { get; init; } = string.Empty;
    public string? InstitutionId { get; init; }
    public string? InstitutionName { get; init; }
    public IReadOnlyList<LinkedBankAccountResponse> Accounts { get; init; } = [];
    public bool RequiresAccountOptOutHandling { get; init; }
    public IReadOnlyList<LinkedBankAccountResponse> PendingDeselectedAccounts { get; init; } = [];
}
