namespace finance_tracker_backend.Infrastructure;

public static class NotificationDedupeKeys
{
    public static string Welcome() => "type-welcome";

    public static string BudgetAlert(Guid id, int threshold) =>
        $"type-budget-alert:id-{id:D}:threshold-{threshold}";

    public static string DueReminder(Guid id, int day) =>
        $"type-due-reminder:id-{id:D}:day-{day}";

    public static string Statement(Guid id) =>
        $"type-statement:id-{id:D}";
}
