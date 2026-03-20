using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IEnsureUserService
{
    Task<EnsureUserResponse> EnsureAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
