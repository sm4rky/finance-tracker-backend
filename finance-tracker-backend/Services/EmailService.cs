using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class EmailService(
    IResendTemplateEmailSender resendTemplateEmailSender,
    IEmailLogRepository emailLogRepository,
    ILogger<EmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(
        Guid profileId,
        string recipientEmail,
        string templateId,
        string? subject,
        string dedupeKey,
        IReadOnlyDictionary<string, string>? variables,
        CancellationToken cancellationToken = default)
    {
        if (await emailLogRepository
                .ExistsByProfileAndDedupeKeyAsync(profileId, dedupeKey, cancellationToken)
                .ConfigureAwait(false))
        {
            logger.LogInformation(
                "Skipping duplicate email for profile {ProfileId} and dedupe key {DedupeKey}.",
                profileId,
                dedupeKey);
            return;
        }

        var to = string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail.Trim();
        if (string.IsNullOrEmpty(to))
        {
            logger.LogWarning("Skipping email: profile {ProfileId} has no email.", profileId);
            return;
        }

        if (string.IsNullOrEmpty(templateId))
        {
            logger.LogWarning("Email template id is not configured; skipping Resend call for profile {ProfileId}.",
                profileId);
            return;
        }

        var outcome = await resendTemplateEmailSender
            .SendWithTemplateAsync(to, templateId, subject, variables, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;

        await InsertLogAsync(
            profileId,
            to,
            templateId,
            dedupeKey,
            outcome.Success,
            outcome.ProviderMessageId,
            outcome.ErrorMessage,
            now,
            cancellationToken).ConfigureAwait(false);

        if (!outcome.Success)
            logger.LogWarning("Template email failed for profile {ProfileId}: {Error}", profileId,
                outcome.ErrorMessage);
    }

    private async Task InsertLogAsync(
        Guid profileId,
        string to,
        string? templateId,
        string dedupeKey,
        bool success,
        string? providerId,
        string? error,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var log = new EmailLog
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            RecipientEmail = to,
            TemplateId = templateId,
            DedupeKey = dedupeKey,
            Status = success ? "sent" : "failed",
            ProviderMessageId = providerId,
            ErrorMessage = error,
            CreatedAt = createdAt
        };
        await emailLogRepository.InsertAsync(log, cancellationToken).ConfigureAwait(false);
    }
}