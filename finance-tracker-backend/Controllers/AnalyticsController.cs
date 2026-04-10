using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AnalyticsController(INetWorthService netWorthService, ICashflowService cashflowService)
    : ControllerBase
{
    [HttpGet("net-worth")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(NetWorthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NetWorthResponse>> GetNetWorth(CancellationToken cancellationToken)
    {
        var dto = await netWorthService.GetNetWorthAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("cashflow")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CashflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CashflowResponse>> GetCashflow(
        [FromQuery] QueryCashflowRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await cashflowService.GetAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
