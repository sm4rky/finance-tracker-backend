using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IEnsureUserService ensureUserService) : ControllerBase
{
    /// <summary>
    /// Idempotent: ensures public.profiles and default free public.profile_subscriptions for the JWT subject,
    /// then returns a snapshot for the client (Zustand).
    /// </summary>
    [HttpPost("ensure")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EnsureUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EnsureUserResponse>> Ensure(CancellationToken cancellationToken)
    {
        var dto = await ensureUserService.EnsureAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
