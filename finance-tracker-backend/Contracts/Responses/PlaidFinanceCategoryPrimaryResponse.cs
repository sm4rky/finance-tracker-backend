namespace finance_tracker_backend.Contracts.Responses;

public sealed class PlaidFinanceCategoryPrimaryResponse
{
    public string PfcVersion { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
