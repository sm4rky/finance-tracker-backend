namespace finance_tracker_backend.Contracts.Requests;

public sealed class SyncPlaidRecurringCashflowsRequest
{
    public Guid LinkedBankId { get; set; }
}
