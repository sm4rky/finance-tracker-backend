using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface INotificationPreferenceService
{
    Task<ProfileNotificationPreferenceResponse> GetMyNotificationPreferenceAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> GetProfileNotificationPreferenceAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateMyEmailEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateProfileEmailEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateMyDueReminderEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateProfileDueReminderEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateMyReminderDaysBeforeAsync(
        ClaimsPrincipal user,
        int days,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateProfileReminderDaysBeforeAsync(
        ClaimsPrincipal user,
        Guid profileId,
        int days,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateMyBudgetAlertEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateProfileBudgetAlertEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateMyBudgetAlertThresholdAsync(
        ClaimsPrincipal user,
        int threshold,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateProfileBudgetAlertThresholdAsync(
        ClaimsPrincipal user,
        Guid profileId,
        int threshold,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateMyMonthlyStatementEnabledAsync(
        ClaimsPrincipal user,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<ProfileNotificationPreferenceResponse> UpdateProfileMonthlyStatementEnabledAsync(
        ClaimsPrincipal user,
        Guid profileId,
        bool enabled,
        CancellationToken cancellationToken = default);
}
