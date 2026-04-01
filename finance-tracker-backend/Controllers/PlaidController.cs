using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Middleware;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PlaidController(
    IPlaidConnectionService plaidConnectionService,
    IPlaidTransactionSyncService plaidTransactionSyncService) : ControllerBase
{
    [HttpPost("link-token")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreatePlaidLinkTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreatePlaidLinkTokenResponse>> CreateLinkToken(
        [FromBody] CreatePlaidLinkTokenRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new CreatePlaidLinkTokenRequest();
        var dto = await plaidConnectionService.CreateLinkTokenAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("exchange")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExchangePlaidPublicTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExchangePlaidPublicTokenResponse>> Exchange(
        [FromBody] ExchangePlaidPublicTokenRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await plaidConnectionService.ExchangePublicTokenAsync(User, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("connections")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<LinkedBankSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<LinkedBankSummaryResponse>>> ListConnections(CancellationToken cancellationToken)
    {
        var list = await plaidConnectionService.ListConnectionsAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(list);
    }

    [HttpPost("connections/{linkedBankId:guid}/soft-disconnect")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SoftDisconnectLinkedBankResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SoftDisconnectLinkedBankResponse>> SoftDisconnect(
        Guid linkedBankId,
        CancellationToken cancellationToken)
    {
        var dto = await plaidConnectionService.SoftDisconnectAsync(User, linkedBankId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("connections/unlink")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UnlinkInstitutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnlinkInstitutionResponse>> UnlinkInstitution(
        [FromBody] UnlinkInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await plaidConnectionService.UnlinkInstitutionAsync(User, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("connections/{linkedBankId:guid}/transactions/sync")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SyncPlaidTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SyncPlaidTransactionsResponse>> SyncTransactionsForBank(
        Guid linkedBankId,
        CancellationToken cancellationToken)
    {
        var dto = await plaidTransactionSyncService.SyncLinkedBankAsync(User, linkedBankId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
