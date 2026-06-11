using System.Globalization;

namespace finance_tracker_backend.Infrastructure;

public static class NotificationDedupeKeys
{
    public static string Welcome() => "type-welcome";

    public static string BudgetAlert(Guid periodId, int threshold) =>
        $"type-budget-alert:period-id-{periodId:D}:threshold-{threshold}";

    public static string DueReminder(Guid recurringCashflowId, DateOnly dueDate) =>
        $"type-due-reminder:id-{recurringCashflowId:D}:due-date-{dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    public static string Statement(Guid id) =>
        $"type-statement:id-{id:D}";
}