using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PlanController(IPlanService planService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>> ListSubscriptionPlans(
        CancellationToken cancellationToken)
    {
        var dto = await planService.ListSubscriptionPlansAsync(cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("payment")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPaymentResponse>>> ListAllSubscriptionPayments(
        CancellationToken cancellationToken)
    {
        var dto = await planService.ListAllSubscriptionPaymentsAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("payment/me")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPaymentResponse>>> ListMySubscriptionPayments(
        CancellationToken cancellationToken)
    {
        var dto = await planService.ListMySubscriptionPaymentsAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("payment/{profileId:guid}")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPaymentResponse>>> ListProfileSubscriptionPayments(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var dto = await planService
            .ListProfileSubscriptionPaymentsAsync(User, profileId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("payment-history")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPaymentHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPaymentHistoryResponse>>> ListAllSubscriptionPaymentHistory(
        CancellationToken cancellationToken)
    {
        var dto = await planService.ListAllSubscriptionPaymentHistoryAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("payment-history/me")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPaymentHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPaymentHistoryResponse>>> ListMySubscriptionPaymentHistory(
        CancellationToken cancellationToken)
    {
        var dto = await planService.ListMySubscriptionPaymentHistoryAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("payment-history/{profileId:guid}")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPaymentHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPaymentHistoryResponse>>> ListProfileSubscriptionPaymentHistory(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var dto = await planService
            .ListProfileSubscriptionPaymentHistoryAsync(User, profileId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
