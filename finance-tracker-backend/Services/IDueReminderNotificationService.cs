namespace finance_tracker_backend.Services;

public interface IDueReminderNotificationService
{
    Task SendDueRemindersAsync(CancellationToken cancellationToken = default);
}