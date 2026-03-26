using System.Security.Claims;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class ProfileSubscriptionService(IProfileSubscriptionRepository profileSubscriptionRepository)
    : IProfileSubscriptionService
{
    public async Task EnsureDefaultFreePlanExistsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(user);

        if (await profileSubscriptionRepository.ExistsForProfileAsync(userId, cancellationToken).ConfigureAwait(false))
            return;

        var now = DateTimeOffset.UtcNow;
        await profileSubscriptionRepository.InsertAsync(
            new ProfileSubscription
            {
                Id = Guid.NewGuid(),
                ProfileId = userId,
                PlanId = "free",
                StartDate = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken).ConfigureAwait(false);
    }

    public Task<ProfileSubscription?> GetForProfileAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        profileSubscriptionRepository.GetForProfileAsync(profileId, cancellationToken);

    private static Guid RequireUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Authenticated user is missing a valid 'sub' claim.");
        return userId;
    }
}
