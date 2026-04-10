using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface INetWorthService
{
    Task<NetWorthResponse> GetNetWorthAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
