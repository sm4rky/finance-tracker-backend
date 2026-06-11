using System.Globalization;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;
using Microsoft.Extensions.Configuration;

namespace finance_tracker_backend.Services;

public sealed class BudgetAlertNotificationService(
    IConfiguration configuration,
    IProfileRepository profileRepository,
    IProfileNotificationPreferenceRepository notificationPreferenceRepository,
    IEmailService emailService,
    IPushNotificationService pushNotificationService) : IBudgetAlertNotificationService
{
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public async Task SendBudgetAlertAsync(
        ProfileBudget budget,
        ProfileBudgetPeriod period,
        decimal spentAmount,
        CancellationToken cancellationToken = default)
    {
        var profileId = budget.ProfileId;
        var preference = await notificationPreferenceRepository
            .GetByProfileIdAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        if (preference is null || !preference.BudgetAlertEnabled)
            return;

        if (period.AmountLimit <= 0m)
            return;

        var threshold = preference.BudgetAlertThreshold;
        var spentPercent = spentAmount / period.AmountLimit * 100m;
        if (spentPercent < threshold)
            return;

        var appUrl = configuration["Application:PublicUrl"]?.Trim();
        if (string.IsNullOrEmpty(appUrl))
            appUrl = "http://localhost:3000";
        appUrl = $"{appUrl.TrimEnd('/')}/budgets";

        var spentText = spentAmount.ToString("C", UsCulture);
        var limitText = period.AmountLimit.ToString("C", UsCulture);
        var spentPercentText = $"{spentPercent.ToString("0.#", CultureInfo.InvariantCulture)}%";
        var subject = $"{budget.Name} budget reached {spentPercentText}";
        var pushTitle = "Budget alert";
        var pushBody = $"{budget.Name} reached {spentPercentText}: {spentText} of {limitText} spent.";
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["budget_name"] = budget.Name,
            ["threshold_percent"] = spentPercentText,
            ["spent_amount"] = spentText,
            ["amount_limit"] = limitText,
            ["app_url"] = appUrl
        };
        var dedupeKey = NotificationDedupeKeys.BudgetAlert(period.Id, threshold);

        if (preference.EmailEnabled)
        {
            var profile = await profileRepository.GetByIdAsync(profileId, cancellationToken).ConfigureAwait(false);
            var recipientEmail = profile?.Email?.Trim() ?? string.Empty;
            var templateId = configuration["Resend:BudgetAlertTemplateId"] ?? string.Empty;

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
        }

        await pushNotificationService
            .SendPushAsync(
                profileId,
                dedupeKey,
                pushTitle,
                pushBody,
                appUrl,
                cancellationToken)
            .ConfigureAwait(false);
    }
}