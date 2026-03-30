namespace finance_tracker_backend.Contracts.Responses;

public sealed class SyncPlaidTransactionsResponse
{
    public Guid LinkedBankId { get; init; }
    public string? TransactionsUpdateStatus { get; init; }
    public DateTimeOffset SyncedAt { get; init; }
}
