namespace finance_tracker_backend.Contracts.Requests;

public sealed class UpdateProfileBudgetRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal AmountLimit { get; set; }
    public bool IsActive { get; set; } = true;
}
