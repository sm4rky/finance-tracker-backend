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

    [HttpGet("recent")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(List<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetRecent(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var dto = await transactionService.GetRecentAsync(User, limit, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Create(
        [FromBody] SaveTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await transactionService.CreateAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPut("{transactionId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Update(
        [FromRoute] Guid transactionId,
        [FromBody] SaveTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await transactionService.UpdateAsync(User, transactionId, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("delete")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DeleteTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DeleteTransactionsResponse>> DeleteMany(
        [FromBody] DeleteTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await transactionService.DeleteManyAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
