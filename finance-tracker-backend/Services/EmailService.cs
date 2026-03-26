using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace finance_tracker_backend.Services;

public sealed class EmailService(
    IResendTemplateEmailSender resendTemplateEmailSender,
    IEmailLogRepository emailLogRepository,
    IConfiguration configuration,
    ILogger<EmailService> logger) : IEmailService
{
    public async Task SendWelcomeForNewProfileAsync(
        Guid profileId,
        string recipientEmail,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        var to = string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail.Trim();
        if (string.IsNullOrEmpty(to))
        {
            logger.LogWarning("Skipping welcome email: profile {ProfileId} has no email.", profileId);
            return;
        }

        var templateId = configuration["Resend:WelcomeTemplateId"]?.Trim();
        var now = DateTimeOffset.UtcNow;

        if (string.IsNullOrEmpty(templateId))
        {
            logger.LogWarning("Resend:WelcomeTemplateId is not configured; skipping Resend call.");
            await InsertLogAsync(
                profileId,
                to,
                templateId,
                false,
                null,
                "Resend:WelcomeTemplateId is not configured.",
                now,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var appUrl = configuration["Application:PublicUrl"]?.Trim();
        if (string.IsNullOrEmpty(appUrl))
            appUrl = "http://localhost:3000";

        var variables = new Dictionary<string, string>(StringComparer.Ordinal) { ["app_url"] = appUrl };
        var outcome = await resendTemplateEmailSender
            .SendWithTemplateAsync(to, templateId, variables, cancellationToken)
            .ConfigureAwait(false);

        await InsertLogAsync(
            profileId,
            to,
            templateId,
            outcome.Success,
            outcome.ProviderMessageId,
            outcome.ErrorMessage,
            now,
            cancellationToken).ConfigureAwait(false);

        if (!outcome.Success)
            logger.LogWarning("Welcome template email failed for profile {ProfileId}: {Error}", profileId, outcome.ErrorMessage);
    }

    private async Task InsertLogAsync(
        Guid profileId,
        string to,
        string? templateId,
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
            Status = success ? "sent" : "failed",
            ProviderMessageId = providerId,
            ErrorMessage = error,
            CreatedAt = createdAt
        };
        await emailLogRepository.InsertAsync(log, cancellationToken).ConfigureAwait(false);
    }
}
