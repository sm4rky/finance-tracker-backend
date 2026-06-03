namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileCustomCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorSet { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public IReadOnlyList<ProfileCustomCategoryPfcPrimaryResponse> PfcPrimaries { get; set; } = [];
}
