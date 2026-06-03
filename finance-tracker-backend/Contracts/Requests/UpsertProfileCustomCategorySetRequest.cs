namespace finance_tracker_backend.Contracts.Requests;

public sealed class UpsertProfileCustomCategorySetRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<UpsertProfileCustomCategoryRequest> Categories { get; set; } = [];
}
