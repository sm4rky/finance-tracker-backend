namespace finance_tracker_backend.Contracts.Requests;

public sealed class UpsertProfileCustomCategoryRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorSet { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public IReadOnlyList<UpsertProfileCustomCategoryPfcPrimaryRequest> PfcPrimaries { get; set; } = [];
}
