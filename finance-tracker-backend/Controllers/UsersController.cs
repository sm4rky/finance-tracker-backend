using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IEnsureUserService ensureUserService, IProfileService profileService)
    : ControllerBase
{
    /// <summary>
    /// Idempotent: ensures public.profiles and default free public.profile_subscriptions for the JWT subject.
    /// On first profile insert, sends welcome via Resend (Resend:WelcomeTemplateId) and records public.email_logs.
    /// Supabase Auth emails (confirm signup, password reset, …) stay in the Supabase + Resend project settings.
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

    [HttpPost("username")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EnsureUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnsureUserResponse>> SetUsername(
        [FromBody] SetUsernameRequest body,
        CancellationToken cancellationToken)
    {
        await profileService.SetUsernameAsync(User, body.Username, cancellationToken).ConfigureAwait(false);
        var dto = await ensureUserService.EnsureAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
