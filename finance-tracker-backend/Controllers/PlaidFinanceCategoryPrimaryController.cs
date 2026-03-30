using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/plaid-finance-category-primary")]
[Authorize]
public sealed class PlaidFinanceCategoryPrimaryController(
    IPlaidFinanceCategoryPrimaryReadService plaidFinanceCategoryPrimaryReadService) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<PlaidFinanceCategoryPrimaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PlaidFinanceCategoryPrimaryResponse>>> List(
        [FromQuery] string? pfcVersion,
        CancellationToken cancellationToken)
    {
        var items = await plaidFinanceCategoryPrimaryReadService
            .ListAsync(pfcVersion, cancellationToken)
            .ConfigureAwait(false);
        return Ok(items);
    }
}
