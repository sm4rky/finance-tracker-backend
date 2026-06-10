using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/profile-budgets")]
public sealed class ProfileBudgetController(IProfileBudgetService profileBudgetService) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileBudgetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProfileBudgetResponse>>> List(CancellationToken cancellationToken)
    {
        var items = await profileBudgetService.ListAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("ongoing-periods")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileBudgetPeriodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProfileBudgetPeriodResponse>>> ListOngoingPeriods(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var items = await profileBudgetService.ListOngoingPeriodsAsync(User, limit, cancellationToken)
            .ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileBudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileBudgetResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await profileBudgetService.GetByIdAsync(User, id, cancellationToken).ConfigureAwait(false);
        return Ok(item);
    }

    [HttpGet("{id:guid}/periods")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileBudgetPeriodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProfileBudgetPeriodResponse>>> ListPeriods(
        Guid id,
        CancellationToken cancellationToken)
    {
        var items = await profileBudgetService.ListPeriodsAsync(User, id, cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileBudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileBudgetResponse>> Create(
        [FromBody] CreateProfileBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var item = await profileBudgetService.CreateAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(item);
    }

    [HttpPut("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileBudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileBudgetResponse>> Update(
        Guid id,
        [FromBody] UpdateProfileBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var item = await profileBudgetService.UpdateAsync(User, id, request, cancellationToken).ConfigureAwait(false);
        return Ok(item);
    }

    [HttpPatch("{id:guid}/active")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileBudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileBudgetResponse>> UpdateActive(
        Guid id,
        [FromBody] UpdateProfileBudgetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var item = await profileBudgetService.UpdateActiveAsync(User, id, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await profileBudgetService.DeleteAsync(User, id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
