using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IStackedExpensesByPfcPrimaryService
{
    Task<StackedExpensesByPfcPrimaryResponse> GetAsync(
        ClaimsPrincipal user,
        StackedExpensesByPfcPrimaryQueryRequest request,
        CancellationToken cancellationToken = default);
}
