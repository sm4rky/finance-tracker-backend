using System.Text.Json;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class PushNotificationService(
    IPushSubscriptionRepository pushSubscriptionRepository,
    IPushLogRepository pushLogRepository,
    IWebPushSender webPushSender,
    ILogger<PushNotificationService> logger) : IPushNotificationService
{
    public async Task SendPushAsync(
        Guid profileId,
        string dedupeKey,
        string title,
        string body,
        string url,
        CancellationToken cancellationToken = default)
    {
        if (await pushLogRepository
                .ExistsByProfileAndDedupeKeyAsync(profileId, dedupeKey, cancellationToken)
                .ConfigureAwait(false))
        {
            logger.LogInformation(
                "Skipping duplicate push notification for profile {ProfileId} and dedupe key {DedupeKey}.",
                profileId,
                dedupeKey);
            return;
        }

        var subscriptions = await pushSubscriptionRepository
            .ListByProfileIdAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        if (subscriptions.Count == 0)
        {
            logger.LogInformation("Skipping push notification: profile {ProfileId} has no push subscriptions.",
                profileId);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            title,
            body,
            url
        });

        var sentCount = 0;
        var errors = new List<string>();
        foreach (var subscription in subscriptions)
        {
            var outcome = await webPushSender.SendAsync(subscription, payloadJson, cancellationToken)
                .ConfigureAwait(false);
            if (outcome.Success)
            {
                sentCount++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(outcome.ErrorMessage))
                errors.Add(outcome.ErrorMessage);

            logger.LogWarning(
                "Push notification failed for profile {ProfileId}, subscription {PushSubscriptionId}: {Error}",
                profileId,
                subscription.Id,
                outcome.ErrorMessage);

            if (outcome.ShouldDeleteSubscription)
                await pushSubscriptionRepository
                    .DeleteForProfileAsync(profileId, subscription.Id, cancellationToken)
                    .ConfigureAwait(false);
        }

        var success = sentCount > 0;
        await InsertLogAsync(
            profileId,
            dedupeKey,
            success ? "sent" : "failed",
            success ? null : string.Join(" | ", errors.Distinct()),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertLogAsync(
        Guid profileId,
        string dedupeKey,
        string status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var log = new PushLog
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PushSubscriptionId = null,
            DedupeKey = dedupeKey,
            Status = status,
            ErrorMessage = errorMessage,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await pushLogRepository.InsertAsync(log, cancellationToken).ConfigureAwait(false);
    }
}