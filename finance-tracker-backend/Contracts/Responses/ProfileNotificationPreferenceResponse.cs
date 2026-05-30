namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileNotificationPreferenceResponse
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public bool EmailEnabled { get; init; }
    public bool DueReminderEnabled { get; init; }
    public int ReminderDaysBefore { get; init; }
    public bool BudgetAlertEnabled { get; init; }
    public int BudgetAlertThreshold { get; init; }
    public bool MonthlyStatementEnabled { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
