namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileBudgetPeriodResponse
{
    public Guid Id { get; init; }
    public DateOnly PeriodStartDate { get; init; }
    public DateOnly PeriodEndDate { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public decimal AmountLimit { get; init; }
    public decimal SpentAmount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
