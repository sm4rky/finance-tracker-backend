namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileCustomCategoryPfcPrimaryResponse
{
    public Guid Id { get; set; }
    public string PfcPrimaryCode { get; set; } = string.Empty;
    public string PfcVersion { get; set; } = string.Empty;
}
