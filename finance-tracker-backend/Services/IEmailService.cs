namespace finance_tracker_backend.Services;

public interface IEmailService
{
    Task SendEmailAsync(
        Guid profileId,
        string recipientEmail,
        string templateId,
        string? subject,
        string dedupeKey,
        IReadOnlyDictionary<string, string>? variables,
        CancellationToken cancellationToken = default);
}