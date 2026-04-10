using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface ICashflowService
{
    Task<CashflowResponse> GetAsync(
        ClaimsPrincipal user,
        QueryCashflowRequest request,
        CancellationToken cancellationToken = default);
}
