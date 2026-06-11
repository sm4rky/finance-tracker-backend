using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;

namespace finance_tracker_backend.Services;

public sealed class EnsureUserService(
    IProfileService profileService,
    IProfileSubscriptionService profileSubscriptionService,
    IEmailService emailService,
    IConfiguration configuration) : IEnsureUserService
{
    public async Task<EnsureUserResponse> EnsureAsync(ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var created = await profileService.EnsureRecordExistsAsync(user, cancellationToken).ConfigureAwait(false);
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

        var username = string.IsNullOrWhiteSpace(profile.Username) ? null : profile.Username.Trim();

        if (!created)
            return new EnsureUserResponse
            {
                Email = email,
                FullName = fullName,
                AvatarUrl = avatar,
                Username = username,
                Role = profile.Role,
                PasswordLoginEnabled = profile.PasswordLoginEnabled,
                Plan = planFromDb ?? string.Empty
            };

        var templateId = configuration["Resend:WelcomeTemplateId"]?.Trim() ?? string.Empty;
        var appUrl = configuration["Application:PublicUrl"]?.Trim();
        if (string.IsNullOrEmpty(appUrl))
            appUrl = "http://localhost:3000";

        var variables = new Dictionary<string, string>(StringComparer.Ordinal) { ["app_url"] = appUrl };
        await emailService
            .SendEmailAsync(
                userId,
                email,
                templateId,
                null,
                NotificationDedupeKeys.Welcome(),
                variables,
                cancellationToken)
            .ConfigureAwait(false);

        return new EnsureUserResponse
        {
            Email = email,
            FullName = fullName,
            AvatarUrl = avatar,
            Username = username,
            Role = profile.Role,
            PasswordLoginEnabled = profile.PasswordLoginEnabled,
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