using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Models;

namespace finance_tracker_backend.Services;

public sealed class EnsureUserService(
    IProfileService profileService,
    IProfileSubscriptionService profileSubscriptionService) : IEnsureUserService
{
    public async Task<EnsureUserResponse> EnsureAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        await profileService.EnsureRecordExistsAsync(user, cancellationToken).ConfigureAwait(false);
        await profileSubscriptionService.EnsureDefaultFreePlanExistsAsync(user, cancellationToken)
            .ConfigureAwait(false);

        var userId = RequireUserId(user);
        var profile = await profileService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
            throw new InvalidOperationException("Profile was not found after ensure.");

        var subscription = await profileSubscriptionService.GetForProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        var planFromDb = subscription?.PlanId;

        var email = string.IsNullOrWhiteSpace(profile.Email)
            ? user.FindFirstValue("email")
            : profile.Email;
        email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim();

        var fullName = string.IsNullOrWhiteSpace(profile.FullName) ? string.Empty : profile.FullName.Trim();

        var avatar = string.IsNullOrWhiteSpace(profile.AvatarUrl) ? null : profile.AvatarUrl.Trim();

        return new EnsureUserResponse
        {
            Email = email,
            FullName = fullName,
            AvatarUrl = avatar,
            Role = profile.Role,
            Plan = planFromDb ?? string.Empty
        };
    }

    private static Guid RequireUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Authenticated user is missing a valid 'sub' claim.");
        return userId;
    }
}
