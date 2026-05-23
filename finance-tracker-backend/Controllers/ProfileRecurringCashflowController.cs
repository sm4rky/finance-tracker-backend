using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/profile-recurring-cashflow")]
public sealed class ProfileRecurringCashflowController(
    IProfileRecurringCashflowService recurringCashflowService,
    IPlaidRecurringCashflowRefreshService plaidRecurringCashflowRefreshService) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileRecurringCashflowResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProfileRecurringCashflowResponse>>> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var list = await recurringCashflowService.ListAsync(User, status, cancellationToken).ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("calendar")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileRecurringCashflowCalendarOccurrenceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProfileRecurringCashflowCalendarOccurrenceResponse>>> Calendar(
        [FromQuery] string dateFrom,
        [FromQuery] string dateTo,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(dateFrom, out var from) || !DateOnly.TryParse(dateTo, out var to))
            throw new ArgumentException("dateFrom and dateTo must be ISO dates (YYYY-MM-DD).");

        var list = await recurringCashflowService.GetCalendarAsync(User, from, to, cancellationToken).ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileRecurringCashflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileRecurringCashflowResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var dto = await recurringCashflowService.GetByIdAsync(User, id, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileRecurringCashflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileRecurringCashflowResponse>> Create(
        [FromBody] SaveProfileRecurringCashflowRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await recurringCashflowService.CreateAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileRecurringCashflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileRecurringCashflowResponse>> Update(
        Guid id,
        [FromBody] SaveProfileRecurringCashflowRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await recurringCashflowService.UpdateAsync(User, id, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await recurringCashflowService.DeleteAsync(User, id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("sync")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SyncPlaidRecurringCashflowsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncPlaidRecurringCashflowsResponse>> SyncFromPlaid(
        [FromBody] SyncPlaidRecurringCashflowsRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await plaidRecurringCashflowRefreshService
            .SyncLinkedBankRecurringCashflowsAsync(User, request.LinkedBankId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
