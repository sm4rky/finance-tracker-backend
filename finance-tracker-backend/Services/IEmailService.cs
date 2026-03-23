namespace finance_tracker_backend.Services;

public interface IEmailService
{
    Task SendWelcomeForNewProfileAsync(
        Guid profileId,
        string recipientEmail,
        string? displayName,
        CancellationToken cancellationToken = default);
}
