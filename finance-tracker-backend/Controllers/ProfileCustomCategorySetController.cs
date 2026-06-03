using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/profile-custom-category-sets")]
public sealed class ProfileCustomCategorySetController(
    IProfileCustomCategorySetService customCategorySetService) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileCustomCategorySetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProfileCustomCategorySetResponse>>> List(
        CancellationToken cancellationToken)
    {
        var items = await customCategorySetService.ListAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpPost("upsert")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProfileCustomCategorySetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileCustomCategorySetResponse>> Upsert(
        [FromBody] UpsertProfileCustomCategorySetRequest request,
        CancellationToken cancellationToken)
    {
        var item = await customCategorySetService.UpsertAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await customCategorySetService.DeleteAsync(User, id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
