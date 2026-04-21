using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface INetWorthService
{
    Task<NetWorthResponse> GetNetWorthAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<NetWorthResponse> GetNetWorthForProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<MonthlyNetWorthHistoryResponse> GetMonthlyNetWorthHistoryAsync(
        ClaimsPrincipal user,
        MonthlyNetWorthHistoryQueryRequest request,
        CancellationToken cancellationToken = default);
}
