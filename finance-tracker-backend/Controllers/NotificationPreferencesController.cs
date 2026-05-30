using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/notification-preferences")]
public sealed class NotificationPreferencesController(INotificationPreferenceService notificationPreferenceService)
    : ControllerBase
{
    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileNotificationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> GetMyNotificationPreference(CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.GetMyNotificationPreferenceAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("{profileId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileNotificationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> GetProfileNotificationPreference(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.GetProfileNotificationPreferenceAsync(User, profileId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("me/email-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateMyEmailEnabled(
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateMyEmailEnabledAsync(User, request.Enabled, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("{profileId:guid}/email-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateProfileEmailEnabled(
        Guid profileId,
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateProfileEmailEnabledAsync(
                User,
                profileId,
                request.Enabled,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("me/due-reminder-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateMyDueReminderEnabled(
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateMyDueReminderEnabledAsync(
                User,
                request.Enabled,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("{profileId:guid}/due-reminder-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateProfileDueReminderEnabled(
        Guid profileId,
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateProfileDueReminderEnabledAsync(
                User,
                profileId,
                request.Enabled,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("me/reminder-days-before")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateMyReminderDaysBefore(
        [FromBody] UpdateReminderDaysBeforeRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateMyReminderDaysBeforeAsync(User, request.Days, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("{profileId:guid}/reminder-days-before")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateProfileReminderDaysBefore(
        Guid profileId,
        [FromBody] UpdateReminderDaysBeforeRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateProfileReminderDaysBeforeAsync(
                User,
                profileId,
                request.Days,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("me/budget-alert-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateMyBudgetAlertEnabled(
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateMyBudgetAlertEnabledAsync(
                User,
                request.Enabled,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("{profileId:guid}/budget-alert-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateProfileBudgetAlertEnabled(
        Guid profileId,
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateProfileBudgetAlertEnabledAsync(
                User,
                profileId,
                request.Enabled,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("me/budget-alert-threshold")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateMyBudgetAlertThreshold(
        [FromBody] UpdateBudgetAlertThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateMyBudgetAlertThresholdAsync(
                User,
                request.Threshold,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("{profileId:guid}/budget-alert-threshold")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateProfileBudgetAlertThreshold(
        Guid profileId,
        [FromBody] UpdateBudgetAlertThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateProfileBudgetAlertThresholdAsync(
                User,
                profileId,
                request.Threshold,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("me/monthly-statement-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateMyMonthlyStatementEnabled(
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateMyMonthlyStatementEnabledAsync(
                User,
                request.Enabled,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPatch("{profileId:guid}/monthly-statement-enabled")]
    public async Task<ActionResult<ProfileNotificationPreferenceResponse>> UpdateProfileMonthlyStatementEnabled(
        Guid profileId,
        [FromBody] UpdateBooleanPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await notificationPreferenceService.UpdateProfileMonthlyStatementEnabledAsync(
                User,
                profileId,
                request.Enabled,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
