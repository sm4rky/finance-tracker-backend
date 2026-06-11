namespace finance_tracker_backend.Services;

public interface IPushNotificationService
{
    Task SendPushAsync(
        Guid profileId,
        string dedupeKey,
        string title,
        string body,
        string url,
        CancellationToken cancellationToken = default);
}
