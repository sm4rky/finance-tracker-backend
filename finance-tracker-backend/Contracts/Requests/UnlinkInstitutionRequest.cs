namespace finance_tracker_backend.Contracts.Requests;

public sealed class UnlinkInstitutionRequest
{
    public Guid LinkedBankId { get; set; }
    public bool DeleteTransactions { get; set; }
}
