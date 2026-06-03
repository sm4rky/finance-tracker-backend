namespace finance_tracker_backend.Contracts.Requests;

public sealed class UpsertProfileCustomCategoryPfcPrimaryRequest
{
    public string PfcPrimaryCode { get; set; } = string.Empty;
    public string PfcVersion { get; set; } = string.Empty;
}
