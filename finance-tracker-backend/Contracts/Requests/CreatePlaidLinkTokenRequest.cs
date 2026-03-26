namespace finance_tracker_backend.Contracts.Requests;

public sealed class CreatePlaidLinkTokenRequest
{
    /// <summary>connect | relink | update. Default connect.</summary>
    public string Intent { get; set; } = "connect";

    public Guid? LinkedBankId { get; set; }
}
