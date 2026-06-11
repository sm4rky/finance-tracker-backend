using finance_tracker_backend.Services;

namespace finance_tracker_backend.Jobs;

public sealed class DueReminderNotificationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<DueReminderNotificationJob> logger)
{
    public async Task RunAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IDueReminderNotificationService>();
            await notificationService.SendDueRemindersAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Due reminder notification job failed.");
            throw;
        }
    }
}