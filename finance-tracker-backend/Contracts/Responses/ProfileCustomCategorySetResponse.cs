namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileCustomCategorySetResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public IReadOnlyList<ProfileCustomCategoryResponse> Categories { get; set; } = [];
}
