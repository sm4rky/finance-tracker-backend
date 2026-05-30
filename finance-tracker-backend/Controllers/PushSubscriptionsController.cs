using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/push-subscriptions")]
public sealed class PushSubscriptionsController(IPushSubscriptionService pushSubscriptionService) : ControllerBase
{
    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PushSubscriptionResponse>>> ListMyPushSubscriptions(
        CancellationToken cancellationToken)
    {
        var dto = await pushSubscriptionService.ListMyPushSubscriptionsAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("{profileId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PushSubscriptionResponse>>> ListProfilePushSubscriptions(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var dto = await pushSubscriptionService.ListProfilePushSubscriptionsAsync(User, profileId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("me")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PushSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PushSubscriptionResponse>> UpsertMyPushSubscription(
        [FromBody] UpsertPushSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await pushSubscriptionService.UpsertMyPushSubscriptionAsync(User, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpDelete("me/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyPushSubscription(
        Guid id,
        CancellationToken cancellationToken)
    {
        await pushSubscriptionService.DeleteMyPushSubscriptionAsync(User, id, cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }
}
