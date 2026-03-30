using finance_tracker_backend.Contracts.Pagination;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PagedResponse<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<TransactionResponse>>> Query(
        [FromQuery] QueryTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await transactionService.QueryAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
