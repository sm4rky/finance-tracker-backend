using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class NotificationPreferenceService(
    IProfileNotificationPreferenceRepository preferenceRepository,
    IProfileService profileService) : INotificationPreferenceService
{
    public async Task<ProfileNotificationPreferenceResponse> GetMyNotificationPreferenceAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var row = await preferenceRepository.GetByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        return ToResponse(row ?? throw new KeyNotFoundException("Notification preferences were not found."));
    }

    public async Task<ProfileNotificationPreferenceResponse> GetProfileNotificationPreferenceAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        var row = await preferenceRepository.GetByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        return ToResponse(row ?? throw new KeyNotFoundException("Notification preferences were not found."));
    }

    public Task<ProfileNotificationPreferenceResponse> UpdateMyEmailEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        UpdateEmailEnabledAsync(user.RequireProfileId(), enabled, cancellationToken);

    public async Task<ProfileNotificationPreferenceResponse> UpdateProfileEmailEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        return await UpdateEmailEnabledAsync(profileId, enabled, cancellationToken).ConfigureAwait(false);
    }

    public Task<ProfileNotificationPreferenceResponse> UpdateMyDueReminderEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        UpdateDueReminderEnabledAsync(user.RequireProfileId(), enabled, cancellationToken);

    public async Task<ProfileNotificationPreferenceResponse> UpdateProfileDueReminderEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        return await UpdateDueReminderEnabledAsync(profileId, enabled, cancellationToken).ConfigureAwait(false);
    }

    public Task<ProfileNotificationPreferenceResponse> UpdateMyReminderDaysBeforeAsync(
        ClaimsPrincipal user,
        int days,
        CancellationToken cancellationToken = default) =>
        UpdateReminderDaysBeforeAsync(user.RequireProfileId(), days, cancellationToken);

    public async Task<ProfileNotificationPreferenceResponse> UpdateProfileReminderDaysBeforeAsync(
        ClaimsPrincipal user,
        Guid profileId,
        int days,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        return await UpdateReminderDaysBeforeAsync(profileId, days, cancellationToken).ConfigureAwait(false);
    }

    public Task<ProfileNotificationPreferenceResponse> UpdateMyBudgetAlertEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        UpdateBudgetAlertEnabledAsync(user.RequireProfileId(), enabled, cancellationToken);

    public async Task<ProfileNotificationPreferenceResponse> UpdateProfileBudgetAlertEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        return await UpdateBudgetAlertEnabledAsync(profileId, enabled, cancellationToken).ConfigureAwait(false);
    }

    public Task<ProfileNotificationPreferenceResponse> UpdateMyBudgetAlertThresholdAsync(
        ClaimsPrincipal user,
        int threshold,
        CancellationToken cancellationToken = default) =>
        UpdateBudgetAlertThresholdAsync(user.RequireProfileId(), threshold, cancellationToken);

    public async Task<ProfileNotificationPreferenceResponse> UpdateProfileBudgetAlertThresholdAsync(
        ClaimsPrincipal user,
        Guid profileId,
        int threshold,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        return await UpdateBudgetAlertThresholdAsync(profileId, threshold, cancellationToken).ConfigureAwait(false);
    }

    public Task<ProfileNotificationPreferenceResponse> UpdateMyMonthlyStatementEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        UpdateMonthlyStatementEnabledAsync(user.RequireProfileId(), enabled, cancellationToken);

    public async Task<ProfileNotificationPreferenceResponse> UpdateProfileMonthlyStatementEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);
        return await UpdateMonthlyStatementEnabledAsync(profileId, enabled, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProfileNotificationPreferenceResponse> UpdateEmailEnabledAsync(
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var row = await GetByProfileIdOrThrowAsync(profileId, cancellationToken).ConfigureAwait(false);
        row.EmailEnabled = enabled;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await preferenceRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        return ToResponse(row);
    }

    private async Task<ProfileNotificationPreferenceResponse> UpdateDueReminderEnabledAsync(
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var row = await GetByProfileIdOrThrowAsync(profileId, cancellationToken).ConfigureAwait(false);
        row.DueReminderEnabled = enabled;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await preferenceRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        return ToResponse(row);
    }

    private async Task<ProfileNotificationPreferenceResponse> UpdateReminderDaysBeforeAsync(
        Guid profileId,
        int days,
        CancellationToken cancellationToken)
    {
        if (days <= 0)
            throw new ArgumentException("days must be greater than 0.");

        var row = await GetByProfileIdOrThrowAsync(profileId, cancellationToken).ConfigureAwait(false);
        row.ReminderDaysBefore = days;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await preferenceRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        return ToResponse(row);
    }

    private async Task<ProfileNotificationPreferenceResponse> UpdateBudgetAlertEnabledAsync(
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var row = await GetByProfileIdOrThrowAsync(profileId, cancellationToken).ConfigureAwait(false);
        row.BudgetAlertEnabled = enabled;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await preferenceRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        return ToResponse(row);
    }

    private async Task<ProfileNotificationPreferenceResponse> UpdateBudgetAlertThresholdAsync(
        Guid profileId,
        int threshold,
        CancellationToken cancellationToken)
    {
        if (threshold is <= 0 or > 100)
            throw new ArgumentException("threshold must be between 1 and 100.");

        var row = await GetByProfileIdOrThrowAsync(profileId, cancellationToken).ConfigureAwait(false);
        row.BudgetAlertThreshold = threshold;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await preferenceRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        return ToResponse(row);
    }

    private async Task<ProfileNotificationPreferenceResponse> UpdateMonthlyStatementEnabledAsync(
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var row = await GetByProfileIdOrThrowAsync(profileId, cancellationToken).ConfigureAwait(false);
        row.MonthlyStatementEnabled = enabled;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await preferenceRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        return ToResponse(row);
    }

    private async Task<ProfileNotificationPreference> GetByProfileIdOrThrowAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await preferenceRepository.GetByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false)
               ?? throw new KeyNotFoundException("Notification preferences were not found.");
    }

    private async Task RequireAdminAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var profileId = user.RequireProfileId();
        var profile = await profileService.GetByIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(profile?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Admin access is required.");
    }

    private static ProfileNotificationPreferenceResponse ToResponse(ProfileNotificationPreference row) => new()
    {
        Id = row.Id,
        ProfileId = row.ProfileId,
        EmailEnabled = row.EmailEnabled,
        DueReminderEnabled = row.DueReminderEnabled,
        ReminderDaysBefore = row.ReminderDaysBefore,
        BudgetAlertEnabled = row.BudgetAlertEnabled,
        BudgetAlertThreshold = row.BudgetAlertThreshold,
        MonthlyStatementEnabled = row.MonthlyStatementEnabled,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };
}