using System.Globalization;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;
using Microsoft.Extensions.Configuration;

namespace finance_tracker_backend.Services;

public sealed class DueReminderNotificationService(
    IConfiguration configuration,
    IProfileRepository profileRepository,
    IProfileNotificationPreferenceRepository notificationPreferenceRepository,
    IProfileRecurringCashflowRepository recurringCashflowRepository,
    IEmailService emailService,
    IPushNotificationService pushNotificationService,
    ILogger<DueReminderNotificationService> logger) : IDueReminderNotificationService
{
    private const int DefaultBatchSize = 200;
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public async Task SendDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = configuration.GetValue("Hangfire:DueReminderNotificationBatchSize", DefaultBatchSize);
        if (batchSize < 1)
            batchSize = DefaultBatchSize;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var afterProfileId = Guid.Empty;
        var ok = 0;
        var failed = 0;
        var batches = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var profileIds = await profileRepository
                .ListProfileIdsAfterIdAsync(afterProfileId, batchSize, cancellationToken)
                .ConfigureAwait(false);

            if (profileIds.Count == 0)
                break;

            batches++;

            foreach (var profileId in profileIds)
            {
                try
                {
                    ok += await SendDueRemindersForProfileAsync(profileId, today, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogError(
                        ex,
                        "Due reminder notification failed for profile {ProfileId}; continuing with next profile.",
                        profileId);
                }
            }

            afterProfileId = profileIds[^1];
            if (profileIds.Count < batchSize)
                break;
        }

        logger.LogInformation(
            "Due reminder notification job finished for {Today}: {Sent} reminder(s), {Failed} failed profile(s), {Batches} batch(es), batch size {BatchSize}.",
            today,
            ok,
            failed,
            batches,
            batchSize);
    }

    private async Task<int> SendDueRemindersForProfileAsync(
        Guid profileId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var preference = await notificationPreferenceRepository
            .GetByProfileIdAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        if (preference is null || !preference.DueReminderEnabled)
            return 0;

        var targetDate = today.AddDays(preference.ReminderDaysBefore);
        var rows = await recurringCashflowRepository
            .ListForProfileAsync(profileId, status: null, cancellationToken)
            .ConfigureAwait(false);

        var dueRows = rows
            .Where(row => row.PredictedNextDate == targetDate)
            .Where(row => !string.Equals(row.Frequency, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (dueRows.Count == 0)
            return 0;

        var profile = preference.EmailEnabled
            ? await profileRepository.GetByIdAsync(profileId, cancellationToken).ConfigureAwait(false)
            : null;
        var recipientEmail = profile?.Email?.Trim() ?? string.Empty;
        var templateId = configuration["Resend:UpcomingPaymentReminderTemplateId"] ?? string.Empty;
        var appUrl = configuration["Application:PublicUrl"]?.Trim();
        if (string.IsNullOrEmpty(appUrl))
            appUrl = "http://localhost:3000";
        appUrl = $"{appUrl.TrimEnd('/')}/subscriptions";

        var sent = 0;
        foreach (var row in dueRows)
        {
            var isOutflow = string.Equals(row.Direction, "outflow", StringComparison.OrdinalIgnoreCase);
            var name = ResolveCashflowName(row);
            var amount = row.ExpectedAmount.ToString("C", UsCulture);
            var dueDateText = targetDate.ToString("MMMM d, yyyy", UsCulture);
            var daysText = preference.ReminderDaysBefore == 1 ? "1 day" : $"{preference.ReminderDaysBefore} days";
            var reminderTitle = isOutflow ? "Upcoming payment reminder" : "Upcoming income reminder";
            var actionText = isOutflow ? "is due on" : "is expected on";
            var subjectVerb = isOutflow ? "due" : "expected";
            var subject = $"{name} {subjectVerb} in {daysText}";
            var pushTitle = isOutflow ? "Upcoming payment" : "Upcoming income";
            var pushBody = isOutflow
                ? $"{name} is due in {daysText} for {amount}."
                : $"{name} is expected in {daysText} for {amount}.";
            var variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["reminder_title"] = reminderTitle,
                ["cashflow_name"] = name,
                ["cashflow_action_text"] = actionText,
                ["due_date"] = dueDateText,
                ["amount"] = amount,
                ["app_url"] = appUrl
            };
            var dedupeKey = NotificationDedupeKeys.DueReminder(row.Id, targetDate);

            if (preference.EmailEnabled)
                await emailService
                    .SendEmailAsync(
                        profileId,
                        recipientEmail,
                        templateId,
                        subject,
                        dedupeKey,
                        variables,
                        cancellationToken)
                    .ConfigureAwait(false);

            await pushNotificationService
                .SendPushAsync(
                    profileId,
                    dedupeKey,
                    pushTitle,
                    pushBody,
                    appUrl,
                    cancellationToken)
                .ConfigureAwait(false);

            sent++;
        }

        return sent;
    }

    private static string ResolveCashflowName(ProfileRecurringCashflow row)
    {
        if (!string.IsNullOrWhiteSpace(row.MerchantName))
            return row.MerchantName.Trim();

        if (!string.IsNullOrWhiteSpace(row.Description))
            return row.Description.Trim();

        return "A cashflow";
    }
}