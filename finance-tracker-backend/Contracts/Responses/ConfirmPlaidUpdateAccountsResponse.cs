namespace finance_tracker_backend.Contracts.Responses;

public sealed class ConfirmPlaidUpdateAccountsResponse
{
    public Guid LinkedBankId { get; init; }
    public SyncPlaidTransactionsResponse Sync { get; init; } = null!;
}
