using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class PushSubscriptionService(
    IPushSubscriptionRepository pushSubscriptionRepository,
    IProfileService profileService) : IPushSubscriptionService
{
    public async Task<IReadOnlyList<PushSubscriptionResponse>> ListMyPushSubscriptionsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var rows = await pushSubscriptionRepository.ListByProfileIdAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<PushSubscriptionResponse>> ListProfilePushSubscriptionsAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        var rows = await pushSubscriptionRepository.ListByProfileIdAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToResponse).ToList();
    }

    public async Task<PushSubscriptionResponse> UpsertMyPushSubscriptionAsync(
        ClaimsPrincipal user,
        UpsertPushSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profileId = user.RequireProfileId();
        var endpoint = TrimRequired(request.Endpoint, "endpoint");
        var p256dh = TrimRequired(request.P256dh, "p256dh");
        var auth = TrimRequired(request.Auth, "auth");

        var row = await pushSubscriptionRepository.UpsertByEndpointAsync(
                new PushSubscription
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profileId,
                    Endpoint = endpoint,
                    P256dh = p256dh,
                    Auth = auth,
                    UserAgent = TrimToNull(request.UserAgent)
                },
                cancellationToken)
            .ConfigureAwait(false);

        return ToResponse(row);
    }

    public async Task DeleteMyPushSubscriptionAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var deleted = await pushSubscriptionRepository.DeleteForProfileAsync(profileId, id, cancellationToken)
            .ConfigureAwait(false);
        if (deleted == 0)
            throw new KeyNotFoundException("Push subscription was not found.");
    }

    private async Task RequireAdminAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var profileId = user.RequireProfileId();
        var profile = await profileService.GetByIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(profile?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Admin access is required.");
    }

    private static string TrimRequired(string? value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{fieldName} is required.") : value.Trim();
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PushSubscriptionResponse ToResponse(PushSubscription row) => new()
    {
        Id = row.Id,
        ProfileId = row.ProfileId,
        Endpoint = row.Endpoint,
        UserAgent = row.UserAgent,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };
}
