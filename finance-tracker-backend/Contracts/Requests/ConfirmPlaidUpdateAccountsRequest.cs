namespace finance_tracker_backend.Contracts.Requests;

public sealed class ConfirmPlaidUpdateAccountsRequest
{
    public IReadOnlyList<PlaidAccountOptOutDecision> Decisions { get; set; } = [];
}

public sealed class PlaidAccountOptOutDecision
{
    public string PlaidAccountId { get; set; } = string.Empty;
    public bool DeleteTransactions { get; set; }
}
