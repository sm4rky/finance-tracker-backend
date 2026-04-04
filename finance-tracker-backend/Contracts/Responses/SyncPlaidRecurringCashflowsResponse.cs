namespace finance_tracker_backend.Contracts.Responses;

public sealed class SyncPlaidRecurringCashflowsResponse
{
    public Guid LinkedBankId { get; init; }
    public DateTimeOffset SyncedAt { get; init; }
}
