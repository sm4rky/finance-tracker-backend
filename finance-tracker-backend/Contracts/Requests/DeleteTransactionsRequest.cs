namespace finance_tracker_backend.Contracts.Requests;

public sealed class DeleteTransactionsRequest
{
    public List<Guid> TransactionIds { get; set; } = [];
}
